using MarketData.Client.Wpf.Bootstrapper;
using MarketData.Client.Wpf.Services;
using MarketData.Client.Wpf.ViewModels.ModelConfigs;
using MarketData.Grpc;
using MarketData.Wpf.Shared;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Windows.Input;

namespace MarketData.Client.Wpf.ViewModels;

public class ModelConfigViewModel : ViewModelBase
{
    private readonly ILogger<ModelConfigViewModel> _logger;
    private readonly IModelConfigService _modelConfigService;
    private readonly IDialogService _dialogService;
    private readonly CreateModelConfigParamsViewModel _viewModelFactory;
    private readonly string _instrument;
    private readonly string[] _supportedModels;

    private ConfigurationsResponse _configs;

    private string _activeModel;
    private int _tickIntervalMs;
    private bool _isSwitchingModel;
    private bool _activeModelChanged = false;
    private bool _tickIntervalChanged = false;
    private ModelConfigParamsViewModelBase? _activeConfigViewModel;

    public ModelConfigViewModel(
        string instrument,
        ConfigurationsResponse config,
        IEnumerable<string> supportedModels,
        IModelConfigService modelConfigService,
        CreateModelConfigParamsViewModel viewModelFactory,
        IDialogService dialogService,
        ILogger<ModelConfigViewModel> logger)
    {
        _instrument = instrument;
        _supportedModels = supportedModels.ToArray();
        _configs = config;
        _activeModel = config.ActiveModel;
        _tickIntervalMs = config.TickIntervalMs;
        _modelConfigService = modelConfigService;
        _dialogService = dialogService;
        _logger = logger;
        _viewModelFactory = viewModelFactory;

        // Create the appropriate child ViewModel based on active model
        UpdateActiveConfigViewModel();

        PublishConfigChanges = new AsyncRelayCommand(ExecutePublishChanges);
    }

    public ICommand PublishConfigChanges { get; }

    // Expose the instrument name as a read-only property for display in the UI
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

    public bool HasModifications => 
        (ActiveConfigViewModel?.IsModified ?? false) || _activeModelChanged || _tickIntervalChanged;

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

    // referenced in WPF XAML to populate model selection dropdown
    public string[] SupportedModels => _supportedModels;

    /// <summary>
    /// The ViewModel for the currently active model's configuration
    /// </summary>
    public ModelConfigParamsViewModelBase? ActiveConfigViewModel
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
        if (e.PropertyName == nameof(ModelConfigParamsViewModelBase.IsModified))
            OnPropertyChanged(nameof(HasModifications));
    }

    private async Task ExecutePublishChanges()
    {
        _logger.LogDebug("{MethodName} called", nameof(ExecutePublishChanges));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // timeout for publishing changes
        using var publisher = new ModelConfigPublisher(_instrument, _activeModel, _modelConfigService, _dialogService, _logger);

        if (_activeConfigViewModel != null && _activeConfigViewModel.IsModified)
        {
            var success = await publisher.TryPublishModelParams(_activeConfigViewModel, cts.Token);
            if (success)
            {
                // After successful publish, the ViewModel should have reset its modified state
                if (_activeConfigViewModel.IsModified)
                    throw new Exception("Model params should not be marked as modified after successful publish. " +
                        "This likely means the ActiveConfigViewModel did not properly reset its modified state.");
            }
        }
        
        if (_activeModelChanged)
        {
            IsSwitchingModel = true;
            var (success, configs) = await publisher.TrySwitchModel(cts.Token);
            if (success && configs != null)
            {
                _configs = configs;
                UpdateActiveConfigViewModel(); // Refresh the config ViewModel with new configs from server
                _activeModelChanged = false;
            }
            IsSwitchingModel = false;
        }

        if (_tickIntervalChanged)
        {
            var success = await publisher.PublishTickInterval(_tickIntervalMs, cts.Token);
            if (success)
                _tickIntervalChanged = false;
        }

        publisher.LogPublishResultsSummary();
        OnPropertyChanged(nameof(HasModifications));
    }

    private void UpdateActiveConfigViewModel()
    {
        _logger.LogInformation("Updating ActiveConfigViewModel for instrument {Instrument} with active model {ActiveModel}", _instrument, _activeModel);

        if (_configs.ActiveModel != _activeModel)
        {
            // If there is no pending local model change, this indicates a real server/client desync.
            if (!_activeModelChanged)
            {
                _logger.LogWarning(
                    "Active model in configs ({ConfigsActiveModel}) does not match expected active model ({ActiveModel}). " +
                    "This may indicate that the configs are out of sync with the selected active model.",
                    _configs.ActiveModel, _activeModel);

                _dialogService.ShowWarning(
                    $"Active model in configs ({_configs.ActiveModel}) does not match expected active model ({_activeModel}). " +
                    "This may indicate that the configs are out of sync with the selected active model.");
            }

            // Ensure the configs used to build the child ViewModel reflect the currently selected active model.
            _configs.ActiveModel = _activeModel;
        }
        ActiveConfigViewModel = _viewModelFactory(_configs);
    }
}
