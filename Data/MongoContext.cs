/*
 * File        : MongoContext.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Holds the MongoDB database handle and typed collection accessors.
 */
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Data;

public sealed class MongoContext
{
    public IMongoDatabase Database { get; }

    public IMongoCollection<User> Users => Database.GetCollection<User>("Users");

    public IMongoCollection<Prosumer> Prosumers => Database.GetCollection<Prosumer>("Prosumer");

    public IMongoCollection<SolarStationInfo> Stations => Database.GetCollection<SolarStationInfo>("SolarStationInfo");

    public IMongoCollection<EnergyBookingSlot> Slots => Database.GetCollection<EnergyBookingSlot>("EnergyBookingSlots");

    public IMongoCollection<EnergyReservation> Reservations => Database.GetCollection<EnergyReservation>("EnergyReservations");

    // Legacy Android bookings use the original reservation shape and collection.
    public IMongoCollection<Reservation> Bookings => Database.GetCollection<Reservation>("Reservations");

    // Compatibility alias for the legacy booking service.
    public IMongoCollection<EnergyBookingSlot> EnergyBookingSlots => Slots;

    // Opens the MongoDB connection from the configured options.
    public MongoContext(IOptions<MongoDbOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Value);

        // Connect once; MongoClient is thread-safe and registered as a singleton.
        var client = new MongoClient(options.Value.ConnectionString);
        Database = client.GetDatabase(options.Value.DatabaseName);
    }
}