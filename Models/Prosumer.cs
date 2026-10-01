/*
 * File        : Prosumer.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Solar prosumer profile keyed by NIC (collection: Prosumer).
 */
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

    public bool DeactivationRequested { get; set; }
}
