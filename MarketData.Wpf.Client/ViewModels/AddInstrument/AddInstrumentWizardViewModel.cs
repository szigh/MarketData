using MarketData.Client.Wpf.ViewModels.AddInstrument.Steps;
using MarketData.Grpc;
using MarketData.Wpf.Client.Services;
using MarketData.Wpf.Shared;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MarketData.Client.Wpf.ViewModels.AddInstrument
{
    public class AddInstrumentWizardViewModel : ViewModelBase
    {
        private ObservableCollection<AddInstrumentViewModelBase> _steps = new();
        private AddInstrumentViewModelBase? _currentStep => _currentStepIndex >= 0 && _currentStepIndex < _steps.Count ? _steps[_currentStepIndex] : null;
        private int _currentStepIndex = -1;
        private string? _addedInstrument;
        private bool _isInitialized = false;
        private readonly ModelConfigurationService.ModelConfigurationServiceClient _modelConfigurationServiceClient;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<AddInstrumentWizardViewModel> _logger;
        private readonly IDialogService _dialogService;

        public AddInstrumentWizardViewModel(
            ModelConfigurationService.ModelConfigurationServiceClient modelConfigurationServiceClient,
            ILoggerFactory loggerFactory,
            ILogger<AddInstrumentWizardViewModel> logger,
            IDialogService dialogService)
        {
            NextCommand = new AsyncRelayCommand(ExecuteNext, CanExecuteNext);
            BackCommand = new AsyncRelayCommand(ExecuteBack, CanExecuteBack);
            CancelCommand = new AsyncRelayCommand(ExecuteCancel);
            _modelConfigurationServiceClient = modelConfigurationServiceClient;
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
                    new ConfigureModelParameters(),
                    new EndScreen()
                ];

                _currentStepIndex = 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing AddInstrumentWizardViewModel");
                _dialogService.ShowError($"Failed to load instrument configurations. Please try again later. {ex.Message}");
            }
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
                    NextCommand.CanExecute(null);
                    BackCommand.CanExecute(null);
                }
            }
        }

        public string NextCommandText
        {
            get => _currentStepIndex < _steps.Count - 1 ? "Next >" : "Finish";
        }

        private async Task ExecuteCancel()
        {
            throw new NotImplementedException();
        }

        private async Task ExecuteBack()
        {
            throw new NotImplementedException();
        }

        private bool CanExecuteBack()
        {
            return _currentStepIndex > 0;
        }

        private bool CanExecuteNext()
        {
            return CurrentStep?.IsValid() ?? false;
        }

        private async Task ExecuteNext()
        {
            if (_currentStepIndex < _steps.Count - 1)
            {
                CurrentStepIndex++;
            }
        }

        public ICommand NextCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand CancelCommand { get; }

        public string? AddedInstrument
        {
            get => _addedInstrument;
        }
    }
}
