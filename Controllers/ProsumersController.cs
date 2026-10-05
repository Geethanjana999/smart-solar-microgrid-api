/*
 * File        : ProsumersController.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Self-service prosumer endpoints for the Android app.
 */
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Filters;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[MobileApiException]
[Route("api/prosumers")]
public sealed class ProsumersController(IProsumerService service) : ControllerBase
{
    private string? Nic => User.FindFirstValue("nic");

    // POST api/prosumers/register - anonymous; the account stays inactive until Backoffice activates it.
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(ProsumerRequest request)
    {
        var prosumer = await service.RegisterAsync(request);
        return StatusCode(StatusCodes.Status201Created, prosumer);
    }

    // GET api/prosumers/me
    [Authorize(Roles = Roles.Prosumer)]
    [HttpGet("me")]
    public async Task<IActionResult> Me() =>
        Nic is null ? Forbid() : Ok(await service.GetAsync(Nic));

    // PUT api/prosumers/me - edit own profile.
    [Authorize(Roles = Roles.Prosumer)]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(ProsumerUpdateRequest request) =>
        Nic is null ? Forbid() : Ok(await service.UpdateAsync(Nic, request));

    // POST api/prosumers/me/deactivation-request - asks Backoffice to deactivate the account.
    [Authorize(Roles = Roles.Prosumer)]
    [HttpPost("me/deactivation-request")]
    public async Task<IActionResult> RequestDeactivation()
    {
        if (Nic is null) return Forbid();
        await service.RequestDeactivationAsync(Nic);
        return NoContent();
    }
}