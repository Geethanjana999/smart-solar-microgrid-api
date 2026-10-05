/*
 * File        : Prosumer.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Solar prosumer profile keyed by NIC (collection: Prosumer).
 */
using MongoDB.Bson.Serialization.Attributes;

namespace SmartSolarMicrogrid.Api.Models;

public sealed class Prosumer : Entity
{
    public string Nic { get; set; } = "";

    public string FullName { get; set; } = "";

    public string Email { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";

    public bool IsActive { get; set; } = true;

    // True after self-registration until a Backoffice user activates the account.
    public bool PendingActivation { get; set; }

    // True when the prosumer asked Backoffice to deactivate the account.
    public bool DeactivationRequested { get; set; }

    // Derived display status; not stored in MongoDB.
    // Pending > Deactivation-Requested > Active > Inactive (matches ProsumerService.ListAsync filters).
    [BsonIgnore]
    public string Status
    {
        get
        {
            if (PendingActivation) return "Pending";
            if (DeactivationRequested) return "Deactivation-Requested";
            return IsActive ? "Active" : "Inactive";
        }
    }
}