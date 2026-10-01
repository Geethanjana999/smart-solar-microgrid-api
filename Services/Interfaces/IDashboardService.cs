/*
 * File        : IDashboardService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Contract for dashboard summary figures.
 */
using SmartSolarMicrogrid.Api.DTOs;

namespace SmartSolarMicrogrid.Api.Services.Interfaces;

public interface IDashboardService
{
    // nic == null returns system-wide counts (staff); otherwise the prosumer's own counts.
    Task<DashboardResponse> GetAsync(string? nic);
}
