/*
 * File        : ClaimsPrincipalExtensions.cs
 * Description : Helper for reading the prosumer NIC claim from the JWT.
 */
using System.Security.Claims;

namespace SmartSolarMicrogrid.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Returns the NIC claim of the signed-in prosumer, or throws if the token has none.
    public static string GetNic(this ClaimsPrincipal user) =>
        user.FindFirstValue("nic") is { Length: > 0 } nic
            ? nic
            : throw new InvalidOperationException("Prosumer identity required.");
}
