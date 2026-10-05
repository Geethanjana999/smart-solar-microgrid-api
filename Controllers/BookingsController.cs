/*
 * File        : BookingsController.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Booking endpoints for the Android prosumer app. The NIC always comes from the JWT.
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
[Authorize(Roles = Roles.Prosumer)]
[MobileApiException]
[Route("api/bookings")]
public sealed class BookingsController(IBookingService service) : ControllerBase
{
    private string? Nic => User.FindFirstValue("nic");

    // GET api/bookings - the caller's own bookings (any ?nic= value is ignored).
    [HttpGet]
    public async Task<IActionResult> List() =>
        Nic is null ? Forbid() : Ok(await service.ListAsync(Nic));

    // GET api/bookings/dashboard - summary for the home screen (literal route, declared before {id}).
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() =>
        Nic is null ? Forbid() : Ok(await service.DashboardAsync(Nic));

    // GET api/bookings/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id) =>
        Nic is null ? Forbid() : Ok(await service.GetAsync(Nic, id));

    // POST api/bookings - reserves energy from a slot; booking starts as Pending.
    [HttpPost]
    public async Task<IActionResult> Create(BookingRequest request)
    {
        if (Nic is null) return Forbid();
        var created = await service.CreateAsync(Nic, request);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    // PUT api/bookings/{id} - change slot, energy or notes.
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, BookingRequest request) =>
        Nic is null ? Forbid() : Ok(await service.UpdateAsync(Nic, id, request));

    // DELETE api/bookings/{id} - cancels the booking and returns the energy to the slot.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(string id)
    {
        if (Nic is null) return Forbid();
        await service.CancelAsync(Nic, id);
        return NoContent();
    }
}