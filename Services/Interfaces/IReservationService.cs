/*
 * File        : IReservationService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Contract for reservation booking, approval, views and operator QR verification.
 */
using SmartSolarMicrogrid.Api.DTOs;

namespace SmartSolarMicrogrid.Api.Services.Interfaces;

public interface IReservationService
{
    Task<ReservationResponse> CreateAsync(string nic, ReservationRequest request);

    // nic == null means the caller is staff and may edit any prosumer's reservation.
    Task<ReservationResponse> UpdateAsync(string id, string? nic, ReservationUpdateRequest request);

    Task<ReservationResponse> CancelAsync(string id, string? nic);

    // nic == null lists reservations of every prosumer (staff).
    Task<List<ReservationResponse>> QueryAsync(string? nic, ReservationQuery query);

    Task<ReservationResponse> ApproveAsync(string id);

    Task<ReservationResponse> VerifyAsync(string token);

    Task<ReservationResponse> FinalizeAsync(string id, string operatorName);
}
