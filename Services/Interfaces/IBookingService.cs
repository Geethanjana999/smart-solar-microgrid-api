/*
 * File        : IBookingService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Booking operations available to a prosumer from the Android app.
 */
using SmartSolarMicrogrid.Api.DTOs;

namespace SmartSolarMicrogrid.Api.Services.Interfaces;

public interface IBookingService
{
    Task<BookingResponse> CreateAsync(string nic, BookingRequest request);

    Task<List<BookingResponse>> ListAsync(string nic);

    Task<BookingResponse> GetAsync(string nic, string id);

    Task<BookingResponse> UpdateAsync(string nic, string id, BookingRequest request);

    Task CancelAsync(string nic, string id);

    Task<DashboardResponse> DashboardAsync(string nic);
}