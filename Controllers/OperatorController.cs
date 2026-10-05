/*
 * File        : OperatorController.cs
 * Description : Grid operator endpoints for QR verification and finalising energy transfers.
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartSolarMicrogrid.Api.Configuration;
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Services.Interfaces;

namespace SmartSolarMicrogrid.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.BackofficeOrOperator)]
[Route("api/operator")]
public sealed class OperatorController(IReservationService service) : ControllerBase
{
    // POST api/operator/verify-qr - check a scanned QR token against the server (approved reservations only).
    [HttpPost("verify-qr")]
    public async Task<ActionResult<ReservationResponse>> Verify(QrVerifyRequest request) =>
        Ok(await service.VerifyAsync(request.QrToken));

    // POST api/operator/reservations/{id}/finalize - mark the transfer as done.
    [HttpPost("reservations/{id}/finalize")]
    public async Task<ActionResult<ReservationResponse>> Finalize(string id) =>
        Ok(await service.FinalizeAsync(id, User.Identity?.Name ?? "operator"));
}
