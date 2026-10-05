/*
 * File        : StationsController.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Endpoints for microgrid stations: Backoffice management, operator battery slots, public map queries.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[Route("api/stations")]
[Route("api/nodes")] // Alias for frontend
public sealed class StationsController(IStationService service) : ControllerBase
{
    // GET api/stations - public list of stations (with GPS) for the map screens.
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<SolarStationInfo>>> Get([FromQuery] bool activeOnly = true) =>
        Ok(await service.GetAsync(activeOnly));

    // GET api/stations/{id}/slots - frontend alias for station slots.
    [AllowAnonymous]
    [HttpGet("{id}/slots")]
    public async Task<ActionResult<List<SlotResponse>>> GetSlots(string id, [FromServices] ISlotService slotService) =>
        Ok(await slotService.GetAsync(id));

    // GET api/stations/{id} - specific station (for frontend compatibility).
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult<SolarStationInfo>> GetById(string id)
    {
        var stations = await service.GetAsync(false);
        var station = stations.FirstOrDefault(s => s.Id == id);
        if (station == null) return NotFound();
        return Ok(station);
    }

    // GET api/stations/nearby?latitude=&longitude=&radiusKm= - stations around the caller, nearest first.
    [AllowAnonymous]
    [HttpGet("nearby")]
    public async Task<ActionResult<List<NearbyStationResponse>>> Nearby(
        [FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radiusKm = 25) =>
        Ok(await service.GetNearbyAsync(latitude, longitude, radiusKm));

    // POST api/stations - Backoffice creates a station.
    [Authorize(Roles = Roles.Backoffice)]
    [HttpPost]
    [ProducesResponseType(typeof(SolarStationInfo), StatusCodes.Status201Created)]
    public async Task<ActionResult<SolarStationInfo>> Create(StationRequest request)
    {
        var station = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = station.Id }, station);
    }

    // PUT api/stations/{id} - Backoffice updates a station.
    [Authorize(Roles = Roles.Backoffice)]
    [HttpPut("{id}")]
    public async Task<ActionResult<SolarStationInfo>> Update(string id, StationRequest request) =>
        Ok(await service.UpdateAsync(id, request));

    // PUT api/stations/{id}/battery-slots - operator/Backoffice sets the free battery slots.
    [Authorize(Roles = Roles.BackofficeOrOperator)]
    [HttpPut("{id}/battery-slots")]
    public async Task<ActionResult<SolarStationInfo>> UpdateBatterySlots(string id, BatterySlotsRequest request) =>
        Ok(await service.UpdateBatterySlotsAsync(id, request));

    // PUT api/stations/{id}/schedule - Backoffice sets the operational schedule.
    [Authorize(Roles = Roles.Backoffice)]
    [HttpPut("{id}/schedule")]
    public async Task<ActionResult<SolarStationInfo>> UpdateSchedule(string id, NodeScheduleRequest request) =>
        Ok(await service.UpdateScheduleAsync(id, request));

    // DELETE api/stations/{id} - Backoffice deactivates (blocked while active reservations exist).
    [Authorize(Roles = Roles.Backoffice)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [HttpDelete("{id}")]
    [HttpPatch("{id}/deactivate")] // Alias for frontend
    public async Task<IActionResult> Deactivate(string id)
    {
        await service.DeactivateAsync(id);
        return NoContent();
    }

    // POST api/stations/{id}/activate - Backoffice reactivates a deactivated station.
    [Authorize(Roles = Roles.Backoffice)]
    [HttpPost("{id}/activate")]
    public async Task<ActionResult<SolarStationInfo>> Activate(string id) => Ok(await service.ActivateAsync(id));
}
