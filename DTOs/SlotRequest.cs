/*
 * File        : SlotRequest.cs
 * Description : Payload for creating or updating an energy booking slot.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class SlotRequest
{
    [Required]
    public string StationId { get; set; } = "";

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal AvailableKwh { get; set; }

    [Range(0, double.MaxValue)]
    public decimal PricePerKwh { get; set; }
}
