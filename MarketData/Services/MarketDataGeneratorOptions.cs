namespace MarketData.Services;

public class MarketDataGeneratorOptions
{
    public const string SectionName = "MarketDataGenerator";

    public int CheckIntervalMilliseconds { get; set; } = 100;
    public int DatabasePersistenceMilliseconds { get; set; } = 10000;
    public int GrpcPublishMilliseconds { get; set; } = 100;
}
