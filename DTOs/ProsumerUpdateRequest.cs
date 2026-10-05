/*
 * File        : ProsumerUpdateRequest.cs
 * Description : Payload for a prosumer editing their own profile.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class ProsumerUpdateRequest
{
    [Required]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";
}
