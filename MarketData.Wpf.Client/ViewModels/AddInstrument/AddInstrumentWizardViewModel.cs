using MarketData.Client.Wpf.ViewModels.AddInstrument.Steps;
using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Client.ViewModels.ModelConfigs;
using MarketData.Wpf.Shared;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument;

public class AddInstrumentWizardViewModel : ViewModelBase
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AddInstrumentWizardViewModel> _logger;
    private readonly IDialogService _dialogService;

    private ObservableCollection<AddInstrumentViewModelBase> _steps = new();
    private AddInstrumentViewModelBase? _currentStep => _currentStepIndex >= 0 && _currentStepIndex < _steps.Count 
        ? _steps[_currentStepIndex] 
        : null;

    private int _currentStepIndex = -1;
    private string? _addedInstrument;
    private bool _isInitialized = false;
    private bool? _dialogResult;
    private readonly ModelConfigurationService.ModelConfigurationServiceClient _modelConfigurationServiceClient;
    private readonly ModelConfigViewModelFactory _modelConfigViewModelFactory;

    public AddInstrumentWizardViewModel(
        ModelConfigurationService.ModelConfigurationServiceClient modelConfigurationServiceClient,
        ModelConfigViewModelFactory modelConfigViewModelFactory,
        ILoggerFactory loggerFactory,
        ILogger<AddInstrumentWizardViewModel> logger,
        IDialogService dialogService)
    {
        NextCommand = new AsyncRelayCommand(ExecuteNext, CanExecuteNext);
        BackCommand = new AsyncRelayCommand(ExecuteBack, CanExecuteBack);
        CancelCommand = new AsyncRelayCommand(ExecuteCancel);
        _modelConfigurationServiceClient = modelConfigurationServiceClient;
        _modelConfigViewModelFactory = modelConfigViewModelFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _dialogService = dialogService;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_isInitialized)
            return;

        try
        {
            var availableModels = await _modelConfigurationServiceClient.GetSupportedModelsAsync(
                new GetSupportedModelsRequest(), cancellationToken: ct);
            var existingInstruments = await _modelConfigurationServiceClient.GetAllInstrumentsAsync(
                new GetAllInstrumentsRequest(), cancellationToken: ct);

            _steps =
            [
                new NameInstrument(availableModels.SupportedModels,
                    existingInstruments.Configurations.Select(x => x.InstrumentName)),
                new ConfigureModelParameters(null),
                new EndScreen()
            ];

            // Subscribe to ErrorsChanged from all steps (INotifyDataErrorInfo)
            foreach (var step in _steps) step.ErrorsChanged += OnStepErrorsChanged;

            _currentStepIndex = 0;
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing AddInstrumentWizardViewModel");
            _dialogService.ShowError($"Failed to load instrument configurations. Please try again later. {ex.Message}");
            DialogResult = false; // Close the dialog on initialization failure
        }
    }

    private void OnStepErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        // When any step's validation errors change, update the Next button's enabled state
        (NextCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
    }

    public AddInstrumentViewModelBase? CurrentStep
    {
        get => _currentStep;
    }

    public int CurrentStepIndex
    {
        get => _currentStepIndex;
        set
        {
            if (SetProperty(ref _currentStepIndex, value))
            {
                OnPropertyChanged(nameof(CurrentStep));
                OnPropertyChanged(nameof(NextCommandText));
                (NextCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
                (BackCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand NextCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand CancelCommand { get; }

    public string? AddedInstrument
    {
        get => _addedInstrument;
    }

    public bool? DialogResult
    {
        get => _dialogResult;
        set => SetProperty(ref _dialogResult, value);
    }

    public string NextCommandText
    {
        get => _currentStepIndex < _steps.Count - 1 ? "Next >" : "Finish";
    }

    private async Task ExecuteCancel()
    {
        var confirmed = _dialogService.ShowConfirmation(
            "Any unsaved changes will be lost and the instrument will be removed. Do you want to continue?",
            "Cancel wizard?");

        if (!confirmed)
            return;

        // Check if there's unsaved work (instrument was added but wizard not completed)
        if (!string.IsNullOrEmpty(_addedInstrument) 
            && CurrentStepIndex != 0 // instrument should not added if in first step
            )
        {
            try
            {
                // Clean up - remove the instrument if it was added
                await RemoveInstrumentAsync();
                _logger.LogInformation("Wizard cancelled - instrument {InstrumentName} was removed", _addedInstrument);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up instrument {InstrumentName} during cancel", _addedInstrument);
                _dialogService.ShowError($"Failed to clean up instrument data: {ex.Message}");
                // Still close the window even if cleanup fails
            }
        }

        DialogResult = false; // Signals cancellation to close the window
    }

    private async Task ExecuteNext()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // timeout for step execution
        var ct = cts.Token;
        try
        {
            if (CurrentStep is NameInstrument nameVm)
            {
                // Add the instrument + set the model
                await AddInstrumentAsync(nameVm.InstrumentName, nameVm.TickIntervalMs,
                    nameVm.InitialPrice, nameVm.SelectedModel, ct);

                // Reload, with the model parameters
                var modelConfigViewModel = await ReloadInstrumentConfiguration(ct);

                // Update the ConfigureModelParameters step with the new model config view model
                if (_steps[1] is ConfigureModelParameters configureModelParametersStep)
                {
                    configureModelParametersStep.ModelConfig = modelConfigViewModel;
                }
            }

            if (CurrentStep is ConfigureModelParameters configureVm)
            {
                await TryPublishConfigAsync(configureVm, ct);

                // Reload
                var configResponse = await _modelConfigurationServiceClient.GetConfigurationsAsync(
                new GetConfigurationsRequest
                {
                    InstrumentName = _addedInstrument
                }, cancellationToken: ct);

                if (_steps[2] is EndScreen endScreenStep)
                {
                    endScreenStep.SetPropertiesAndValidate(_addedInstrument, configResponse);
                }
            }

            if (CurrentStep is EndScreen)
            {
                DialogResult = true; // Signals successful completion to close the window
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionDuringStep(ex);
        }

        if (_currentStepIndex < _steps.Count - 1)
        {
            CurrentStepIndex++;
        }
    }

    private async Task ExecuteBack()
    {
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)); // timeout for step execution
        var ct = cts.Token;

        try
        {
            if (CurrentStep is ConfigureModelParameters vm)
            {
                // If we're going back from the ConfigureModelParameters step, we need to remove the instrument we added in the previous step
                if (!string.IsNullOrEmpty(_addedInstrument))
                    await RemoveInstrumentAsync(ct);

                vm.ModelConfig = null;
            }

            if (CurrentStep is EndScreen)
            {
                // do not allow this
                _dialogService.ShowError("Cannot go back from the final step. " +
                    "Please click 'Finish' to complete the wizard, or 'Cancel' to exit and delete instrument data",
                    "Navigation Error");
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionDuringStep(ex);
        }

        if (_currentStepIndex > 0)
        {
            CurrentStepIndex--;
        }
    }

    /// <summary>
    /// Should be called when an exception occurs during a step transition (Next or Back) to handle cleanup and error reporting
    /// </summary>
    /// <param name="ex">The exception that occurred during the step transition.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleExceptionDuringStep(Exception ex)
    {
        _logger.LogError(ex, "Exception during wizard step for Add Instrument");

        //attempt to cleanup
        if (!string.IsNullOrEmpty(_addedInstrument))
        {
            try
            {
                await RemoveInstrumentAsync();
                _logger.LogInformation("Cleaned up instrument {InstrumentName} after error during Back navigation", _addedInstrument);
                _dialogService.ShowError($"An error occurred during the current step: {ex.Message}");
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx, "Failed to clean up instrument {InstrumentName} after error during Back navigation", _addedInstrument);
                _dialogService.ShowError($"An error occurred during the current step: {ex.Message}.\n" +
                    $"Cleanup was unsuccessful: {cleanupEx.Message}! Database consistency may be affected.");
            }
        }
        else
        {
            _logger.LogInformation("No instrument to clean up after error during navigation");
            _dialogService.ShowError($"An error occurred during the current step: {ex.Message}");
        }

        DialogResult = false; // Close the dialog on error
    }

    private async Task TryPublishConfigAsync(ConfigureModelParameters configureVm, CancellationToken ct)
    {
        try
        {
            var success = await configureVm.ModelConfig!.ExecutePublishConfigChangesUnsafe(ct);
            if (!success)
            {
                _logger.LogError("Failed to publish model configuration changes for instrument {InstrumentName}",
                    configureVm.ModelConfig.InstrumentName);
                throw new Exception("Failed to publish model configuration changes. Please try again.");
            }
        }
        catch (ValidationException vex)
        {
            _logger.LogWarning(vex, "Validation error when publishing model configuration changes for instrument {InstrumentName}",
                configureVm.ModelConfig!.InstrumentName);
            _dialogService.ShowError($"Validation error: {vex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error when publishing model configuration changes for instrument {InstrumentName}",
                configureVm.ModelConfig!.InstrumentName);
            _dialogService.ShowError($"An unexpected error occurred: {ex.Message}");
            throw;
        }
    }

    private async Task<ModelConfigViewModelBase> ReloadInstrumentConfiguration(CancellationToken ct)
    {
        try
        {
            var configResponse = await _modelConfigurationServiceClient.GetConfigurationsAsync(
                new GetConfigurationsRequest
                {
                    InstrumentName = _addedInstrument
                }, cancellationToken: ct);

            var modelConfigViewModel = _modelConfigViewModelFactory.Create(configResponse);
            
            if (modelConfigViewModel == null)
            {
                throw new Exception($"Failed to reload model config view model for instrument {_addedInstrument} " +
                    $"with model {configResponse.ActiveModel}");
            }

            return modelConfigViewModel;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reload instrument configuration for instrument {InstrumentName}", 
                _addedInstrument);
            throw;
        }
    }

    private async Task RemoveInstrumentAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Removing instrument {InstrumentName} as user is navigating back from ConfigureModelParameters step",
            _addedInstrument);
        // Entity Framework (on the server) will cascade delete the associated model configuration when we remove the instrument
        var res = await _modelConfigurationServiceClient.TryRemoveInstrumentAsync(new TryRemoveInstrumentRequest()
        {
            InstrumentName = _addedInstrument
        }, cancellationToken: ct);
        if (res.Removed)
        {
            _logger.LogInformation("Instrument {InstrumentName} removed successfully", _addedInstrument);
            _addedInstrument = null;
        }
        else
        {
            _logger.LogError("Failed to remove instrument {InstrumentName}. Reason: {Reason}", _addedInstrument, res.Message);
            throw new Exception($"Failed to remove instrument. Reason: {res.Message}");
        }
    }

    private async Task AddInstrumentAsync(string instrumentName, int tickIntervalMs, double initialPrice, 
        string selectedModel, CancellationToken ct)
    {
        _logger.LogInformation("Adding instrument {InstrumentName} with tick interval {TickInterval} ms and initial price {InitialPrice}",
                        instrumentName, tickIntervalMs, initialPrice);

        if (double.IsNaN(initialPrice) || double.IsInfinity(initialPrice) || initialPrice <= 0)
        {
            _logger.LogError("Invalid initial price {InitialPrice} for instrument {InstrumentName}. Initial price must be a positive real number.",
                initialPrice, instrumentName);
            throw new Exception("Initial price must be a positive real number.");
        }

        var res = await _modelConfigurationServiceClient.TryAddInstrumentAsync(new TryAddInstrumentRequest()
        {
            InstrumentName = instrumentName,
            TickIntervalMs = tickIntervalMs,
            InitialPriceValue = initialPrice,
            InitialPriceTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        }, cancellationToken: ct);

        if (res.Added)
        {
            _logger.LogInformation("Instrument {InstrumentName} added successfully", instrumentName);
            _addedInstrument = instrumentName;

            // If the instrument was added successfully,
            // we need to switch to the selected model before the next step
            // a quirk of the API at the moment is it will use a "default" model when an instrument is first added,
            // which is not what we want
            _logger.LogInformation("Setting model {ModelType} for instrument {InstrumentName}",
                selectedModel, instrumentName);

            var setModelResponse = await _modelConfigurationServiceClient.SwitchModelAsync(
                new SwitchModelRequest
                {
                    InstrumentName = instrumentName,
                    ModelType = selectedModel
                }, cancellationToken: ct);
            if (setModelResponse.NewModel == selectedModel)
            {
                _logger.LogInformation("Model {ModelType} set successfully for instrument {InstrumentName}",
                    selectedModel, instrumentName);
            }
            else
            {
                _logger.LogError("Failed to set model {ModelType} for instrument {InstrumentName}. Reason: {Reason}",
                    selectedModel, instrumentName, setModelResponse.Message);
                throw new Exception($"Failed to set model for instrument. Reason: {setModelResponse.Message}");
            }
        }
        else
        {
            _logger.LogError("Failed to add instrument {InstrumentName}. Reason: {Reason}", instrumentName, res.Message);
            throw new Exception($"Failed to add instrument. Reason: {res.Message}");
        }
    }

    private bool CanExecuteBack()
    {
        return _currentStepIndex == 1;
    }

    private bool CanExecuteNext()
    {
        return (!(CurrentStep?.HasErrors)) ?? false;
    }
}
