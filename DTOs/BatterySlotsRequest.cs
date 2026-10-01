/*
 * File        : BatterySlotsRequest.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Payload for a grid operator updating how many battery slots are free.
 */
using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class BatterySlotsRequest
{
    [Range(0, int.MaxValue)]
    public int AvailableBatterySlots { get; set; }
}
