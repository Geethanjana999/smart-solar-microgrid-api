/*
 * File        : StaffReservationsController.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Backoffice/operator endpoints for managing and approving reservations on the web app.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.BackofficeOrOperator)]
[Route("api/staff/reservations")]
[Route("api/bookings")] // Alias for frontend compatibility
public sealed class StaffReservationsController(IReservationService service) : ControllerBase
{
    // GET api/staff/reservations?prosumerNic=&state=&scope=... - all reservations with filters.
    [HttpGet]
    public async Task<ActionResult<List<ReservationResponse>>> Get([FromQuery] ReservationQuery query) =>
        Ok(await service.QueryAsync(null, query));

    // GET api/bookings/history - Alias for history scope for the frontend
    [HttpGet("history")]
    public async Task<ActionResult<List<ReservationResponse>>> GetHistory([FromQuery] ReservationQuery query)
    {
        query.Scope = "history";
        return Ok(await service.QueryAsync(null, query));
    }

    // POST api/staff/reservations - book on behalf of a prosumer.
    [HttpPost]
    public async Task<ActionResult<ReservationResponse>> Create(StaffReservationRequest request) =>
        Ok(await service.CreateAsync(
            request.ProsumerNic,
            new ReservationRequest { SlotId = request.SlotId, EnergyKwh = request.EnergyKwh }));

    // PUT api/staff/reservations/{id} - change the reserved energy.
    [HttpPut("{id}")]
    public async Task<ActionResult<ReservationResponse>> Update(string id, ReservationUpdateRequest request) =>
        Ok(await service.UpdateAsync(id, null, request));

    // DELETE api/staff/reservations/{id} - cancel a reservation.
    [HttpDelete("{id}")]
    [HttpPatch("{id}/cancel")] // Alias for frontend compatibility
    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<ReservationResponse>> Cancel(string id) =>
        Ok(await service.CancelAsync(id, null));

    // POST api/staff/reservations/{id}/approve - approve a pending reservation (releases the QR code).
    [HttpPost("{id}/approve")]
    [HttpPatch("{id}/approve")] // Alias for frontend compatibility
    public async Task<ActionResult<ReservationResponse>> Approve(string id) =>
        Ok(await service.ApproveAsync(id));
}
