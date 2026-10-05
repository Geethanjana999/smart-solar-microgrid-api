/*
 * File        : SlotService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Lists future, active slots that still have energy available.
 */
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Data;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class SlotService(MongoContext db) : ISlotService
{
    public Task<List<SlotResponse>> GetAsync(string stationId) =>
        ListAvailableAsync(stationId, null);

    public async Task<List<SlotResponse>> ListAvailableAsync(string? stationId, DateTime? date)
    {
        var f = Builders<EnergyBookingSlot>.Filter;
        var filter = f.Eq(x => x.IsActive, true)
                     & f.Gt(x => x.AvailableKwh, 0m)
                     & f.Gt(x => x.StartTime, DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(stationId))
        {
            var station = stationId.Trim();
            var stationFilter = Builders<SolarStationInfo>.Filter.Or(
                Builders<SolarStationInfo>.Filter.Eq(x => x.NodeCode, station),
                Builders<SolarStationInfo>.Filter.Eq(x => x.NodeCode, station.ToUpperInvariant()));
            if (ObjectId.TryParse(station, out _))
            {
                stationFilter |= Builders<SolarStationInfo>.Filter.Eq(x => x.Id, station);
            }

            var stationRecord = await db.Stations.Find(stationFilter).FirstOrDefaultAsync();
            if (stationRecord is null)
            {
                return [];
            }

            filter &= f.Eq(x => x.StationId, stationRecord.Id);
        }

        if (date is not null)
        {
            // Sri Lanka day -> UTC range, so a slot at 06:00 PM Colombo lands on the right day.
            var dayStartUtc = SriLankaTime.ToClientInstant(SriLankaTime.ToStoredDate(date.Value));
            filter &= f.Gte(x => x.StartTime, dayStartUtc) & f.Lt(x => x.StartTime, dayStartUtc.AddDays(1));
        }

        var slots = await db.EnergyBookingSlots.Find(filter).SortBy(x => x.StartTime).Limit(200).ToListAsync();
        return slots.Select(SlotResponse.From).ToList();
    }
}