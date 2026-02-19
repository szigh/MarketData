using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Client.ViewModels.ModelConfigs;
using MarketData.Wpf.Shared;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace MarketData.Wpf.Client.ViewModels;

public class ModelConfigViewModel : ViewModelBase
{
    private readonly ModelConfigService _modelConfigService;
    private readonly ModelConfigViewModelFactory _viewModelFactory;
    private readonly string _instrument;
    private readonly string[] _supportedModels;

    private ConfigurationsResponse _config;
    private string _activeModel;
    private int _tickIntervalMs;
    private bool _isSwitchingModel;
    private bool _activeModelChanged = false;
    private bool _tickIntervalChanged = false;
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
        _tickIntervalMs = config.TickIntervalMs;
        _modelConfigService = modelConfigService;
        _viewModelFactory = new ModelConfigViewModelFactory(instrument, modelConfigService);

        // Create the appropriate child ViewModel based on active model
        UpdateActiveConfigViewModel();

        PublishConfigChanges = new AsyncRelayCommand(ExecutePublishChanges);
    }

    public ICommand PublishConfigChanges { get; }

    public string Instrument => _instrument;

    public int TickIntervalMs
    {
        get => _tickIntervalMs;
        set
        {
            if (_tickIntervalMs != value)
            {
                SetProperty(ref _tickIntervalMs, value);
                _tickIntervalChanged = true;
                OnPropertyChanged(nameof(HasModifications));
            }
        }
    }

    public bool HasModifications
    {
        get
        {
            return (ActiveConfigViewModel?.IsModified ?? false) || _activeModelChanged || _tickIntervalChanged;
        }
    }

    public string ActiveModel
    {
        get => _activeModel;
        set
        {
            if (_activeModel != value && !_isSwitchingModel)
            {
                SetProperty(ref _activeModel, value); 
                UpdateActiveConfigViewModel(); // Update UI immediately
                _activeModelChanged = true;
                OnPropertyChanged(nameof(HasModifications));
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
        private set
        {
            _activeConfigViewModel?.PropertyChanged -= OnActiveConfigViewModelPropertyChanged;

            SetProperty(ref _activeConfigViewModel, value);

            _activeConfigViewModel?.PropertyChanged += OnActiveConfigViewModelPropertyChanged;

            OnPropertyChanged(nameof(HasModifications));
        }
    }

    private void OnActiveConfigViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ModelConfigViewModelBase.IsModified))
            OnPropertyChanged(nameof(HasModifications));
    }

    private async Task ExecutePublishChanges()
    {
        if (_activeConfigViewModel != null)
        {
            try
            {
                await _activeConfigViewModel.ExecutePublishConfigChangesSafe();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update model parameters: {ex.Message}",
                    "Error updating model parameters",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        if (_activeModelChanged)
        {
            try
            {
                var success = await SwitchModelAsync(_activeModel, _activeModel); // Switch to the same model to trigger reload
                if (success)
                    _activeModelChanged = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to switch model: {ex.Message}",
                    "Error switching model",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        if (_tickIntervalChanged)
        {
            try
            {
                var result = await _modelConfigService.UpdateTickIntervalAsync(_instrument, _tickIntervalMs);
                if (result.Success)
                    _tickIntervalChanged = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update tick interval: {ex.Message}",
                    "Error updating tick interval",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task<bool> SwitchModelAsync(string newModel, string oldModel)
    {
        IsSwitchingModel = true;
        try
        {
            await _modelConfigService.SwitchModelAsync(_instrument, newModel);
            
            // Reload configuration to get the new model's config
            _config = await _modelConfigService.GetConfigurationsAsync(_instrument);
            UpdateActiveConfigViewModel();
            return true;
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

            return false;
        }
        finally
        {
            IsSwitchingModel = false;
        }
    }

    private void UpdateActiveConfigViewModel()
    {
        ActiveConfigViewModel = _viewModelFactory.Create(_activeModel, _config);
    }
}
