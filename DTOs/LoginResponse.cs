/*
 * File        : LoginResponse.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Token and identity details returned after a successful login.
 */
namespace SmartSolarMicrogrid.Api.DTOs;

public sealed record UserResponse(string Username, string Email, string Role, string? ProsumerNic);

public sealed record LoginResponse(string Token, DateTime ExpiresAt, UserResponse User);