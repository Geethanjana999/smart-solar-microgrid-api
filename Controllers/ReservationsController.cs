/*
 * File        : ReservationsController.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Prosumer endpoints for booking, viewing (current/history/search), updating and cancelling reservations.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Extensions;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Prosumer)]
[Route("api/reservations")]
public sealed class ReservationsController(IReservationService service) : ControllerBase
{
    // GET api/reservations?state=&from=&to=&stationId=&search= - own reservations with filters.
    [HttpGet]
    public async Task<ActionResult<List<ReservationResponse>>> Get([FromQuery] ReservationQuery query) =>
        Ok(await service.QueryAsync(User.GetNic(), query));

    // GET api/reservations/current - open bookings whose slot has not ended.
    [HttpGet("current")]
    public async Task<ActionResult<List<ReservationResponse>>> Current([FromQuery] ReservationQuery query)
    {
        query.Scope = "current";
        return Ok(await service.QueryAsync(User.GetNic(), query));
    }

    // GET api/reservations/pending - bookings still waiting for approval.
    [HttpGet("pending")]
    public async Task<ActionResult<List<ReservationResponse>>> Pending([FromQuery] ReservationQuery query)
    {
        query.State = "Pending";
        return Ok(await service.QueryAsync(User.GetNic(), query));
    }

    // GET api/reservations/history - completed, cancelled and expired bookings.
    [HttpGet("history")]
    public async Task<ActionResult<List<ReservationResponse>>> History([FromQuery] ReservationQuery query)
    {
        query.Scope = "history";
        return Ok(await service.QueryAsync(User.GetNic(), query));
    }

    // POST api/reservations - create a reservation (returns the summary).
    [HttpPost]
    public async Task<ActionResult<ReservationResponse>> Create(ReservationRequest request) =>
        Ok(await service.CreateAsync(User.GetNic(), request));

    // PUT api/reservations/{id} - change the reserved energy (returns the summary).
    [HttpPut("{id}")]
    public async Task<ActionResult<ReservationResponse>> Update(string id, ReservationUpdateRequest request) =>
        Ok(await service.UpdateAsync(id, User.GetNic(), request));

    // DELETE api/reservations/{id} - cancel a reservation (returns the summary).
    [HttpDelete("{id}")]
    public async Task<ActionResult<ReservationResponse>> Cancel(string id) =>
        Ok(await service.CancelAsync(id, User.GetNic()));
}
