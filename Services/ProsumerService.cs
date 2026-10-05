/*
 * File        : ProsumerService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Prosumer rules: registration with Backoffice activation, profile edits, deactivation and reactivation.
 */
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.Data;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class ProsumerService(MongoContext db) : IProsumerService
{
    // Self-registration from the mobile app: the account stays inactive until Backoffice activates it.
    public Task<Prosumer> RegisterAsync(ProsumerRequest request) => InsertAsync(request, active: false);

    // Backoffice creates a prosumer who can log in immediately.
    public Task<Prosumer> CreateAsync(ProsumerRequest request) => InsertAsync(request, active: true);

    // Lists prosumers; status = pending | active | inactive | deactivation-requested (or null for all).
    public Task<List<Prosumer>> ListAsync(string? status)
    {
        var filter = status?.ToLowerInvariant() switch
        {
            "pending" => Builders<Prosumer>.Filter.Eq(x => x.PendingActivation, true),
            "active" => Builders<Prosumer>.Filter.Eq(x => x.IsActive, true),
            "inactive" => Builders<Prosumer>.Filter.Eq(x => x.IsActive, false),
            "deactivation-requested" => Builders<Prosumer>.Filter.Eq(x => x.DeactivationRequested, true),
            null or "" => Builders<Prosumer>.Filter.Empty,
            _ => throw new InvalidOperationException("Unknown status filter.")
        };

        return db.Prosumers.Find(filter).SortByDescending(x => x.CreatedAt).ToListAsync();
    }

    // Looks up a prosumer by NIC or Id.
    public async Task<Prosumer> GetAsync(string nicOrId)
    {
        var filter = Builders<Prosumer>.Filter.Eq(x => x.Nic, nicOrId);
        if (MongoDB.Bson.ObjectId.TryParse(nicOrId, out _))
        {
            filter = Builders<Prosumer>.Filter.Or(
                filter,
                Builders<Prosumer>.Filter.Eq(x => x.Id, nicOrId));
        }

        return await db.Prosumers.Find(filter).FirstOrDefaultAsync()
               ?? throw new KeyNotFoundException("Prosumer not found.");
    }

    // Updates the editable profile fields of the prosumer.
    public async Task<Prosumer> UpdateAsync(string nicOrId, ProsumerUpdateRequest request)
    {
        var prosumer = await GetAsync(nicOrId);

        // The prosumer's login email follows the profile email; it must stay unique.
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.Find(x => x.Email == email && x.ProsumerNic != prosumer.Nic).AnyAsync())
        {
            throw new InvalidOperationException("Email already in use.");
        }

        await db.Users.UpdateManyAsync(
            x => x.ProsumerNic == prosumer.Nic && x.Role == Roles.Prosumer,
            Builders<User>.Update.Set(x => x.Email, email).Set(x => x.UpdatedAt, DateTime.UtcNow));

        prosumer.FullName = request.FullName;
        prosumer.Email = request.Email;
        prosumer.Phone = request.Phone;
        prosumer.Address = request.Address;
        prosumer.UpdatedAt = DateTime.UtcNow;
        await db.Prosumers.ReplaceOneAsync(x => x.Nic == prosumer.Nic, prosumer);
        return prosumer;
    }

    // Flags the account so a Backoffice user can review the deactivation request.
    public async Task RequestDeactivationAsync(string nicOrId)
    {
        var prosumer = await GetAsync(nicOrId);
        prosumer.DeactivationRequested = true;
        prosumer.UpdatedAt = DateTime.UtcNow;
        await db.Prosumers.ReplaceOneAsync(x => x.Nic == prosumer.Nic, prosumer);
    }

    // Backoffice only: activates a pending or deactivated account and re-enables its login.
    public async Task<Prosumer> ActivateAsync(string nicOrId) => await SetActiveAsync(nicOrId, true);

    // Backoffice only: deactivates the account unless the prosumer still has open reservations.
    public async Task<Prosumer> DeactivateAsync(string nicOrId)
    {
        var prosumer = await GetAsync(nicOrId);
        var hasOpenReservations = await db.Reservations
            .Find(x => x.ProsumerNic == prosumer.Nic && (x.Status == ReservationStatus.Pending || x.Status == ReservationStatus.Confirmed))
            .AnyAsync();
        if (hasOpenReservations)
        {
            throw new InvalidOperationException("Prosumer has active reservations.");
        }

        return await SetActiveAsync(nicOrId, false);
    }

    // Inserts the prosumer profile and its login (username = NIC); rejects duplicate NICs.
    private async Task<Prosumer> InsertAsync(ProsumerRequest request, bool active)
    {
        if (await db.Prosumers.Find(x => x.Nic == request.Nic).AnyAsync())
        {
            throw new InvalidOperationException("NIC already registered.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.Find(x => x.Email == email).AnyAsync())
        {
            throw new InvalidOperationException("Email already in use.");
        }

        var prosumer = new Prosumer
        {
            Nic = request.Nic,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            IsActive = active,
            PendingActivation = !active
        };

        var user = new User { Username = request.Nic, Email = email, Role = Roles.Prosumer, ProsumerNic = request.Nic, IsActive = active };
        var password = string.IsNullOrWhiteSpace(request.Password) ? "Password123!" : request.Password;
        if (password.Length < 6) throw new InvalidOperationException("Password must be at least 6 characters.");
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);

        await db.Prosumers.InsertOneAsync(prosumer);
        await db.Users.InsertOneAsync(user);
        return prosumer;
    }

    // Sets the active flag on the profile and keeps the matching login in step.
    private async Task<Prosumer> SetActiveAsync(string nicOrId, bool active)
    {
        var prosumer = await GetAsync(nicOrId);
        prosumer.IsActive = active;
        prosumer.PendingActivation = false;
        prosumer.DeactivationRequested = false;
        prosumer.UpdatedAt = DateTime.UtcNow;
        await db.Prosumers.ReplaceOneAsync(x => x.Nic == prosumer.Nic, prosumer);

        await db.Users.UpdateManyAsync(
            x => x.ProsumerNic == prosumer.Nic && x.Role == Roles.Prosumer,
            Builders<User>.Update.Set(x => x.IsActive, active).Set(x => x.UpdatedAt, DateTime.UtcNow));
        return prosumer;
    }
}
