/*
 * File        : UserService.cs
 * Description : Business rules for creating, listing and updating user accounts.
 */
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.Data;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Services;

public sealed class UserService(MongoContext db) : IUserService
{
    // Returns every user account.
    public Task<List<User>> GetAsync() => db.Users.Find(_ => true).ToListAsync();

    // Creates a Backoffice or Grid Operator user (prosumer logins are created through prosumer registration).
    public async Task<User> CreateAsync(UserRequest request)
    {
        if (!Roles.Staff.Contains(request.Role))
        {
            throw new InvalidOperationException("Role must be Backoffice or GridOperator.");
        }

        if (await db.Users.Find(x => x.Username == request.Username).AnyAsync())
        {
            throw new InvalidOperationException("Username already exists.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.Find(x => x.Email == email).AnyAsync())
        {
            throw new InvalidOperationException("Email already in use.");
        }

        var user = new User { Username = request.Username, Email = email, Role = request.Role };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, request.Password);
        await db.Users.InsertOneAsync(user);
        return user;
    }

    // Changes a user's role and active flag.
    public async Task<User> UpdateAsync(string id, UserUpdateRequest request)
    {
        var user = await db.Users.Find(x => x.Id == id).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("User not found.");

        if (user.Role == Roles.Prosumer || request.Role == Roles.Prosumer)
        {
            throw new InvalidOperationException("Prosumer accounts are managed through the prosumer endpoints.");
        }

        if (!Roles.Staff.Contains(request.Role))
        {
            throw new InvalidOperationException("Role must be Backoffice or GridOperator.");
        }

        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        await db.Users.ReplaceOneAsync(x => x.Id == id, user);
        return user;
    }
}
