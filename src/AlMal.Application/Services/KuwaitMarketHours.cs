namespace AlMal.Application.Services;

/// <summary>
/// Static utility for determining Kuwait Stock Exchange trading hours.
/// Kuwait Time (KWT) is UTC+3. The market operates Sunday through Thursday, 9:00 AM - 12:40 PM.
/// </summary>
public static class KuwaitMarketHours
{
    // Kuwait Time = UTC+3
    private static readonly TimeZoneInfo KuwaitTimeZone =
        TimeZoneInfo.CreateCustomTimeZone("KWT", TimeSpan.FromHours(3), "Kuwait Time", "Kuwait Time");

    // Market hours: Sunday-Thursday 9:00 AM - 12:40 PM KWT
    private static readonly TimeOnly MarketOpen = new(9, 0);
    private static readonly TimeOnly MarketClose = new(12, 40);

    /// <summary>
    /// Returns true if the Kuwait Stock Exchange is currently within trading hours
    /// (Sunday-Thursday, 9:00 AM - 12:40 PM KWT).
    /// </summary>
    public static bool IsMarketOpen()
    {
        var kuwaitNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, KuwaitTimeZone);
        var dayOfWeek = kuwaitNow.DayOfWeek;

        // Market is open Sunday through Thursday
        if (dayOfWeek == DayOfWeek.Friday || dayOfWeek == DayOfWeek.Saturday)
            return false;

        var timeNow = TimeOnly.FromDateTime(kuwaitNow);
        return timeNow >= MarketOpen && timeNow <= MarketClose;
    }

    /// <summary>
    /// Returns the current date and time in Kuwait (UTC+3).
    /// </summary>
    public static DateTime GetKuwaitTime() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, KuwaitTimeZone);
}
