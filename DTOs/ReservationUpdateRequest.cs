/*
 * File        : ReservationUpdateRequest.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Payload for changing the energy amount of an existing reservation.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class ReservationUpdateRequest
{
    [Range(0.01, double.MaxValue)]
    public decimal EnergyKwh { get; set; }
}
