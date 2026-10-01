/*
 * File        : ReservationResponse.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Reservation joined with station and slot details for the clients.
 */
namespace SmartSolarMicrogrid.Api.DTOs;

// QrToken is only populated once the reservation is approved (Confirmed).
public sealed record ReservationResponse(
    string Id,
    string ProsumerNic,
    string StationId,
    string StationName,
    string StationLocation,
    string SlotId,
    DateTime SlotStart,
    DateTime SlotEnd,
    decimal EnergyKwh,
    decimal TotalPrice,
    string Status,
    string? QrToken,
    DateTime CreatedAt,
    DateTime? FinalizedAt,
    string? FinalizedBy);
