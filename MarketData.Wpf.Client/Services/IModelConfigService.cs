using MarketData.Grpc;

namespace MarketData.Client.Wpf.Services;

/// <summary>
/// Service interface for managing model configurations via gRPC
/// </summary>
public interface IModelConfigService : IDisposable
{
    /// <summary>
    /// Gets the list of supported model types
    /// </summary>
    Task<IEnumerable<string>> GetSupportedModelsAsync(CancellationToken ct);


    /// <summary>
    /// Asynchronously retrieves a collection of all available instrument names.
    /// </summary>
    Task<IEnumerable<string>> GetAllInstrumentsAsync(CancellationToken ct);

    /// <summary>
    /// Gets all configurations for a specific instrument
    /// </summary>
    Task<ConfigurationsResponse> GetConfigurationsAsync(string instrumentName, CancellationToken ct);

    /// <summary>
    /// Switches the active model for an instrument
    /// </summary>
    Task<SwitchModelResponse> SwitchModelAsync(string instrumentName, string modelType, CancellationToken ct);

    /// <summary>
    /// Updates the tick interval for an instrument
    /// </summary>
    Task<UpdateConfigResponse> UpdateTickIntervalAsync(string instrumentName, int tickIntervalMs, CancellationToken ct);

    /// <summary>
    /// Updates RandomMultiplicative model configuration
    /// </summary>
    Task UpdateRandomMultiplicativeConfigAsync(
        string instrumentName,
        double standardDeviation,
        double mean,
        CancellationToken ct);

    /// <summary>
    /// Updates MeanReverting model configuration
    /// </summary>
    Task UpdateMeanRevertingConfigAsync(
        string instrumentName,
        double mean,
        double kappa,
        double sigma,
        double dt,
        CancellationToken ct);

    /// <summary>
    /// Updates RandomAdditiveWalk model configuration
    /// </summary>
    Task UpdateRandomAdditiveWalkConfigAsync(
        string instrumentName,
        IEnumerable<(double probability, double stepValue)> walkSteps,
        CancellationToken ct);

    /// <summary>
    /// Attempts to remove the specified instrument asynchronously and returns the result of the operation.
    /// </summary>
    /// <remarks>This method may throw exceptions if the operation is canceled or if an error occurs during
    /// the removal process.</remarks>
    /// <param name="instrumentName">The name of the instrument to remove. This parameter cannot be null or empty.</param>
    /// <param name="ct">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A tuple containing a boolean value that indicates whether the removal was successful, and a message providing
    /// additional information about the operation.</returns>
    Task<(bool Response, string Message)> TryRemoveInstrumentAsync(string instrumentName, CancellationToken ct);
    Task<(bool Response, string Message)> TryAddInstrumentAsync(string instrumentName, double initialValue, int tickIntervalMs, string modelType = "Flat", CancellationToken ct = default);
}
