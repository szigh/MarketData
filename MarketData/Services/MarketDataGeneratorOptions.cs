namespace MarketData.Services;

public class MarketDataGeneratorOptions
{
    public const string SectionName = "MarketDataGenerator";

    public int CheckIntervalMilliseconds { get; set; } = 100;
}
