/*
 * File        : IAuthService.cs
 * Description : Contract for authenticating users and issuing JWT tokens.
 */
using SmartSolarMicrogrid.Api.DTOs;

namespace SmartSolarMicrogrid.Api.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
