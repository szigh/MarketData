using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
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

    private async Task SwitchModelAsync(string newModel, string oldModel)
    {
        IsSwitchingModel = true;
        try
        {
            var response = await _modelConfigService.SwitchModelAsync(_instrument, newModel);
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

    // Expose configuration details
    public bool HasRandomMultiplicative => _config.RandomMultiplicative != null;
    public double? RandomMultiplicativeStdDev => _config.RandomMultiplicative?.StandardDeviation;
    public double? RandomMultiplicativeMean => _config.RandomMultiplicative?.Mean;

    public bool HasMeanReverting => _config.MeanReverting != null;
    public double? MeanRevertingMean => _config.MeanReverting?.Mean;
    public double? MeanRevertingKappa => _config.MeanReverting?.Kappa;
    public double? MeanRevertingSigma => _config.MeanReverting?.Sigma;
    public double? MeanRevertingDt => _config.MeanReverting?.Dt;

    public bool IsFlatConfigured => _config.FlatConfigured;

    public bool HasRandomAdditiveWalk => _config.RandomAdditiveWalk != null;
    public int? WalkStepCount => _config.RandomAdditiveWalk?.WalkSteps.Count;
}
