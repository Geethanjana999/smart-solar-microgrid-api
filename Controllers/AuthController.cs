/*
 * File        : AuthController.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : REST endpoint for logging in.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    // POST api/auth/login - anonymous; returns a JWT.
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request) =>
        Ok(await service.LoginAsync(request));

    // GET api/auth/me - validates token and returns user details for frontend refresh.
    [Authorize]
    [HttpGet("me")]
    public ActionResult<UserResponse> Me()
    {
        var username = User.FindFirstValue(System.Security.Claims.ClaimTypes.Name) ?? "";
        var email = User.FindFirstValue(System.Security.Claims.ClaimTypes.Email) ?? "";
        var role = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) ?? "";
        var nic = User.FindFirstValue("nic");
        if (string.IsNullOrWhiteSpace(nic)) nic = null;
        
        return Ok(new UserResponse(username, email, role, nic));
    }

    // POST api/auth/logout - handles frontend logout requests.
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() => Ok();
}
