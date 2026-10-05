/*
 * File        : LoginRequest.cs
 * Description : Credentials submitted to POST /api/auth/login.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class LoginRequest
{
    [Required]
    public string Username { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";
}
