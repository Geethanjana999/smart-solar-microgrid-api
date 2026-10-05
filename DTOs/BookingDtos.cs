/*
 * File        : BookingDtos.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Request/response contracts for the Android bookings endpoints.
 */
using System.ComponentModel.DataAnnotations;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services;

namespace SmartSolarMicrogrid.Api.DTOs;

// Used for create (POST) and update (PUT). The NIC is never read from the body (it comes from the JWT).
// Either SlotId, or NodeId + SlotDate + SlotTime, identifies the slot.
public sealed class BookingRequest
{
    [StringLength(50)]
    public string? SlotId { get; set; }

    [StringLength(50, ErrorMessage = "Node id is too long.")]
    public string? NodeId { get; set; }

    public DateTime? SlotDate { get; set; }

    [StringLength(40, ErrorMessage = "Slot time is too long.")]
    public string? SlotTime { get; set; }

    [Range(typeof(decimal), "0.1", "1000", ErrorMessage = "Energy must be between 0.1 and 1000 kWh.")]
    public decimal EnergyKwh { get; set; }

    [StringLength(500, ErrorMessage = "Notes are too long.")]
    public string? Notes { get; set; }
}

public sealed record BookingResponse(
    string Id,
    string ProsumerNic,
    string SlotId,
    string NodeId,
    DateTime SlotDate,
    string SlotTime,
    DateTime StartTime,
    DateTime EndTime,
    decimal EnergyKwh,
    decimal PricePerKwh,
    decimal TotalPrice,
    string Notes,
    string Status,
    DateTime CreatedAt)
{
    public static BookingResponse From(Reservation r) => new(
        r.Id,
        r.ProsumerNic,
        r.SlotId,
        r.NodeId,
        SriLankaTime.ToClientInstant(SriLankaTime.LocalDate(r.StartTime)),
        SriLankaTime.SlotText(r.StartTime, r.EndTime),
        r.StartTime,
        r.EndTime,
        r.EnergyKwh,
        r.PricePerKwh,
        Math.Round(r.EnergyKwh * r.PricePerKwh, 2),
        r.Notes,
        r.Status.ToString(),
        r.CreatedAt);
}