/*
 * File        : DashboardController.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Dashboard summary endpoint for all signed-in roles.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Extensions;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.All)]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardService service) : ControllerBase
{
    // GET api/dashboard - own counts for prosumers, system-wide counts for staff.
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get() =>
        Ok(await service.GetAsync(User.IsInRole(Roles.Prosumer) ? User.GetNic() : null));
}
