namespace MarketData.Models;

public class Instrument
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// How often to check new prices
    /// </summary>
    public int TickIntervalMillieconds { get; set; }
}
