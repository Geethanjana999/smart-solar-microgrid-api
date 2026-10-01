/*
 * File        : StaffReservationRequest.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Payload for Backoffice/operator staff booking on behalf of a prosumer.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class StaffReservationRequest
{
    [Required]
    public string ProsumerNic { get; set; } = "";

    [Required]
    public string SlotId { get; set; } = "";

    [Range(0.01, double.MaxValue)]
    public decimal EnergyKwh { get; set; }
}
