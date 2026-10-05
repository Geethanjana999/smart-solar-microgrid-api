/*
 * File        : SolarStationInfo.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Microgrid node / solar hub (collection: SolarStationInfo).
 */
namespace SmartSolarMicrogrid.Api.Models;

public sealed class SolarStationInfo : Entity
{
    public string Name { get; set; } = "";

    public string Location { get; set; } = "";

    public string NodeCode { get; set; } = "";

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public decimal CapacityKwh { get; set; }

    // Total battery storage slots installed at the station.
    public int BatteryStorageSlots { get; set; }

    // Battery slots currently free; maintained by grid operators.
    public int AvailableBatterySlots { get; set; }

    public OperatingSchedule Schedule { get; set; } = new();

    public bool IsActive { get; set; } = true;

    [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
    public string Status => IsActive ? "Active" : "Inactive";
    
    // Frontend aliases
    [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
    public int BatterySlotsCount => BatteryStorageSlots;

    [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
    public int AvailableSlotsCount => AvailableBatterySlots;
}
