/*
 * File        : DashboardResponse.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Summary counts shown on the web and mobile dashboards.
 */
namespace SmartSolarMicrogrid.Api.DTOs;

// For prosumers the reservation counts are their own; for staff they cover all prosumers.
public sealed record DashboardResponse(
    int ActiveStations,
    int AvailableSlots,
    int PendingReservations,
    int ApprovedFutureReservations,
    int CompletedReservations,
    decimal AvailableEnergyKwh);
