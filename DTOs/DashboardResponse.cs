/*
 * File        : DashboardResponse.cs
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
