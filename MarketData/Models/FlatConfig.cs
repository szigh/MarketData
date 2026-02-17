namespace MarketData.Models;

public class FlatConfig
{
    public int Id { get; set; }
    public int InstrumentId { get; set; }

    public Instrument Instrument { get; set; } = null!;
}
