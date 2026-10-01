/*
 * File        : IProsumerService.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Contract for prosumer registration, self-service actions and Backoffice management.
 */
using SmartSolarMicrogrid.Api.DTOs;
using SmartSolarMicrogrid.Api.Models;

namespace SmartSolarMicrogrid.Api.Services.Interfaces;

public interface IProsumerService
{
    Task<Prosumer> RegisterAsync(ProsumerRequest request);

    Task<Prosumer> CreateAsync(ProsumerRequest request);

    Task<List<Prosumer>> ListAsync(string? status);

    Task<Prosumer> GetAsync(string nic);

    Task<Prosumer> UpdateAsync(string nic, ProsumerUpdateRequest request);

    Task RequestDeactivationAsync(string nic);

    Task<Prosumer> ActivateAsync(string nic);

    Task<Prosumer> DeactivateAsync(string nic);
}
