using MarketData.Grpc;

namespace MarketData.Client.Grpc.Services;

/// <summary>
/// Service interface for managing model configurations via gRPC
/// </summary>
public interface IModelConfigService : IDisposable
{
    /// <summary>
    /// Gets the list of supported model types
    /// </summary>
    Task<SupportedModelsResponse> GetSupportedModelsAsync(CancellationToken ct);

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
    Task<GetAllInstrumentsResponse> GetAllInstrumentsAsync(CancellationToken ct = default);
    Task<TryRemoveInstrumentResponse> TryRemoveInstrumentAsync(string instrumentName, CancellationToken ct = default);
    Task<TryAddInstrumentResponse> TryAddInstrumentAsync(string instrumentName, int tickIntervalMs, double initialPrice, CancellationToken ct = default);
}
