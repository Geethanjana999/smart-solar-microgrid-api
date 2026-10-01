/*
 * File        : EnergyBookingSlot.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Bookable energy time slot at a station (collection: EnergyBookingSlots).
 */
namespace SmartSolarMicrogrid.Api.Models;

public sealed class EnergyBookingSlot : Entity
{
    public string StationId { get; set; } = "";

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public decimal AvailableKwh { get; set; }

    public decimal PricePerKwh { get; set; }

    public bool IsActive { get; set; } = true;
}
