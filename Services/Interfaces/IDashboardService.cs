/*
 * File        : IDashboardService.cs
 * Description : Contract for dashboard summary figures.
 */
using SmartSolarMicrogrid.Api.DTOs;

namespace SmartSolarMicrogrid.Api.Services.Interfaces;

public interface IDashboardService
{
    // nic == null returns system-wide counts (staff); otherwise the prosumer's own counts.
    Task<DashboardResponse> GetAsync(string? nic);
}
