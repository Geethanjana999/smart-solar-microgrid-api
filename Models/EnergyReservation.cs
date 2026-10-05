/*
 * File        : EnergyReservation.cs
 * Description : Prosumer reservation of a slot, including its QR token (collection: EnergyReservations).
 */
namespace SmartSolarMicrogrid.Api.Models;

public sealed class EnergyReservation : Entity
{
    public string ProsumerNic { get; set; } = "";

    public string StationId { get; set; } = "";

    public string SlotId { get; set; } = "";

    public decimal EnergyKwh { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

    // Token encoded into the QR code shown by the mobile app and verified by the operator.
    public string QrToken { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime? FinalizedAt { get; set; }

    public string? FinalizedBy { get; set; }
}
