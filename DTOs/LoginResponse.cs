/*
 * File        : LoginResponse.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Token and identity details returned after a successful login.
 */
namespace SmartSolarMicrogrid.Api.DTOs;

public sealed record LoginResponse(string Token, DateTime ExpiresAt, string Username, string Role, string? ProsumerNic);
