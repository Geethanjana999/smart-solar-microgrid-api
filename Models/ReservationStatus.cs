/*
 * File        : ReservationStatus.cs
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
