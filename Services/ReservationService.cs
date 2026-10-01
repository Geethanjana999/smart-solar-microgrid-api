/*
 * File        : ReservationService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Reservation rules: 7-day booking window, 12-hour notice, approval, QR verification,
 *               finalisation, and the filtered views used by the dashboards.
 */
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Data;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class ReservationService(MongoContext db) : IReservationService
{
    // True when the slot starts at least 12 hours from now (required to update or cancel).
    private static bool HasEnoughNotice(DateTime slotStart) => slotStart >= DateTime.UtcNow.AddHours(12);

    // Books energy from a slot that starts within the next 7 days; the reservation starts as Pending.
    public async Task<ReservationResponse> CreateAsync(string nic, ReservationRequest request)
    {
        var prosumer = await db.Prosumers.Find(x => x.Nic == nic).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Prosumer not found.");
        if (!prosumer.IsActive)
        {
            throw new InvalidOperationException("Prosumer account is not active.");
        }

        var slot = await db.Slots.Find(x => x.Id == request.SlotId && x.IsActive).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Available slot not found.");

        if (await db.Stations.Find(x => x.Id == slot.StationId && x.IsActive).FirstOrDefaultAsync() == null)
        {
            throw new InvalidOperationException("The station for this slot is not active.");
        }

        if (slot.StartTime > DateTime.UtcNow.AddDays(7) || slot.StartTime < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Bookings must be within the next 7 days.");
        }

        if (request.EnergyKwh > slot.AvailableKwh)
        {
            throw new InvalidOperationException("Insufficient energy available.");
        }

        var reservation = new EnergyReservation
        {
            ProsumerNic = nic,
            StationId = slot.StationId,
            SlotId = slot.Id,
            EnergyKwh = request.EnergyKwh
        };

        await AdjustSlotEnergyAsync(slot, -request.EnergyKwh);
        await db.Reservations.InsertOneAsync(reservation);
        return await ToResponseAsync(reservation);
    }

    // Changes the reserved energy (12-hour notice required) and adjusts the slot by the difference.
    public async Task<ReservationResponse> UpdateAsync(string id, string? nic, ReservationUpdateRequest request)
    {
        var reservation = await GetEditableAsync(id, nic);
        var slot = await GetSlotAsync(reservation.SlotId);

        if (!HasEnoughNotice(slot.StartTime))
        {
            throw new InvalidOperationException("Updates require at least 12 hours notice.");
        }

        var difference = request.EnergyKwh - reservation.EnergyKwh;
        if (difference > slot.AvailableKwh)
        {
            throw new InvalidOperationException("Insufficient energy available.");
        }

        await AdjustSlotEnergyAsync(slot, -difference);
        reservation.EnergyKwh = request.EnergyKwh;
        reservation.UpdatedAt = DateTime.UtcNow;
        await db.Reservations.ReplaceOneAsync(x => x.Id == id, reservation);
        return await ToResponseAsync(reservation);
    }

    // Cancels a reservation (12-hour notice required) and returns its energy to the slot.
    public async Task<ReservationResponse> CancelAsync(string id, string? nic)
    {
        var reservation = await GetEditableAsync(id, nic);
        var slot = await GetSlotAsync(reservation.SlotId);

        if (!HasEnoughNotice(slot.StartTime))
        {
            throw new InvalidOperationException("Cancellation requires at least 12 hours notice.");
        }

        await AdjustSlotEnergyAsync(slot, reservation.EnergyKwh);
        reservation.Status = ReservationStatus.Cancelled;
        reservation.UpdatedAt = DateTime.UtcNow;
        await db.Reservations.ReplaceOneAsync(x => x.Id == id, reservation);
        return await ToResponseAsync(reservation);
    }

    // Lists reservations with optional status, scope (current/history), date, station and text filters.
    public async Task<List<ReservationResponse>> QueryAsync(string? nic, ReservationQuery query)
    {
        var builder = Builders<EnergyReservation>.Filter;
        var filter = builder.Empty;

        var owner = nic ?? query.ProsumerNic;
        if (!string.IsNullOrWhiteSpace(owner))
        {
            filter &= builder.Eq(x => x.ProsumerNic, owner);
        }

        if (!string.IsNullOrWhiteSpace(query.State))
        {
            if (!Enum.TryParse<ReservationStatus>(query.State, true, out var status))
            {
                throw new InvalidOperationException("Unknown reservation state.");
            }

            filter &= builder.Eq(x => x.Status, status);
        }

        if (!string.IsNullOrWhiteSpace(query.StationId))
        {
            filter &= builder.Eq(x => x.StationId, query.StationId);
        }

        var reservations = await db.Reservations.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync();
        var results = await ToResponsesAsync(reservations);

        var now = DateTime.UtcNow;
        // A reservation is "current" while it is open (Pending/Confirmed) and its slot has not ended.
        bool IsCurrent(ReservationResponse r) =>
            (r.Status == nameof(ReservationStatus.Pending) || r.Status == nameof(ReservationStatus.Confirmed)) && r.SlotEnd > now;

        IEnumerable<ReservationResponse> filtered = results;
        switch (query.Scope?.ToLowerInvariant())
        {
            case "current":
                filtered = filtered.Where(IsCurrent);
                break;
            case "history":
                filtered = filtered.Where(r => !IsCurrent(r));
                break;
            case null or "":
                break;
            default:
                throw new InvalidOperationException("Scope must be 'current' or 'history'.");
        }

        if (query.From.HasValue)
        {
            filtered = filtered.Where(r => r.SlotStart >= query.From.Value.ToUniversalTime());
        }

        if (query.To.HasValue)
        {
            filtered = filtered.Where(r => r.SlotStart <= query.To.Value.ToUniversalTime());
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            filtered = filtered.Where(r =>
                r.StationName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || r.StationLocation.Contains(term, StringComparison.OrdinalIgnoreCase)
                || r.Status.Contains(term, StringComparison.OrdinalIgnoreCase)
                || r.Id.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return filtered.ToList();
    }

    // Staff approve a pending reservation; only then does the prosumer receive a QR token.
    public async Task<ReservationResponse> ApproveAsync(string id)
    {
        var reservation = await db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Reservation not found.");

        if (reservation.Status != ReservationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending reservations can be approved.");
        }

        var slot = await GetSlotAsync(reservation.SlotId);
        if (slot.EndTime <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("The slot for this reservation has already ended.");
        }

        reservation.Status = ReservationStatus.Confirmed;
        reservation.UpdatedAt = DateTime.UtcNow;
        await db.Reservations.ReplaceOneAsync(x => x.Id == id, reservation);
        return await ToResponseAsync(reservation);
    }

    // Finds the approved reservation matching a scanned QR token.
    public async Task<ReservationResponse> VerifyAsync(string token)
    {
        var reservation = await db.Reservations.Find(x => x.QrToken == token).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Valid reservation not found.");

        if (reservation.Status != ReservationStatus.Confirmed)
        {
            throw new InvalidOperationException($"Reservation is {reservation.Status} and cannot be dispatched.");
        }

        return await ToResponseAsync(reservation);
    }

    // Marks an approved reservation completed and records which operator finalised it.
    public async Task<ReservationResponse> FinalizeAsync(string id, string operatorName)
    {
        var reservation = await db.Reservations.Find(x => x.Id == id).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Reservation not found.");

        if (reservation.Status != ReservationStatus.Confirmed)
        {
            throw new InvalidOperationException("Only approved reservations can be finalized.");
        }

        reservation.Status = ReservationStatus.Completed;
        reservation.FinalizedAt = DateTime.UtcNow;
        reservation.FinalizedBy = operatorName;
        reservation.UpdatedAt = DateTime.UtcNow;
        await db.Reservations.ReplaceOneAsync(x => x.Id == id, reservation);
        return await ToResponseAsync(reservation);
    }

    // Loads an open reservation; when nic is given it must belong to that prosumer.
    private async Task<EnergyReservation> GetEditableAsync(string id, string? nic)
    {
        var reservation = await db.Reservations.Find(x => x.Id == id && (nic == null || x.ProsumerNic == nic)).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Reservation not found.");

        if (reservation.Status == ReservationStatus.Cancelled || reservation.Status == ReservationStatus.Completed)
        {
            throw new InvalidOperationException("Reservation is no longer editable.");
        }

        return reservation;
    }

    // Loads a slot or throws 404.
    private async Task<EnergyBookingSlot> GetSlotAsync(string slotId) =>
        await db.Slots.Find(x => x.Id == slotId).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Slot not found.");

    // Changes a slot's available energy only if nobody else changed it meanwhile (optimistic concurrency).
    private async Task AdjustSlotEnergyAsync(EnergyBookingSlot slot, decimal delta)
    {
        var expected = slot.AvailableKwh;
        slot.AvailableKwh = expected + delta;
        slot.UpdatedAt = DateTime.UtcNow;

        var result = await db.Slots.ReplaceOneAsync(x => x.Id == slot.Id && x.AvailableKwh == expected, slot);
        if (result.MatchedCount == 0)
        {
            throw new InvalidOperationException("The slot was just changed by another booking. Please try again.");
        }
    }

    // Converts one reservation into its client response.
    private async Task<ReservationResponse> ToResponseAsync(EnergyReservation reservation) =>
        (await ToResponsesAsync(new[] { reservation })).Single();

    // Joins reservations with their slots and stations (two batched queries) to build client responses.
    private async Task<List<ReservationResponse>> ToResponsesAsync(IReadOnlyCollection<EnergyReservation> reservations)
    {
        if (reservations.Count == 0)
        {
            return new List<ReservationResponse>();
        }

        var slotIds = reservations.Select(x => x.SlotId).Distinct().ToList();
        var stationIds = reservations.Select(x => x.StationId).Distinct().ToList();
        var slots = (await db.Slots.Find(x => slotIds.Contains(x.Id)).ToListAsync()).ToDictionary(x => x.Id);
        var stations = (await db.Stations.Find(x => stationIds.Contains(x.Id)).ToListAsync()).ToDictionary(x => x.Id);

        return reservations.Select(r =>
        {
            slots.TryGetValue(r.SlotId, out var slot);
            stations.TryGetValue(r.StationId, out var station);
            return new ReservationResponse(
                r.Id,
                r.ProsumerNic,
                r.StationId,
                station?.Name ?? "Unknown station",
                station?.Location ?? "",
                r.SlotId,
                slot?.StartTime ?? default,
                slot?.EndTime ?? default,
                r.EnergyKwh,
                r.EnergyKwh * (slot?.PricePerKwh ?? 0),
                r.Status.ToString(),
                r.Status == ReservationStatus.Confirmed ? r.QrToken : null,
                r.CreatedAt,
                r.FinalizedAt,
                r.FinalizedBy);
        }).ToList();
    }
}
