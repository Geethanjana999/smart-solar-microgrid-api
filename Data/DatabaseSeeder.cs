/*
 * File        : DatabaseSeeder.cs
 * Description : Creates MongoDB indexes and inserts sample data on first start-up.
 */
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Data;

public sealed class DatabaseSeeder(MongoContext db)
{
    // Ensures indexes exist, then seeds sample users, prosumer, station and slot if the database is empty.
    public async Task SeedAsync()
    {
        await BackfillUserEmailsAsync();
        await CreateIndexesAsync();
        await BackfillBatterySlotsAsync();

        if (await db.Users.Find(_ => true).AnyAsync())
        {
            return;
        }

        var hasher = new PasswordHasher<User>();
        var users = new[]
        {
            new User { Username = "backoffice", Email = "backoffice@smartsolar.lk", Role = Roles.Backoffice },
            new User { Username = "operator", Email = "operator@smartsolar.lk", Role = Roles.GridOperator },
            new User { Username = "199012345678", Email = "prosumer@example.com", Role = Roles.Prosumer, ProsumerNic = "199012345678" }
        };

        foreach (var user in users)
        {
            user.PasswordHash = hasher.HashPassword(user, "Password123!");
            await db.Users.InsertOneAsync(user);
        }

        
        // await SeedSampleDataAsync();
    }

    // Inserts sample prosumers, stations, slots and reservations in every status so the clients have data to show.
    private async Task SeedSampleDataAsync()
    {
        var hasher = new PasswordHasher<User>();
        var prosumers = new[]
        {
            new Prosumer { Nic = "199012345678", FullName = "Sample Prosumer", Email = "prosumer@example.com", Phone = "0771234567", Address = "Colombo" },
            new Prosumer { Nic = "199256789012", FullName = "Nimal Perera", Email = "nimal@example.com", Phone = "0712345678", Address = "Kandy" },
            new Prosumer { Nic = "198834567890", FullName = "Kamala Silva", Email = "kamala@example.com", Phone = "0763456789", Address = "Galle", IsActive = false, PendingActivation = true }
        };
        await db.Prosumers.InsertManyAsync(prosumers);

        // Extra logins for the second and third prosumers (the first was created with the base users).
        foreach (var p in prosumers.Skip(1))
        {
            var user = new User { Username = p.Nic, Email = p.Email, Role = Roles.Prosumer, ProsumerNic = p.Nic, IsActive = p.IsActive };
            user.PasswordHash = hasher.HashPassword(user, "Password123!");
            await db.Users.InsertOneAsync(user);
        }

        var stations = new[]
        {
            new SolarStationInfo { Name = "Colombo Solar Hub", Location = "Colombo", Latitude = 6.9271, Longitude = 79.8612, CapacityKwh = 500, BatteryStorageSlots = 12, AvailableBatterySlots = 12 },
            new SolarStationInfo { Name = "Kandy Hill Microgrid", Location = "Kandy", Latitude = 7.2906, Longitude = 80.6337, CapacityKwh = 300, BatteryStorageSlots = 8, AvailableBatterySlots = 6 },
            new SolarStationInfo { Name = "Galle Coastal Node", Location = "Galle", Latitude = 6.0535, Longitude = 80.2210, CapacityKwh = 250, BatteryStorageSlots = 6, AvailableBatterySlots = 6 }
        };
        await db.Stations.InsertManyAsync(stations);

        var today = DateTime.UtcNow.Date;
        var slots = new List<EnergyBookingSlot>();
        foreach (var station in stations)
        {
            // One slot already in the past (for history) and slots on each of the next three days.
            slots.Add(NewSlot(station, today.AddDays(-2).AddHours(10), 100, 40m));
            for (var day = 1; day <= 3; day++)
            {
                slots.Add(NewSlot(station, today.AddDays(day).AddHours(10), 100, 42.50m));
            }
        }

        // Reservations in each status; slot energy is reduced to match what is reserved.
        var colomboSlots = slots.Where(s => s.StationId == stations[0].Id).OrderBy(s => s.StartTime).ToList();
        var reservations = new[]
        {
            NewReservation("199012345678", colomboSlots[0], 20, ReservationStatus.Completed),
            NewReservation("199012345678", colomboSlots[2], 15, ReservationStatus.Confirmed),
            NewReservation("199256789012", colomboSlots[2], 10, ReservationStatus.Pending),
            NewReservation("199256789012", colomboSlots[3], 5, ReservationStatus.Cancelled)
        };
        foreach (var r in reservations.Where(r => r.Status is ReservationStatus.Confirmed or ReservationStatus.Pending or ReservationStatus.Completed))
        {
            slots.First(s => s.Id == r.SlotId).AvailableKwh -= r.EnergyKwh;
        }

        await db.Slots.InsertManyAsync(slots);
        await db.Reservations.InsertManyAsync(reservations);
    }

    // Builds a two-hour slot starting at the given instant.
    private static EnergyBookingSlot NewSlot(SolarStationInfo station, DateTime start, decimal kwh, decimal price) => new()
    {
        StationId = station.Id,
        StartTime = start,
        EndTime = start.AddHours(2),
        AvailableKwh = kwh,
        PricePerKwh = price
    };

    // Builds a reservation for a prosumer against a slot.
    private static EnergyReservation NewReservation(string nic, EnergyBookingSlot slot, decimal kwh, ReservationStatus status) => new()
    {
        ProsumerNic = nic,
        StationId = slot.StationId,
        SlotId = slot.Id,
        EnergyKwh = kwh,
        Status = status,
        FinalizedAt = status == ReservationStatus.Completed ? slot.EndTime : null,
        FinalizedBy = status == ReservationStatus.Completed ? "operator" : null
    };

    // Users created before email login existed get an email (prosumer profile email, else username@smartsolar.lk).
    private async Task BackfillUserEmailsAsync()
    {
        var legacy = await db.Users.Find(Builders<User>.Filter.Or(
            Builders<User>.Filter.Exists(x => x.Email, false),
            Builders<User>.Filter.Eq(x => x.Email, ""))).ToListAsync();
        foreach (var user in legacy)
        {
            var prosumer = user.ProsumerNic == null
                ? null
                : await db.Prosumers.Find(x => x.Nic == user.ProsumerNic).FirstOrDefaultAsync();
            user.Email = (prosumer?.Email ?? $"{user.Username}@smartsolar.lk").Trim().ToLowerInvariant();
            await db.Users.ReplaceOneAsync(x => x.Id == user.Id, user);
        }
    }

    // Stations created before AvailableBatterySlots existed get all their slots marked available.
    private async Task BackfillBatterySlotsAsync()
    {
        var legacy = await db.Stations.Find(Builders<SolarStationInfo>.Filter.Exists("AvailableBatterySlots", false)).ToListAsync();
        foreach (var station in legacy)
        {
            station.AvailableBatterySlots = station.BatteryStorageSlots;
            await db.Stations.ReplaceOneAsync(x => x.Id == station.Id, station);
        }
    }

    // Creates the unique and lookup indexes used by the services.
    private async Task CreateIndexesAsync()
    {
        await db.Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(x => x.Username),
            new CreateIndexOptions { Unique = true }));

        await db.Users.Indexes.CreateOneAsync(new CreateIndexModel<User>(
            Builders<User>.IndexKeys.Ascending(x => x.Email),
            new CreateIndexOptions { Unique = true }));

        await db.Prosumers.Indexes.CreateOneAsync(new CreateIndexModel<Prosumer>(
            Builders<Prosumer>.IndexKeys.Ascending(x => x.Nic),
            new CreateIndexOptions { Unique = true }));

        await db.Slots.Indexes.CreateOneAsync(new CreateIndexModel<EnergyBookingSlot>(
            Builders<EnergyBookingSlot>.IndexKeys.Ascending(x => x.StationId).Ascending(x => x.StartTime)));

        await db.Reservations.Indexes.CreateOneAsync(new CreateIndexModel<EnergyReservation>(
            Builders<EnergyReservation>.IndexKeys.Ascending(x => x.ProsumerNic).Ascending(x => x.Status)));
    }
}
