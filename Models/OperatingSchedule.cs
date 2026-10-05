namespace SmartSolarMicrogrid.Api.Models;

public sealed class OperatingSchedule
{
    public string OperationalStartTime { get; set; } = "06:00";
    public string OperationalEndTime { get; set; } = "18:00";
    public string PeakTradingStartTime { get; set; } = "10:00";
    public string PeakTradingEndTime { get; set; } = "14:00";
    public string MaintenanceDay { get; set; } = "Sunday";
}
