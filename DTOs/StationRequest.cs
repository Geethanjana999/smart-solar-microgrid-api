/*
 * File        : StationRequest.cs
 * Description : Payload for creating or updating a microgrid station.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class StationRequest
{
    [Required]
    public string Name { get; set; } = "";

    public string Location { get; set; } = "";

    [Required]
    public string NodeCode { get; set; } = "";

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal CapacityKwh { get; set; }

    [Range(1, int.MaxValue)]
    public int BatterySlotsCount { get; set; }
    
    // For backwards compatibility
    public int BatteryStorageSlots { get => BatterySlotsCount; set => BatterySlotsCount = value; }
}
