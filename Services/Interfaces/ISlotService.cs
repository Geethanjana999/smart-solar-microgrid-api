/*
 * File        : ISlotService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Read access to bookable energy slots for the Android app.
 */
using SmartSolarMicrogrid.Api.DTOs;

namespace SmartSolarMicrogrid.Api.Services.Interfaces;

public interface ISlotService
{
    // date is a Sri Lanka calendar day (optional); stationId is optional.
    Task<List<SlotResponse>> ListAvailableAsync(string? stationId, DateTime? date);

    Task<List<SlotResponse>> GetAsync(string stationId);
}