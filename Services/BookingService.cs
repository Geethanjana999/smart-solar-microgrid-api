/*
 * File        : BookingService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Slot-based booking rules: atomic energy reservation, 12-hour change window, ownership checks.
 */
using System.Globalization;
using MongoDB.Bson;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Data;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class BookingService(MongoContext db) : IBookingService
{
    private const int MinHoursBeforeChange = 12; // update/cancel cut-off before the slot starts

    private static readonly string[] TimeFormats = ["hh:mm tt", "h:mm tt"];

    // ---------- public operations ----------

    public async Task<BookingResponse> CreateAsync(string nic, BookingRequest request)
    {
        await EnsureActiveProsumerAsync(nic);
        var kwh = ValidateEnergy(request.EnergyKwh);
        var slot = await ResolveSlotAsync(request);

        var reserved = await ReserveAsync(slot.Id, kwh);

        var reservation = new Reservation
        {
            ProsumerNic = nic,
            SlotId = reserved.Id,
            NodeId = reserved.StationId,
            StartTime = reserved.StartTime,
            EndTime = reserved.EndTime,
            EnergyKwh = kwh,
            PricePerKwh = reserved.PricePerKwh,
            Notes = request.Notes?.Trim() ?? "",
            Status = ReservationStatus.Pending
        };

        try
        {
            await db.Bookings.InsertOneAsync(reservation);
        }
        catch
        {
            await ReleaseAsync(reserved.Id, kwh); // give the energy back if saving failed
            throw;
        }

        return BookingResponse.From(reservation);
    }

    public async Task<List<BookingResponse>> ListAsync(string nic)
    {
        var items = await db.Bookings.Find(x => x.ProsumerNic == nic)
            .SortByDescending(x => x.StartTime)
            .ToListAsync();
        return items.Select(BookingResponse.From).ToList();
    }

    public async Task<BookingResponse> GetAsync(string nic, string id) =>
        BookingResponse.From(await FindOwnedAsync(nic, id));

    public async Task<BookingResponse> UpdateAsync(string nic, string id, BookingRequest request)
    {
        var reservation = await FindOwnedAsync(nic, id);
        EnsureChangeAllowed(reservation);

        var newKwh = ValidateEnergy(request.EnergyKwh);
        var newSlot = await ResolveSlotAsync(request);
        var oldSlotId = reservation.SlotId;
        var oldKwh = reservation.EnergyKwh;

        EnergyBookingSlot target;
        if (newSlot.Id == oldSlotId)
        {
            // Same slot: only the difference moves.
            var delta = newKwh - oldKwh;
            if (delta > 0) target = await ReserveAsync(newSlot.Id, delta);
            else
            {
                if (delta < 0) await ReleaseAsync(newSlot.Id, -delta);
                target = newSlot;
            }
        }
        else
        {
            // Different slot: take the new energy first, then free the old one.
            target = await ReserveAsync(newSlot.Id, newKwh);
            await ReleaseAsync(oldSlotId, oldKwh);
        }

        reservation.SlotId = target.Id;
        reservation.NodeId = target.StationId;
        reservation.StartTime = target.StartTime;
        reservation.EndTime = target.EndTime;
        reservation.EnergyKwh = newKwh;
        reservation.PricePerKwh = target.PricePerKwh;
        reservation.Notes = request.Notes?.Trim() ?? "";
        reservation.UpdatedAt = DateTime.UtcNow;

        await db.Bookings.ReplaceOneAsync(x => x.Id == reservation.Id, reservation);
        return BookingResponse.From(reservation);
    }

    // Cancelling keeps the record for history and returns the energy to the slot.
    public async Task CancelAsync(string nic, string id)
    {
        var reservation = await FindOwnedAsync(nic, id);
        EnsureChangeAllowed(reservation);

        // Atomic status change: only one caller can flip it, so the energy is released once.
        var result = await db.Bookings.UpdateOneAsync(
            x => x.Id == reservation.Id
                 && x.ProsumerNic == nic
                 && (x.Status == ReservationStatus.Pending || x.Status == ReservationStatus.Confirmed),
            Builders<Reservation>.Update
                .Set(x => x.Status, ReservationStatus.Cancelled)
                .Set(x => x.UpdatedAt, DateTime.UtcNow));

        if (result.ModifiedCount == 0)
        {
            throw new InvalidOperationException("Only pending or confirmed bookings can be cancelled.");
        }

        await ReleaseAsync(reservation.SlotId, reservation.EnergyKwh);
    }

    public async Task<DashboardResponse> DashboardAsync(string nic)
    {
        var all = await db.Bookings.Find(x => x.ProsumerNic == nic).ToListAsync();
        var now = DateTime.UtcNow;

        var upcoming = all
            .Where(IsActive)
            .Where(x => x.StartTime > now)
            .OrderBy(x => x.StartTime)
            .ToList();

        var activeStations = await db.Stations.CountDocumentsAsync(x => x.IsActive);
        var availableSlots = await db.Slots.CountDocumentsAsync(x =>
            x.IsActive && x.AvailableKwh > 0 && x.EndTime > now);

        return new DashboardResponse(
            ActiveStations: (int)activeStations,
            AvailableSlots: (int)availableSlots,
            PendingReservations: all.Count(x => x.Status == ReservationStatus.Pending),
            ApprovedFutureReservations: all.Count(x =>
                x.Status == ReservationStatus.Confirmed && x.StartTime > now),
            CompletedReservations: all.Count(x => x.Status == ReservationStatus.Completed),
            AvailableEnergyKwh: (await db.Slots.Find(x =>
                x.IsActive && x.AvailableKwh > 0 && x.EndTime > now).ToListAsync())
                .Sum(x => x.AvailableKwh));
    }

    // ---------- slot energy (atomic) ----------

    // Takes kWh from the slot only if it is active, in the future and has enough energy left.
    private async Task<EnergyBookingSlot> ReserveAsync(string slotId, decimal kwh)
    {
        var f = Builders<EnergyBookingSlot>.Filter;
        var filter = f.Eq(x => x.Id, slotId)
                     & f.Eq(x => x.IsActive, true)
                     & f.Gte(x => x.AvailableKwh, kwh)
                     & f.Gt(x => x.StartTime, DateTime.UtcNow);

        var updated = await db.EnergyBookingSlots.FindOneAndUpdateAsync(
            filter,
            Builders<EnergyBookingSlot>.Update.Inc(x => x.AvailableKwh, -kwh),
            new FindOneAndUpdateOptions<EnergyBookingSlot> { ReturnDocument = ReturnDocument.After });

        return updated ?? throw new InvalidOperationException("This slot is no longer available or does not have enough energy.");
    }

    private Task ReleaseAsync(string slotId, decimal kwh) =>
        db.EnergyBookingSlots.UpdateOneAsync(
            x => x.Id == slotId,
            Builders<EnergyBookingSlot>.Update.Inc(x => x.AvailableKwh, kwh));

    // ---------- helpers ----------

    private async Task EnsureActiveProsumerAsync(string nic)
    {
        var prosumer = await db.Prosumers.Find(x => x.Nic == nic).FirstOrDefaultAsync()
                       ?? throw new KeyNotFoundException("Prosumer not found.");
        if (!prosumer.IsActive)
        {
            throw new InvalidOperationException("Your account is not active.");
        }
    }

    private async Task<Reservation> FindOwnedAsync(string nic, string id)
    {
        // Same message for "missing" and "not yours" so ids can't be probed.
        if (!ObjectId.TryParse(id, out _)) throw new KeyNotFoundException("Booking not found.");
        var reservation = await db.Bookings.Find(x => x.Id == id && x.ProsumerNic == nic).FirstOrDefaultAsync();
        return reservation ?? throw new KeyNotFoundException("Booking not found.");
    }

    private static bool IsActive(Reservation r) =>
        r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed;

    private static void EnsureChangeAllowed(Reservation r)
    {
        if (!IsActive(r))
        {
            throw new InvalidOperationException("Only pending or confirmed bookings can be changed.");
        }

        if (r.StartTime - DateTime.UtcNow < TimeSpan.FromHours(MinHoursBeforeChange))
        {
            throw new InvalidOperationException($"Bookings can only be changed at least {MinHoursBeforeChange} hours before the slot starts.");
        }
    }

    private static decimal ValidateEnergy(decimal kwh)
    {
        if (kwh <= 0) throw new InvalidOperationException("Energy must be greater than zero.");
        return Math.Round(kwh, 3);
    }

    // Finds the slot by SlotId, or by NodeId (= StationId) + Sri Lanka date + "hh:mm tt - hh:mm tt".
    private async Task<EnergyBookingSlot> ResolveSlotAsync(BookingRequest request)
    {
        var f = Builders<EnergyBookingSlot>.Filter;
        EnergyBookingSlot? slot;

        if (!string.IsNullOrWhiteSpace(request.SlotId))
        {
            var slotId = request.SlotId.Trim();
            if (!ObjectId.TryParse(slotId, out _)) throw new KeyNotFoundException("Slot not found.");
            slot = await db.EnergyBookingSlots.Find(x => x.Id == slotId).FirstOrDefaultAsync();
        }
        else
        {
            var nodeId = (request.NodeId ?? "").Trim();
            if (nodeId.Length == 0) throw new InvalidOperationException("Node is required.");
            if (request.SlotDate is null) throw new InvalidOperationException("Slot date is required.");
            if (!TryParseStart(request.SlotTime ?? "", out var startOfDay))
            {
                throw new InvalidOperationException("Slot time must look like '06:00 PM - 08:00 PM'.");
            }

            var localDate = SriLankaTime.ToStoredDate(request.SlotDate.Value);
            var startUtc = DateTime.SpecifyKind(localDate.Date + startOfDay - SriLankaTime.Offset, DateTimeKind.Utc);

            var stationFilter = Builders<SolarStationInfo>.Filter.Or(
                Builders<SolarStationInfo>.Filter.Eq(x => x.NodeCode, nodeId),
                Builders<SolarStationInfo>.Filter.Eq(x => x.NodeCode, nodeId.ToUpperInvariant()));
            if (ObjectId.TryParse(nodeId, out _))
            {
                stationFilter |= Builders<SolarStationInfo>.Filter.Eq(x => x.Id, nodeId);
            }

            var station = await db.Stations.Find(stationFilter).FirstOrDefaultAsync();
            if (station is null) throw new KeyNotFoundException("Station not found.");

            slot = await db.EnergyBookingSlots
                .Find(f.Eq(x => x.StationId, station.Id) & f.Eq(x => x.StartTime, startUtc))
                .FirstOrDefaultAsync();
        }

        if (slot is null || !slot.IsActive) throw new KeyNotFoundException("Slot not found.");
        return slot;
    }

    private static bool TryParseStart(string text, out TimeSpan start)
    {
        start = default;
        var parts = text.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return false;
        if (!DateTime.TryParseExact(parts[0], TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var s)) return false;
        start = s.TimeOfDay;
        return true;
    }
}