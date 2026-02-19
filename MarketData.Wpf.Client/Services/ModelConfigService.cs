using Grpc.Net.Client;
using MarketData.Client.Shared.Configuration;
using MarketData.Grpc;
using Microsoft.Extensions.Options;

namespace MarketData.Wpf.Client.Services;

public class ModelConfigService : IModelConfigService
{
    private readonly GrpcChannel _channel;
    private readonly ModelConfigurationService.ModelConfigurationServiceClient _client;

    public ModelConfigService(IOptions<GrpcSettings> grpcSettings)
    {
        _channel = GrpcChannel.ForAddress(grpcSettings.Value.ServerUrl);
        _client = new ModelConfigurationService.ModelConfigurationServiceClient(_channel);
    }

    public async Task<SupportedModelsResponse> GetSupportedModelsAsync()
    {
        return await _client.GetSupportedModelsAsync(
            new GetSupportedModelsRequest());
    }

    public async Task<ConfigurationsResponse> GetConfigurationsAsync(string instrumentName)
    {
        return await _client.GetConfigurationsAsync(
            new GetConfigurationsRequest { InstrumentName = instrumentName });
    }

    public async Task<SwitchModelResponse> SwitchModelAsync(string instrumentName, string modelType)
    {
        return await _client.SwitchModelAsync(new SwitchModelRequest
        {
            InstrumentName = instrumentName,
            ModelType = modelType
        });
    }

    public async Task<UpdateConfigResponse> UpdateTickIntervalAsync(string instrumentName, int tickIntervalMs)
    {
        return await _client.UpdateTickIntervalAsync(new UpdateTickIntervalRequest
        {
            InstrumentName = instrumentName,
            TickIntervalMs = tickIntervalMs
        });
    }

    public async Task UpdateRandomMultiplicativeConfigAsync(
        string instrumentName,
        double standardDeviation,
        double mean)
    {
        await _client.UpdateRandomMultiplicativeConfigAsync(
            new UpdateRandomMultiplicativeRequest
            {
                InstrumentName = instrumentName,
                StandardDeviation = standardDeviation,
                Mean = mean
            });
    }

    public async Task UpdateMeanRevertingConfigAsync(
        string instrumentName,
        double mean,
        double kappa,
        double sigma,
        double dt)
    {
        await _client.UpdateMeanRevertingConfigAsync(
            new UpdateMeanRevertingRequest
            {
                InstrumentName = instrumentName,
                Mean = mean,
                Kappa = kappa,
                Sigma = sigma,
                Dt = dt
            });
    }

    public async Task UpdateRandomAdditiveWalkConfigAsync(
        string instrumentName,
        IEnumerable<(double probability, double stepValue)> walkSteps)
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

        await _client.UpdateRandomAdditiveWalkConfigAsync(request);
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}