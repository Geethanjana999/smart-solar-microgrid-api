/*
 * File        : UserRequest.cs
 * Description : Payload for creating a web/mobile user account.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class UserRequest
{
    [Required, MinLength(3)]
    public string Username { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(6)]
    public string Password { get; set; } = "";

    [Required]
    public string Role { get; set; } = "";
}
