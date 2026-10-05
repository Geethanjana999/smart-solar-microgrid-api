using System.ComponentModel.DataAnnotations;

namespace SmartSolarMicrogrid.Api.DTOs;

public sealed class NodeScheduleRequest
{
    [Required]
    public string OperationalStartTime { get; set; } = "";
    
    [Required]
    public string OperationalEndTime { get; set; } = "";
    
    [Required]
    public string PeakTradingStartTime { get; set; } = "";
    
    [Required]
    public string PeakTradingEndTime { get; set; } = "";
    
    public string MaintenanceDay { get; set; } = "";
}
