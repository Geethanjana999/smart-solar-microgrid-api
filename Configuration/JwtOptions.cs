/*
 * File        : JwtOptions.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Strongly typed settings used to issue and validate JWT bearer tokens.
 */
namespace SmartSolarMicrogrid.Api.Configuration;

public sealed class JwtOptions
{
    public string Key { get; set; } = "CHANGE_THIS_DEVELOPMENT_KEY_TO_A_LONG_RANDOM_SECRET_2026";

    public string Issuer { get; set; } = "SmartSolarMicrogrid";

    public string Audience { get; set; } = "SmartSolarClients";

    public int ExpiryMinutes { get; set; } = 480;
}
