/*
 * File        : ReservationQuery.cs
 * Description : Filter and search options for listing reservations.
 */
namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class ReservationQuery
{
    // Pending, Confirmed, Cancelled or Completed.
    public string? State { get; set; }

    // "current" = open bookings whose slot has not ended; "history" = everything else.
    public string? Scope { get; set; }

    // Slot start must fall on or after / on or before these instants.
    public DateTime? From { get; set; }

    public DateTime? To { get; set; }

    public string? StationId { get; set; }

    // Case-insensitive match against station name, location, status or reservation id.
    public string? Search { get; set; }

    // Staff only: limit to one prosumer.
    public string? ProsumerNic { get; set; }
}
