using MarketData.Data;
using MarketData.Models;
using MarketData.PriceSimulator;

namespace MarketData.Services;

/// <summary>
/// Interface for managing price simulator models for instruments.
/// Handles configuration validation, model switching, and configuration updates.
/// </summary>
public interface IInstrumentModelManager
{
    event EventHandler<ModelConfigurationChangedEventArgs>? ConfigurationChanged;
    event EventHandler<ModelConfigurationChangedEventArgs>? ModelSwitched;
    event EventHandler<ModelConfigurationChangedEventArgs>? TickIntervalChanged;
    event EventHandler<ModelConfigurationChangedEventArgs>? InstrumentAdded;
    event EventHandler<ModelConfigurationChangedEventArgs>? InstrumentRemoved;

    /// <summary>
    /// Gets an instrument with all its model configurations loaded
    /// </summary>
    Task<Instrument?> GetInstrumentWithConfigurationsAsync(string instrumentName);

    /// <summary>
    /// Loads all instruments with configurations and ensures they are properly initialized.
    /// Returns a dictionary mapping instrument name to loaded instrument.
    /// </summary>
    Task<Dictionary<string, Instrument>> LoadAndInitializeAllInstrumentsAsync();

    Task<int> UpdateTickIntervalAsync(string instrumentName, int tickIntervalMs); 

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

    /// <summary>
    /// Asynchronously validates that the model type is appropriate for the specified instrument within the given market
    /// data context.
    /// </summary>
    /// <remarks>This method may perform validation by checking the instrument against existing data in the
    /// provided context. The operation is asynchronous and may involve I/O or database access.</remarks>
    /// <param name="instrument">The instrument for which the model type validation is performed.</param>
    /// <param name="context">The market data context that provides the environment and data necessary for validation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the model
    /// type is valid for the specified instrument; otherwise, <see langword="false"/>.</returns>
    Task<bool> EnsureModelTypeAsync(Instrument instrument, MarketDataContext context);

    /// <summary>
    /// Ensures that the model configuration for the specified instrument is present and correctly set up within the
    /// given market data context.
    /// </summary>
    /// <remarks>This method is asynchronous and should be awaited. Throws an exception if the provided
    /// parameters are invalid or if the configuration process encounters an error.</remarks>
    /// <param name="instrument">The instrument for which to ensure model configuration. Cannot be null.</param>
    /// <param name="context">The market data context in which the model configuration is to be applied. Must be a valid context instance.</param>
    /// <returns>A task that represents the asynchronous operation to ensure the model configuration.</returns>
    Task EnsureModelConfigurationAsync(Instrument instrument, MarketDataContext context);

    /// <summary>
    /// Seeds Instrument and Price data for a new instrument. 
    /// If the instrument already exists, it does nothing.
    /// </summary>
    /// <param name="instrumentName">The name of the instrument</param>
    /// <param name="tickIntervalMs">The tick interval in milliseconds</param>
    /// <param name="initialPriceValue">The initial price value</param>
    /// <param name="initialPriceTimestamp">The timestamp of the initial price</param>
    /// <param name="modelType">The model type to use, optional. Defaults to DefaultModelType.</param>
    /// <returns>A tuple containing the instrument and a boolean indicating if it was created</returns>
    Task<(Instrument instrument, bool created)> GetOrCreateInstrumentAsync(
        string instrumentName, int tickIntervalMs,
        decimal initialPriceValue, DateTime initialPriceTimestamp,
        string? modelType = null);
    Task<bool> TryRemoveInstrument(string instrumentName);
}
