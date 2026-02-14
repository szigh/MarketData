namespace MarketData.Models;

public class Instrument
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TickIntervalSeconds { get; set; }
}
