using MarketData.Client.Wpf.Services;
using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using Microsoft.Extensions.Logging;

namespace MarketData.Wpf.Client.ViewModels.ModelConfigs;

/// <summary>
/// Factory for creating model-specific configuration view models
/// </summary>
public class ModelConfigViewModelFactory
{
    private readonly IModelConfigService _modelConfigService;
    private readonly IDialogService _dialogService;
    private readonly ILoggerFactory _loggerFactory;

    public ModelConfigViewModelFactory(IModelConfigService modelConfigService, 
        IDialogService dialogService,
        ILoggerFactory loggerFactory)
    {
        _modelConfigService = modelConfigService;
        _dialogService = dialogService;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Creates the appropriate view model for the given model type and configuration
    /// </summary>
    public ModelConfigViewModelBase? Create(ConfigurationsResponse config)
    {
        return Create(config.ActiveModel, config);
    }

    /// <summary>
    /// Creates the appropriate view model for the given model type and configuration
    /// </summary>
    public ModelConfigViewModelBase? Create(string modelType, ConfigurationsResponse config)
    {
        _loggerFactory.CreateLogger<ModelConfigViewModelFactory>()
            .LogInformation("Creating model config view model for instrument {InstrumentName} " +
            "with model type {ModelType}", config.InstrumentName, modelType);

        return modelType switch
        {
            "RandomMultiplicative" when config.RandomMultiplicative != null =>
                new RandomMultiplicativeConfigViewModel(config.InstrumentName, config.RandomMultiplicative, 
                _modelConfigService, _dialogService, _loggerFactory.CreateLogger<RandomMultiplicativeConfigViewModel>()),

            "MeanReverting" when config.MeanReverting != null =>
                new MeanRevertingConfigViewModel(config.InstrumentName, config.MeanReverting, 
                _modelConfigService, _dialogService, _loggerFactory.CreateLogger<MeanRevertingConfigViewModel>()),

            "Flat" =>
                new FlatConfigViewModel(config.InstrumentName, 
                _dialogService, _loggerFactory.CreateLogger<FlatConfigViewModel>()),

            "RandomAdditiveWalk" when config.RandomAdditiveWalk != null =>
                new RandomAdditiveWalkConfigViewModel(config.InstrumentName, config.RandomAdditiveWalk, 
                _modelConfigService, _dialogService, _loggerFactory.CreateLogger<RandomAdditiveWalkConfigViewModel>()),

            _ => null
        };
    }
}
