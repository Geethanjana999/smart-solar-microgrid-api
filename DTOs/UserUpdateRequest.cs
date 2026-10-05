/*
 * File        : UserUpdateRequest.cs
 * Description : Payload for changing a user's role or active flag.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class UserUpdateRequest
{
    [Required]
    public string Role { get; set; } = "";

    public bool IsActive { get; set; } = true;
}
