using MarketData.Grpc;
using MarketData.Wpf.Client.Services;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

/// <summary>
/// Factory for creating model-specific configuration view models
/// </summary>
public class ModelConfigViewModelFactory
{
    private readonly string _instrumentName;
    private readonly IModelConfigService _modelConfigService;

    public ModelConfigViewModelFactory(string instrumentName, IModelConfigService modelConfigService)
    {
        _instrumentName = instrumentName;
        _modelConfigService = modelConfigService;
    }

    /// <summary>
    /// Creates the appropriate view model for the given model type and configuration
    /// </summary>
    public ModelConfigViewModelBase? Create(string modelType, ConfigurationsResponse config)
    {
        return modelType switch
        {
            "RandomMultiplicative" when config.RandomMultiplicative != null =>
                new RandomMultiplicativeConfigViewModel(_instrumentName, config.RandomMultiplicative, _modelConfigService),

            "MeanReverting" when config.MeanReverting != null =>
                new MeanRevertingConfigViewModel(_instrumentName, config.MeanReverting, _modelConfigService),

            "Flat" =>
                new FlatConfigViewModel(_instrumentName),

            "RandomAdditiveWalk" when config.RandomAdditiveWalk != null =>
                new RandomAdditiveWalkConfigViewModel(_instrumentName, config.RandomAdditiveWalk, _modelConfigService),

            _ => null
        };
    }
}
