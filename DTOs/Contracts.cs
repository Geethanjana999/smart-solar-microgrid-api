using System.ComponentModel.DataAnnotations;
namespace SmartSolarMicrogrid.Api.DTOs;
public sealed class LoginRequest { [Required] public string Username { get; set; } = ""; [Required] public string Password { get; set; } = ""; }
public sealed record LoginResponse(string Token, DateTime ExpiresAt, string Username, string Role, string? ProsumerNic);
public sealed class UserRequest { [Required, MinLength(3)] public string Username { get; set; } = ""; [Required, MinLength(6)] public string Password { get; set; } = ""; [Required] public string Role { get; set; } = ""; public string? ProsumerNic { get; set; } }
public sealed class UserUpdateRequest { [Required] public string Role { get; set; } = ""; public bool IsActive { get; set; } = true; }
public sealed class ProsumerRequest { [Required] public string Nic { get; set; } = ""; [Required] public string FullName { get; set; } = ""; [Required, EmailAddress] public string Email { get; set; } = ""; [Required] public string Phone { get; set; } = ""; public string Address { get; set; } = ""; [Required, MinLength(6)] public string Password { get; set; } = ""; }
public sealed class ProsumerUpdateRequest { [Required] public string FullName { get; set; } = ""; [Required, EmailAddress] public string Email { get; set; } = ""; [Required] public string Phone { get; set; } = ""; public string Address { get; set; } = ""; }
public sealed class StationRequest { [Required] public string Name { get; set; } = ""; [Required] public string Location { get; set; } = ""; [Range(-90,90)] public double Latitude { get; set; } [Range(-180,180)] public double Longitude { get; set; } [Range(0.01,double.MaxValue)] public decimal CapacityKwh { get; set; } [Range(1,int.MaxValue)] public int BatteryStorageSlots { get; set; } }
public sealed class SlotRequest { [Required] public string StationId { get; set; } = ""; public DateTime StartTime { get; set; } public DateTime EndTime { get; set; } [Range(0.01,double.MaxValue)] public decimal AvailableKwh { get; set; } [Range(0,double.MaxValue)] public decimal PricePerKwh { get; set; } }
public sealed class ReservationRequest { [Required] public string SlotId { get; set; } = ""; [Range(0.01,double.MaxValue)] public decimal EnergyKwh { get; set; } }
public sealed class ReservationUpdateRequest { [Range(0.01,double.MaxValue)] public decimal EnergyKwh { get; set; } }
public sealed class QrVerifyRequest { [Required] public string QrToken { get; set; } = ""; }
public sealed record DashboardResponse(int ActiveStations, int AvailableSlots, int PendingReservations, int CompletedReservations, decimal AvailableEnergyKwh);
