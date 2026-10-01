/*
 * File        : ReservationStatus.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Lifecycle states of an energy reservation.
 */
namespace SmartSolarMicrogrid.Api.Models;

public enum ReservationStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}
