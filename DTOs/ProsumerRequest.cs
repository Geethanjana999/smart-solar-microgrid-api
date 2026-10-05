/*
 * File        : ProsumerRequest.cs
 * Description : Payload for prosumer self-registration (NIC is the primary key).
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class ProsumerRequest
{
    [Required]
    public string Nic { get; set; } = "";

    [Required]
    public string FullName { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Phone { get; set; } = "";

    public string Address { get; set; } = "";

    public string? Password { get; set; }
}
