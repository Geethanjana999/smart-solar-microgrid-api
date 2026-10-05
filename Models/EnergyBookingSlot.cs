/*
 * File        : EnergyBookingSlot.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Bookable energy time slot at a station (collection: EnergyBookingSlots).
 */
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolarMicrogrid.Api.Models;

public sealed class EnergyBookingSlot : Entity
{
    public string StationId { get; set; } = "";

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime StartTime { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime EndTime { get; set; }

    // Energy still open for booking in this slot.
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal AvailableKwh { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal PricePerKwh { get; set; }

    public bool IsActive { get; set; } = true;

    // Derived; not stored.
    [BsonIgnore]
    public bool IsBookable => IsActive && AvailableKwh > 0 && StartTime > DateTime.UtcNow;
}