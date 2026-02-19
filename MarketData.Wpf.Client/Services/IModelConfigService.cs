using MarketData.Grpc;

namespace MarketData.Wpf.Client.Services;

/// <summary>
/// Service interface for managing model configurations via gRPC
/// </summary>
public interface IModelConfigService : IDisposable
{
    /// <summary>
    /// Gets the list of supported model types
    /// </summary>
    Task<SupportedModelsResponse> GetSupportedModelsAsync();

    /// <summary>
    /// Gets all configurations for a specific instrument
    /// </summary>
    Task<ConfigurationsResponse> GetConfigurationsAsync(string instrumentName);

    /// <summary>
    /// Switches the active model for an instrument
    /// </summary>
    Task<SwitchModelResponse> SwitchModelAsync(string instrumentName, string modelType);

    /// <summary>
    /// Updates the tick interval for an instrument
    /// </summary>
    Task<UpdateConfigResponse> UpdateTickIntervalAsync(string instrumentName, int tickIntervalMs);

    /// <summary>
    /// Updates RandomMultiplicative model configuration
    /// </summary>
    Task UpdateRandomMultiplicativeConfigAsync(
        string instrumentName,
        double standardDeviation,
        double mean);

    /// <summary>
    /// Updates MeanReverting model configuration
    /// </summary>
    Task UpdateMeanRevertingConfigAsync(
        string instrumentName,
        double mean,
        double kappa,
        double sigma,
        double dt);

    /// <summary>
    /// Updates RandomAdditiveWalk model configuration
    /// </summary>
    Task UpdateRandomAdditiveWalkConfigAsync(
        string instrumentName,
        IEnumerable<(double probability, double stepValue)> walkSteps);
}
