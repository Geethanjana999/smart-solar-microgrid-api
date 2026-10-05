/*
 * File        : SlotsController.cs
 * Description : Endpoints for energy booking slot management.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/slots")]
public sealed class SlotsController(ISlotService service) : ControllerBase
{
    // GET api/slots - public list of open slots, optionally per station.
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<EnergyBookingSlot>>> Get([FromQuery] string? stationId) =>
        Ok(await service.GetAsync(stationId));

    // POST api/slots - Backoffice creates a slot.
    [Authorize(Roles = Roles.Backoffice)]
    [HttpPost]
    public async Task<ActionResult<EnergyBookingSlot>> Create(SlotRequest request) =>
        Ok(await service.CreateAsync(request));

    // PUT api/slots/{id} - update a slot.
    [Authorize(Roles = Roles.Backoffice)]
    [HttpPut("{id}")]
    public async Task<ActionResult<EnergyBookingSlot>> Update(string id, SlotRequest request) =>
        Ok(await service.UpdateAsync(id, request));

    // DELETE api/slots/{id} - delete (blocked while active reservations exist).
    [Authorize(Roles = Roles.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
