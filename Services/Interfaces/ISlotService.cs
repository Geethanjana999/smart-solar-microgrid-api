/*
 * File        : ISlotService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Contract for energy booking slot management.
 */
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Services.Interfaces;

public interface ISlotService
{
    Task<List<EnergyBookingSlot>> GetAsync(string? stationId);

    Task<EnergyBookingSlot> CreateAsync(SlotRequest request);

    Task<EnergyBookingSlot> UpdateAsync(string id, SlotRequest request);

    Task DeleteAsync(string id);
}
