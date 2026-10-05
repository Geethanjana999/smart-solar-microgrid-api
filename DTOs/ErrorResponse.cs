/*
 * File        : ErrorResponse.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : JSON error body returned for every failed request (400, 401, 403, 404, 500).
 */
namespace SmartSolarMicrogrid.Api.DTOs;

public sealed record ErrorResponse(int Status, string Error)
{
    // Alias to match frontend expectations (err.response.data.message)
    public string Message => Error;
}
