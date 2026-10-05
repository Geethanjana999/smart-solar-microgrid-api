/*
 * File        : Reservation.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Energy booking made by a prosumer against a slot (collection: Reservations).
 */
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolarMicrogrid.Api.Models;

public enum ReservationStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}

public sealed class Reservation : Entity
{
    public string ProsumerNic { get; set; } = "";

    // The slot this booking reserved energy from.
    public string SlotId { get; set; } = "";

    // Station / node of the slot (the app calls this nodeId).
    public string NodeId { get; set; } = "";

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StartTime { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime EndTime { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal EnergyKwh { get; set; }

    // Price per kWh at the time of booking, so later price changes don't alter old bookings.
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal PricePerKwh { get; set; }

    public string Notes { get; set; } = "";

    [BsonRepresentation(BsonType.String)]
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
}