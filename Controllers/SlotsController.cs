/*
 * File        : SlotsController.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Slot catalogue for the Android prosumer app (read-only).
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.Filters;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Prosumer)]
[MobileApiException]
[Route("api/slots")]
public sealed class SlotsController(ISlotService service) : ControllerBase
{
    // GET api/slots?stationId=NODE-001&date=2026-10-12
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? stationId, [FromQuery] DateTime? date) =>
        Ok(await service.ListAvailableAsync(stationId, date));
}