/*
 * File        : StationRequest.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Payload for creating or updating a microgrid station.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class StationRequest
{
    [Required]
    public string Name { get; set; } = "";

    [Required]
    public string Location { get; set; } = "";

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal CapacityKwh { get; set; }

    [Range(1, int.MaxValue)]
    public int BatteryStorageSlots { get; set; }
}
