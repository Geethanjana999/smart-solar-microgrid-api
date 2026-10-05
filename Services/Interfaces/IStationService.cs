/*
 * File        : IStationService.cs
 * Description : Contract for microgrid station management and map queries.
 */
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Services.Interfaces;

public interface IStationService
{
    Task<List<SolarStationInfo>> GetAsync(bool activeOnly = true);

    Task<List<NearbyStationResponse>> GetNearbyAsync(double latitude, double longitude, double radiusKm);

    Task<SolarStationInfo> CreateAsync(StationRequest request);

    Task<SolarStationInfo> UpdateAsync(string id, StationRequest request);

    Task<SolarStationInfo> UpdateScheduleAsync(string id, NodeScheduleRequest request);

    Task<SolarStationInfo> UpdateBatterySlotsAsync(string id, BatterySlotsRequest request);

    Task DeactivateAsync(string id);

    Task<SolarStationInfo> ActivateAsync(string id);
}
