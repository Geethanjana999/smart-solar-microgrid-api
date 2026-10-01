/*
 * File        : ReservationRequest.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Payload for a prosumer creating a reservation.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class ReservationRequest
{
    [Required]
    public string SlotId { get; set; } = "";

    [Range(0.01, double.MaxValue)]
    public decimal EnergyKwh { get; set; }
}
