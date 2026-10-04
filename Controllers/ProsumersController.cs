/*
 * File        : ProsumersController.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Prosumer registration, self-service profile endpoints and Backoffice prosumer management.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Extensions;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/prosumers")]
public sealed class ProsumersController(IProsumerService service) : ControllerBase
{
    // POST api/prosumers/register - anonymous self-registration; account awaits Backoffice activation.
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<Prosumer>> Register(ProsumerRequest request) =>
        Ok(await service.RegisterAsync(request));

    // GET api/prosumers/me - the signed-in prosumer's profile.
    [Authorize(Roles = Roles.Prosumer)]
    [HttpGet("me")]
    public async Task<ActionResult<Prosumer>> Me() => Ok(await service.GetAsync(User.GetNic()));

    // PUT api/prosumers/me - edit own profile.
    [Authorize(Roles = Roles.Prosumer)]
    [HttpPut("me")]
    public async Task<ActionResult<Prosumer>> UpdateMe(ProsumerUpdateRequest request) =>
        Ok(await service.UpdateAsync(User.GetNic(), request));

    // POST api/prosumers/me/deactivation-request - ask Backoffice to deactivate the account.
    [Authorize(Roles = Roles.Prosumer)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpPost("me/deactivation-request")]
    public async Task<IActionResult> RequestDeactivation()
    {
        await service.RequestDeactivationAsync(User.GetNic());
        return NoContent();
    }

    // GET api/prosumers?status= - Backoffice list (pending | active | inactive | deactivation-requested).
    [Authorize(Roles = Roles.Backoffice)]
    [HttpGet]
    public async Task<ActionResult<List<Prosumer>>> List([FromQuery] string? status) =>
        Ok(await service.ListAsync(status));

    // GET api/prosumers/{nic} - Backoffice/operator lookup by NIC.
    [Authorize(Roles = Roles.BackofficeOrOperator)]
    [HttpGet("{nic}")]
    public async Task<ActionResult<Prosumer>> Get(string nic) => Ok(await service.GetAsync(nic));

    // POST api/prosumers - Backoffice creates an already-active prosumer.
    [Authorize(Roles = Roles.Backoffice)]
    [HttpPost]
    [ProducesResponseType(typeof(Prosumer), StatusCodes.Status201Created)]
    public async Task<ActionResult<Prosumer>> Create(ProsumerRequest request)
    {
        var prosumer = await service.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { nic = prosumer.Nic }, prosumer);
    }

    // PUT api/prosumers/{nic} - Backoffice edits a profile.
    [Authorize(Roles = Roles.Backoffice)]
    [HttpPut("{nic}")]
    public async Task<ActionResult<Prosumer>> Update(string nic, ProsumerUpdateRequest request) =>
        Ok(await service.UpdateAsync(nic, request));

    // POST api/prosumers/{nic}/activate - approve a pending account or reactivate a deactivated one.
    [Authorize(Roles = Roles.Backoffice)]
    [HttpPost("{nic}/activate")]
    [HttpPatch("{nic}/reactivate")]
    [HttpPost("/api/activations/{nic}/approve")] // Alias for frontend
    public async Task<ActionResult<Prosumer>> Activate(string nic) => Ok(await service.ActivateAsync(nic));

    // POST api/prosumers/{nic}/deactivate - deactivate (blocked while reservations are open).
    [Authorize(Roles = Roles.Backoffice)]
    [HttpPost("{nic}/deactivate")]
    [HttpPatch("{nic}/deactivate")]
    [HttpPost("/api/activations/{nic}/reject")] // Alias for frontend
    public async Task<ActionResult<Prosumer>> Deactivate(string nic) => Ok(await service.DeactivateAsync(nic));

    // GET /api/activations/pending - Alias for frontend pending queue
    [Authorize(Roles = Roles.Backoffice)]
    [HttpGet("/api/activations/pending")]
    public async Task<ActionResult<List<Prosumer>>> PendingActivations() =>
        Ok(await service.ListAsync("pending"));
}
