/*
 * File        : SlotResponse.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Bookable slot returned to the Android app.
 */
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed record SlotResponse(
    string Id,
    string StationId,
    DateTime SlotDate,
    string SlotTime,
    DateTime StartTime,
    DateTime EndTime,
    decimal AvailableKwh,
    decimal PricePerKwh)
{
    public static SlotResponse From(EnergyBookingSlot s) => new(
        s.Id,
        s.StationId,
        SriLankaTime.ToClientInstant(SriLankaTime.LocalDate(s.StartTime)),
        SriLankaTime.SlotText(s.StartTime, s.EndTime),
        s.StartTime,
        s.EndTime,
        s.AvailableKwh,
        s.PricePerKwh);
}