using Grpc.Net.Client;
using MarketData.Client.Shared.Configuration;
using MarketData.Grpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MarketData.Wpf.Client.Services;

public class ModelConfigService : IModelConfigService, IDisposable
{
    private readonly ILogger<ModelConfigService> _logger;
    private readonly GrpcChannel _channel;
    private readonly ModelConfigurationService.ModelConfigurationServiceClient _client;

    private bool _disposed;

    public ModelConfigService(IOptions<GrpcSettings> grpcSettings, ILoggerFactory loggerFactory, ILogger<ModelConfigService> logger)
    {
        _logger = logger;
        _channel = GrpcChannel.ForAddress(grpcSettings.Value.ServerUrl);
        _client = new ModelConfigurationService.ModelConfigurationServiceClient(_channel);
    }

    public async Task<SupportedModelsResponse> GetSupportedModelsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting supported models from gRPC service.");
        return await _client.GetSupportedModelsAsync(
            new GetSupportedModelsRequest(), cancellationToken: ct);
    }

    public async Task<ConfigurationsResponse> GetConfigurationsAsync(string instrumentName, CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting current configurations for instrument {Instrument} from gRPC service.", instrumentName);
        return await _client.GetConfigurationsAsync(
            new GetConfigurationsRequest { InstrumentName = instrumentName }, cancellationToken: ct);
    }

    public async Task<SwitchModelResponse> SwitchModelAsync(string instrumentName, string modelType, CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting model switch for instrument {Instrument} to model type {ModelType} from gRPC service.", instrumentName, modelType);
        return await _client.SwitchModelAsync(new SwitchModelRequest
        {
            InstrumentName = instrumentName,
            ModelType = modelType
        }, cancellationToken: ct);
    }

    public async Task<UpdateConfigResponse> UpdateTickIntervalAsync(string instrumentName, int tickIntervalMs, CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting tick interval update for instrument {Instrument} to {TickIntervalMs} ms from gRPC service.", instrumentName, tickIntervalMs);
        return await _client.UpdateTickIntervalAsync(new UpdateTickIntervalRequest
        {
            InstrumentName = instrumentName,
            TickIntervalMs = tickIntervalMs
        }, cancellationToken: ct);
    }

    public async Task UpdateRandomMultiplicativeConfigAsync(
        string instrumentName,
        double standardDeviation,
        double mean,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting Random Multiplicative config update for instrument {Instrument} " +
            "with StdDev={StandardDeviation}, Mean={Mean} from gRPC service.", instrumentName, standardDeviation, mean);
        
        await _client.UpdateRandomMultiplicativeConfigAsync(
            new UpdateRandomMultiplicativeRequest
            {
                InstrumentName = instrumentName,
                StandardDeviation = standardDeviation,
                Mean = mean
            }, cancellationToken: ct);
    }

    public async Task UpdateMeanRevertingConfigAsync(
        string instrumentName,
        double mean,
        double kappa,
        double sigma,
        double dt,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Requesting Mean Reverting config update for instrument {Instrument} " +
            "with Mean={Mean}, Kappa={Kappa}, Sigma={Sigma}, Dt={Dt} from gRPC service.", 
            instrumentName, mean, kappa, sigma, dt);

        await _client.UpdateMeanRevertingConfigAsync(
            new UpdateMeanRevertingRequest
            {
                InstrumentName = instrumentName,
                Mean = mean,
                Kappa = kappa,
                Sigma = sigma,
                Dt = dt
            }, cancellationToken: ct);
    }

    public async Task UpdateRandomAdditiveWalkConfigAsync(
        string instrumentName,
        IEnumerable<(double probability, double stepValue)> walkSteps, 
        CancellationToken ct = default)
    {
        var request = new UpdateRandomAdditiveWalkRequest
        {
            InstrumentName = instrumentName
        };

        foreach (var (probability, stepValue) in walkSteps)
        {
            request.WalkSteps.Add(new WalkStep
            {
                Probability = probability,
                StepValue = stepValue
            });
        }

        _logger.LogInformation("Requesting Random Additive Walk config update for instrument {Instrument} with {StepCount} steps from gRPC service.", 
            instrumentName, request.WalkSteps.Count);
        await _client.UpdateRandomAdditiveWalkConfigAsync(request, cancellationToken: ct);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _channel?.Dispose();
            }
            _disposed = true;
        }
    }
}