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

    private readonly IDialogService _dialogService;

    public ModelConfigViewModelFactory(string instrumentName, 
        IModelConfigService modelConfigService, IDialogService dialogService)
    {
        _instrumentName = instrumentName;
        _modelConfigService = modelConfigService;
        _dialogService = dialogService;
    }

    /// <summary>
    /// Creates the appropriate view model for the given model type and configuration
    /// </summary>
    public ModelConfigViewModelBase? Create(string modelType, ConfigurationsResponse config)
    {
        return modelType switch
        {
            "RandomMultiplicative" when config.RandomMultiplicative != null =>
                new RandomMultiplicativeConfigViewModel(_instrumentName, config.RandomMultiplicative, _modelConfigService, _dialogService),

            "MeanReverting" when config.MeanReverting != null =>
                new MeanRevertingConfigViewModel(_instrumentName, config.MeanReverting, _modelConfigService, _dialogService),

            "Flat" =>
                new FlatConfigViewModel(_instrumentName, _dialogService),

            "RandomAdditiveWalk" when config.RandomAdditiveWalk != null =>
                new RandomAdditiveWalkConfigViewModel(_instrumentName, config.RandomAdditiveWalk, _modelConfigService, _dialogService),

            _ => null
        };
    }
}
