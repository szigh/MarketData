namespace MarketData.Models;

public class Instrument
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// How often to check new prices
    /// </summary>
    public int TickIntervalMillieconds { get; set; }

    /// <summary>
    /// The type of price simulator model to use for this instrument.
    /// Valid values: "RandomMultiplicative", "MeanReverting", "Flat", "RandomAdditiveWalk"
    /// </summary>
    public string ModelType { get; set; } = "RandomMultiplicative";

    // Navigation properties
    public RandomMultiplicativeConfig? RandomMultiplicativeConfig { get; set; }
    public MeanRevertingConfig? MeanRevertingConfig { get; set; }
    public FlatConfig? FlatConfig { get; set; }
    public RandomAdditiveWalkConfig? RandomAdditiveWalkConfig { get; set; }
}
