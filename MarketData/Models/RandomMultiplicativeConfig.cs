namespace MarketData.Models;

public class RandomMultiplicativeConfig
{
    public int Id { get; set; }
    public int InstrumentId { get; set; }
    public double StandardDeviation { get; set; }
    public double Mean { get; set; }

    public Instrument Instrument { get; set; } = null!;
}
