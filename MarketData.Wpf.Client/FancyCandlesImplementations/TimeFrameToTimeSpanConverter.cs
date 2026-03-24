using FancyCandles;

namespace MarketData.Client.Wpf.FancyCandlesImplementations;

internal static class TimeFrameToTimeSpanConverter
{
    internal static TimeSpan ToTimeSpan(this TimeFrame timeFrame)
    {
        return timeFrame switch
        {
            TimeFrame.S1 => TimeSpan.FromSeconds(1),
            TimeFrame.S2 => TimeSpan.FromSeconds(2),
            TimeFrame.S3 => TimeSpan.FromSeconds(3),
            TimeFrame.S5 => TimeSpan.FromSeconds(5),
            TimeFrame.S10 => TimeSpan.FromSeconds(10),
            TimeFrame.S15 => TimeSpan.FromSeconds(15),
            TimeFrame.S20 => TimeSpan.FromSeconds(20),
            TimeFrame.S30 => TimeSpan.FromSeconds(30),
            TimeFrame.M1 => TimeSpan.FromMinutes(1),
            TimeFrame.M2 => TimeSpan.FromMinutes(2),
            TimeFrame.M3 => TimeSpan.FromMinutes(3),
            TimeFrame.M5 => TimeSpan.FromMinutes(5),
            TimeFrame.M10 => TimeSpan.FromMinutes(10),
            TimeFrame.M15 => TimeSpan.FromMinutes(15),
            TimeFrame.M30 => TimeSpan.FromMinutes(30),
            TimeFrame.H1 => TimeSpan.FromHours(1),
            TimeFrame.H2 => TimeSpan.FromHours(2),
            TimeFrame.H3 => TimeSpan.FromHours(3),
            TimeFrame.H4 => TimeSpan.FromHours(4),
            TimeFrame.Daily => TimeSpan.FromHours(1440),
            TimeFrame.Weekly => TimeSpan.FromHours(10080),
            TimeFrame.Monthly => TimeSpan.FromHours(43200), // Approximation for monthly

            _ => throw new ArgumentOutOfRangeException(nameof(timeFrame), $"Unsupported time frame: {timeFrame}")
        };
    }
}

/*
 For reference:
public enum TimeFrame
{
    S1 = -1,
    S2 = -2,
    S3 = -3,
    S5 = -5,
    S10 = -10,
    S15 = -15,
    S20 = -20,
    S30 = -30,
    M1 = 1,
    M2 = 2,
    M3 = 3,
    M5 = 5,
    M10 = 10,
    M15 = 15,
    M20 = 20,
    M30 = 30,
    H1 = 60,
    H2 = 120,
    H3 = 180,
    H4 = 240,
    Daily = 1440,
    Weekly = 10080,
    Monthly = 43200
}
 
 */
