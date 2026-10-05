/*
 * File        : LoginRequest.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Credentials submitted to POST /api/auth/login.
 *               The identifier may be sent as "nic" (mobile app), "username" or "email" (web).
 */
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class LoginRequest : IValidatableObject
{
    // Web / Backoffice clients.
    [StringLength(254, ErrorMessage = "Username is too long.")]
    public string? Username { get; set; }

    // Mobile app sends the prosumer NIC.
    [StringLength(254, ErrorMessage = "NIC is too long.")]
    public string? Nic { get; set; }

    [StringLength(254, ErrorMessage = "Email is too long.")]
    public string? Email { get; set; }

    [Required(AllowEmptyStrings = false, ErrorMessage = "Password is required.")]
    [StringLength(128, ErrorMessage = "Password is too long.")]
    public string Password { get; set; } = "";

    // First non-empty value among username, nic and email.
    [JsonIgnore]
    public string Identifier =>
        FirstNonEmpty(Username, Nic, Email);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Identifier.Length == 0)
        {
            yield return new ValidationResult(
                "NIC, username or email is required.",
                [nameof(Username), nameof(Nic), nameof(Email)]);
        }
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? "";
}