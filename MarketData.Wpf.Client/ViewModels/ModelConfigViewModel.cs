using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Client.ViewModels.ModelConfigs;
using MarketData.Wpf.Shared;
using System.Windows;

namespace MarketData.Wpf.Client.ViewModels;

public class ModelConfigViewModel : ViewModelBase
{
    private readonly ModelConfigService _modelConfigService;
    private readonly string _instrument;
    private readonly string[] _supportedModels;

    private ConfigurationsResponse _config;
    private string _activeModel;
    private bool _isSwitchingModel;
    private ModelConfigViewModelBase? _activeConfigViewModel;

    public ModelConfigViewModel(
        string instrument,
        ModelConfigService modelConfigService,
        ConfigurationsResponse config,
        SupportedModelsResponse supportedModels)
    {
        _instrument = instrument;
        _supportedModels = supportedModels.SupportedModels.ToArray();
        _config = config;
        _activeModel = config.ActiveModel;
        _modelConfigService = modelConfigService;

        // Create the appropriate child ViewModel based on active model
        UpdateActiveConfigViewModel();
    }

    public string Instrument => _instrument;

    public string ActiveModel
    {
        get => _activeModel;
        set
        {
            if (_activeModel != value && !_isSwitchingModel)
            {
                var oldValue = _activeModel;
                SetProperty(ref _activeModel, value); // Update UI immediately
                _ = SwitchModelAsync(value, oldValue); // Fire and forget
            }
        }
    }

    public bool IsSwitchingModel
    {
        get => _isSwitchingModel;
        private set => SetProperty(ref _isSwitchingModel, value);
    }

    public string[] SupportedModels => _supportedModels;

    /// <summary>
    /// The ViewModel for the currently active model's configuration
    /// </summary>
    public ModelConfigViewModelBase? ActiveConfigViewModel
    {
        get => _activeConfigViewModel;
        private set => SetProperty(ref _activeConfigViewModel, value);
    }

    private async Task SwitchModelAsync(string newModel, string oldModel)
    {
        IsSwitchingModel = true;
        try
        {
            await _modelConfigService.SwitchModelAsync(_instrument, newModel);
            
            // Reload configuration to get the new model's config
            _config = await _modelConfigService.GetConfigurationsAsync(_instrument);
            UpdateActiveConfigViewModel();
        }
        catch (Exception ex)
        {
            // Revert to old model on failure
            _activeModel = oldModel;
            OnPropertyChanged(nameof(ActiveModel));

            MessageBox.Show(
                $"Failed to switch model: {ex.Message}", 
                "Error",
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        }
        finally
        {
            IsSwitchingModel = false;
        }
    }

    private void UpdateActiveConfigViewModel()
    {
        ActiveConfigViewModel = _activeModel switch
        {
            "RandomMultiplicative" when _config.RandomMultiplicative != null =>
                new RandomMultiplicativeConfigViewModel(_instrument, _config.RandomMultiplicative),
            
            "MeanReverting" when _config.MeanReverting != null =>
                new MeanRevertingConfigViewModel(_instrument, _config.MeanReverting),
            
            "Flat" =>
                new FlatConfigViewModel(_instrument),
            
            "RandomAdditiveWalk" when _config.RandomAdditiveWalk != null =>
                new RandomAdditiveWalkConfigViewModel(_instrument, _config.RandomAdditiveWalk),
            
            _ => null
        };
    }
}
