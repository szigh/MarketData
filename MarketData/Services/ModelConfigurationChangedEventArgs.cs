namespace MarketData.Services;

/// <summary>
/// Event args for configuration changes
/// </summary>
public class ModelConfigurationChangedEventArgs : EventArgs
{
    public string InstrumentName { get; init; } = string.Empty;
    public string? ModelType { get; init; }
    public DateTime Timestamp { get; init; }
}

