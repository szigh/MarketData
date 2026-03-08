namespace MarketData.Services;

/// <summary>
/// Event args for configuration changes
/// </summary>
public class ModelConfigurationChangedEventArgs : EventArgs
{
    public string InstrumentName { get; init; } = string.Empty;
    public string NewModelType { get; init; } = string.Empty;
    public int NewTickIntervalMs { get; init; }
}
