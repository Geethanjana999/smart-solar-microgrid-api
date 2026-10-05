/*
 * File        : User.cs
 * Description : Web/mobile login account (collection: Users).
 */
using System.Text.Json.Serialization;

namespace SmartSolarMicrogrid.Api.Models;

public sealed class User : Entity
{
    public string Username { get; set; } = "";

    // Login identifier; stored trimmed and lower-case, unique across users.
    public string Email { get; set; } = "";

    // Never serialised into API responses.
    [JsonIgnore]
    public string PasswordHash { get; set; } = "";

    public string Role { get; set; } = "";

    // Set only for Prosumer accounts; links the login to a Prosumer document.
    public string? ProsumerNic { get; set; }

    public bool IsActive { get; set; } = true;

    [MongoDB.Bson.Serialization.Attributes.BsonIgnore]
    public string Status => IsActive ? "Active" : "Inactive";
}
