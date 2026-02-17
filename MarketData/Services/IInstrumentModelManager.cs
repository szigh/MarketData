using MarketData.Models;
using MarketData.PriceSimulator;

namespace MarketData.Services;

/// <summary>
/// Interface for managing price simulator models for instruments.
/// Handles configuration validation, model switching, and configuration updates.
/// </summary>
public interface IInstrumentModelManager
{
    /// <summary>
    /// Event raised when a model configuration is changed.
    /// Subscribers can use this to hot-reload simulators.
    /// </summary>
    event EventHandler<ModelConfigurationChangedEventArgs>? ConfigurationChanged;

    /// <summary>
    /// Gets an instrument with all its model configurations loaded
    /// </summary>
    Task<Instrument?> GetInstrumentWithConfigurationsAsync(string instrumentName);

    /// <summary>
    /// Loads all instruments with configurations and ensures they are properly initialized.
    /// Returns a dictionary mapping instrument name to loaded instrument.
    /// </summary>
    Task<Dictionary<string, Instrument>> LoadAndInitializeAllInstrumentsAsync();

    /// <summary>
    /// Switches the active model for an instrument.
    /// Automatically creates default configuration if it doesn't exist.
    /// </summary>
    /// <returns>The previous model type</returns>
    Task<string?> SwitchModelAsync(string instrumentName, string newModelType);

    /// <summary>
    /// Updates RandomMultiplicative configuration for an instrument
    /// </summary>
    Task<RandomMultiplicativeConfig> UpdateRandomMultiplicativeConfigAsync(
        string instrumentName,
        double standardDeviation,
        double mean);

    /// <summary>
    /// Updates MeanReverting configuration for an instrument
    /// </summary>
    Task<MeanRevertingConfig> UpdateMeanRevertingConfigAsync(
        string instrumentName,
        double mean,
        double kappa,
        double sigma,
        double dt);

    /// <summary>
    /// Updates RandomAdditiveWalk configuration for an instrument
    /// </summary>
    Task<RandomAdditiveWalkConfig> UpdateRandomAdditiveWalkConfigAsync(
        string instrumentName,
        string walkStepsJson);

    /// <summary>
    /// Creates the appropriate price simulator for an instrument based on its model type and configuration
    /// </summary>
    IPriceSimulator CreatePriceSimulator(Instrument instrument);
}
