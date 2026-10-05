/*
 * File        : AuthController.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : REST endpoints for logging in, token validation and logout.
 */
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService service) : ControllerBase
{
    // POST api/auth/login - anonymous; returns a JWT, or 401 when credentials/account state are rejected.
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        try
        {
            return Ok(await service.LoginAsync(request));
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    // GET api/auth/me - validates the token and returns user details for frontend refresh.
    [Authorize]
    [HttpGet("me")]
    public ActionResult<UserResponse> Me()
    {
        var username = User.FindFirstValue(ClaimTypes.Name) ?? "";
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "";
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var nic = User.FindFirstValue("nic");
        if (string.IsNullOrWhiteSpace(nic)) nic = null;

        return Ok(new UserResponse(username, email, role, nic));
    }

    // POST api/auth/logout - JWTs are stateless, so the client simply discards the token.
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() => Ok();
}