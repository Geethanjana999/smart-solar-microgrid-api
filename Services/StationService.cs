/*
 * File        : StationService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Business rules for microgrid stations: CRUD, battery slots, deactivation block and nearby search.
 */
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Data;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class StationService(MongoContext db) : IStationService
{
    private const double EarthRadiusKm = 6371.0;

    // Lists stations; by default only active ones.
    public Task<List<SolarStationInfo>> GetAsync(bool activeOnly = true) =>
        db.Stations.Find(x => !activeOnly || x.IsActive).ToListAsync();

    // Returns active stations within radiusKm of the point, nearest first.
    public async Task<List<NearbyStationResponse>> GetNearbyAsync(double latitude, double longitude, double radiusKm)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new InvalidOperationException("Invalid coordinates.");
        }

        var stations = await db.Stations.Find(x => x.IsActive).ToListAsync();
        return stations
            .Select(s => new NearbyStationResponse(s, Math.Round(DistanceKm(latitude, longitude, s.Latitude, s.Longitude), 2)))
            .Where(r => r.DistanceKm <= radiusKm)
            .OrderBy(r => r.DistanceKm)
            .ToList();
    }

    // Creates a station; all battery slots start out available.
    public async Task<SolarStationInfo> CreateAsync(StationRequest request)
    {
        var station = new SolarStationInfo
        {
            Name = request.Name,
            Location = request.Location,
            NodeCode = request.NodeCode,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CapacityKwh = request.CapacityKwh,
            BatteryStorageSlots = request.BatteryStorageSlots,
            AvailableBatterySlots = request.BatteryStorageSlots
        };
        await db.Stations.InsertOneAsync(station);
        return station;
    }

    // Overwrites the station's details, keeping the available battery slots consistent with the new total.
    public async Task<SolarStationInfo> UpdateAsync(string id, StationRequest request)
    {
        var station = await FindAsync(id);
        var change = request.BatteryStorageSlots - station.BatteryStorageSlots;

        station.Name = request.Name;
        station.Location = request.Location;
        station.NodeCode = request.NodeCode;
        station.Latitude = request.Latitude;
        station.Longitude = request.Longitude;
        station.CapacityKwh = request.CapacityKwh;
        station.BatteryStorageSlots = request.BatteryStorageSlots;
        station.AvailableBatterySlots = Math.Clamp(station.AvailableBatterySlots + change, 0, request.BatteryStorageSlots);
        station.UpdatedAt = DateTime.UtcNow;
        await db.Stations.ReplaceOneAsync(x => x.Id == id, station);
        return station;
    }

    public async Task<SolarStationInfo> UpdateScheduleAsync(string id, NodeScheduleRequest request)
    {
        var station = await FindAsync(id);
        
        station.Schedule.OperationalStartTime = request.OperationalStartTime;
        station.Schedule.OperationalEndTime = request.OperationalEndTime;
        station.Schedule.PeakTradingStartTime = request.PeakTradingStartTime;
        station.Schedule.PeakTradingEndTime = request.PeakTradingEndTime;
        station.Schedule.MaintenanceDay = request.MaintenanceDay;
        
        station.UpdatedAt = DateTime.UtcNow;
        await db.Stations.ReplaceOneAsync(x => x.Id == id, station);
        return station;
    }

    // Grid operator updates how many battery slots are free (cannot exceed the installed total).
    public async Task<SolarStationInfo> UpdateBatterySlotsAsync(string id, BatterySlotsRequest request)
    {
        var station = await FindAsync(id);
        if (request.AvailableBatterySlots > station.BatteryStorageSlots)
        {
            throw new InvalidOperationException("Available battery slots cannot exceed the station's total battery slots.");
        }

        station.AvailableBatterySlots = request.AvailableBatterySlots;
        station.UpdatedAt = DateTime.UtcNow;
        await db.Stations.ReplaceOneAsync(x => x.Id == id, station);
        return station;
    }

    // Deactivates a station unless pending/confirmed reservations still exist for it.
    public async Task DeactivateAsync(string id)
    {
        var hasActiveReservations = await db.Reservations
            .Find(x => x.StationId == id && (x.Status == ReservationStatus.Pending || x.Status == ReservationStatus.Confirmed))
            .AnyAsync();
        if (hasActiveReservations)
        {
            throw new InvalidOperationException("Station has active reservations.");
        }

        var station = await FindAsync(id);
        station.IsActive = false;
        station.UpdatedAt = DateTime.UtcNow;
        await db.Stations.ReplaceOneAsync(x => x.Id == id, station);
    }

    // Brings a deactivated station back into service.
    public async Task<SolarStationInfo> ActivateAsync(string id)
    {
        var station = await FindAsync(id);
        station.IsActive = true;
        station.UpdatedAt = DateTime.UtcNow;
        await db.Stations.ReplaceOneAsync(x => x.Id == id, station);
        return station;
    }

    // Loads a station or throws 404.
    private async Task<SolarStationInfo> FindAsync(string id) =>
        await db.Stations.Find(x => x.Id == id).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Station not found.");

    // Great-circle distance between two GPS points using the haversine formula.
    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        static double Rad(double degrees) => degrees * Math.PI / 180;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
