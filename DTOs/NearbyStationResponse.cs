/*
 * File        : NearbyStationResponse.cs
 * Description : A station plus its distance from the caller, used by the mobile map.
 */
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed record NearbyStationResponse(SolarStationInfo Station, double DistanceKm);
