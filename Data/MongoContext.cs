/*
 * File        : MongoContext.cs
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

    // Opens the MongoDB connection from the configured options.
    public MongoContext(IOptions<MongoDbOptions> options)
    {
        // Connect once; MongoClient is thread-safe and registered as a singleton.
        Database = new MongoClient(options.Value.ConnectionString).GetDatabase(options.Value.DatabaseName);
    }
}
