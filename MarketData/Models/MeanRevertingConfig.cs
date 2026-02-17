namespace MarketData.Models;

public class MeanRevertingConfig
{
    public int Id { get; set; }
    public int InstrumentId { get; set; }
    public double Mean { get; set; }
    public double Kappa { get; set; }
    public double Sigma { get; set; }
    public double Dt { get; set; }

    public Instrument Instrument { get; set; } = null!;
}
