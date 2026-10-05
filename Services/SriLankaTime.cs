/*
 * File        : SriLankaTime.cs
 * Project     : Smart Solar Microgrid Trading System - Web API (SE4040 EAD Assignment)
 * Description : Helpers for Sri Lanka time (UTC+05:30, no daylight saving).
 */
using System.Globalization;

namespace SmartSolarMicrogrid.Api.Services;

public static class SriLankaTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(5.5);

    // Current wall-clock time in Sri Lanka.
    public static DateTime Now => DateTime.UtcNow + Offset;

    // Any client date/time -> Sri Lanka calendar date (00:00, kind Utc, used only as a date holder).
    public static DateTime ToStoredDate(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Local
            ? value.ToUniversalTime()
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        return DateTime.SpecifyKind((utc + Offset).Date, DateTimeKind.Utc);
    }

    // Sri Lanka calendar date of a UTC instant.
    public static DateTime LocalDate(DateTime utc) =>
        DateTime.SpecifyKind((DateTime.SpecifyKind(utc, DateTimeKind.Utc) + Offset).Date, DateTimeKind.Utc);

    // Sri Lanka date -> the UTC instant of Colombo midnight (the format the app sends as slotDate).
    public static DateTime ToClientInstant(DateTime localDate) =>
        DateTime.SpecifyKind(localDate.Date - Offset, DateTimeKind.Utc);

    // "06:00 PM - 08:00 PM" for a UTC start/end pair.
    public static string SlotText(DateTime startUtc, DateTime endUtc)
    {
        var start = (DateTime.SpecifyKind(startUtc, DateTimeKind.Utc) + Offset).ToString("hh:mm tt", CultureInfo.InvariantCulture);
        var end = (DateTime.SpecifyKind(endUtc, DateTimeKind.Utc) + Offset).ToString("hh:mm tt", CultureInfo.InvariantCulture);
        return $"{start} - {end}";
    }
}