/*
 * File        : SlotService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Business rules for energy booking slots.
 */
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Data;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class SlotService(MongoContext db) : ISlotService
{
    // Lists active, not-yet-ended slots, optionally limited to one station.
    public Task<List<EnergyBookingSlot>> GetAsync(string? stationId) =>
        db.Slots.Find(x => x.IsActive && (stationId == null || x.StationId == stationId) && x.EndTime > DateTime.UtcNow)
            .ToListAsync();

    // Creates a slot after checking the time range and that the station is active.
    public async Task<EnergyBookingSlot> CreateAsync(SlotRequest request)
    {
        if (request.EndTime <= request.StartTime)
        {
            throw new InvalidOperationException("End time must be after start time.");
        }

        if (await db.Stations.Find(x => x.Id == request.StationId && x.IsActive).FirstOrDefaultAsync() == null)
        {
            throw new InvalidOperationException("Active station not found.");
        }

        var slot = new EnergyBookingSlot
        {
            StationId = request.StationId,
            StartTime = request.StartTime.ToUniversalTime(),
            EndTime = request.EndTime.ToUniversalTime(),
            AvailableKwh = request.AvailableKwh,
            PricePerKwh = request.PricePerKwh
        };
        await db.Slots.InsertOneAsync(slot);
        return slot;
    }

    // Updates a slot's time range, energy and price.
    public async Task<EnergyBookingSlot> UpdateAsync(string id, SlotRequest request)
    {
        var slot = await db.Slots.Find(x => x.Id == id).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Slot not found.");

        if (request.EndTime <= request.StartTime)
        {
            throw new InvalidOperationException("End time must be after start time.");
        }

        slot.StartTime = request.StartTime.ToUniversalTime();
        slot.EndTime = request.EndTime.ToUniversalTime();
        slot.AvailableKwh = request.AvailableKwh;
        slot.PricePerKwh = request.PricePerKwh;
        slot.UpdatedAt = DateTime.UtcNow;
        await db.Slots.ReplaceOneAsync(x => x.Id == id, slot);
        return slot;
    }

    // Deletes a slot unless pending/confirmed reservations still use it.
    public async Task DeleteAsync(string id)
    {
        var hasActiveReservations = await db.Reservations
            .Find(x => x.SlotId == id && (x.Status == ReservationStatus.Pending || x.Status == ReservationStatus.Confirmed))
            .AnyAsync();
        if (hasActiveReservations)
        {
            throw new InvalidOperationException("Slot has active reservations.");
        }

        if ((await db.Slots.DeleteOneAsync(x => x.Id == id)).DeletedCount == 0)
        {
            throw new KeyNotFoundException("Slot not found.");
        }
    }
}
