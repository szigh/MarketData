namespace MarketData.Models;

public class RandomAdditiveWalkConfig
{
    public int Id { get; set; }
    public int InstrumentId { get; set; }
    
    /// <summary>
    /// JSON-serialized array of RandomWalkStep objects
    /// </summary>
    public string WalkStepsJson { get; set; } = string.Empty;

    public Instrument Instrument { get; set; } = null!;
}
