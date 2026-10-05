/*
 * File        : ReservationUpdateRequest.cs
 * Description : Payload for changing the energy amount of an existing reservation.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class ReservationUpdateRequest
{
    [Range(0.01, double.MaxValue)]
    public decimal EnergyKwh { get; set; }
}
