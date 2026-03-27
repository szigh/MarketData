using MarketData.Client.Wpf.Bootstrapper;
using MarketData.Client.Wpf.Services;
using MarketData.Wpf.Shared;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace MarketData.Client.Wpf.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly CreateInstrumentViewModel _instrumentViewModelFactory;
    private readonly IModelConfigService _modelConfigService;
    private readonly IDialogService _dialogService;
    private readonly CreateAddInstrumentWizardViewModel _addInstrumentWizardFactory;
    private readonly CreateInstrumentTabViewModel _tabViewModelFactory;

    private string _title = "Market Data Client";
    private ObservableCollection<InstrumentTabViewModel> _tabs;
    private InstrumentTabViewModel? _selectedTab;

    public MainWindowViewModel(
        CreateInstrumentViewModel instrumentViewModelFactory,
        IModelConfigService modelConfigService,
        IDialogService dialogService,
        CreateAddInstrumentWizardViewModel addInstrumentWizardFactory,
        CreateInstrumentTabViewModel tabViewModelFactory,
        ILogger<MainWindowViewModel> logger)
    {
        _instrumentViewModelFactory = instrumentViewModelFactory;
        _modelConfigService = modelConfigService;
        _dialogService = dialogService;
        _addInstrumentWizardFactory = addInstrumentWizardFactory;
        _tabViewModelFactory = tabViewModelFactory;
        _logger = logger;
        _tabs = [];

        AddInstrumentCommand = new AsyncRelayCommand(ExecuteAddInstrument);
        AddTabCommand = new AsyncRelayCommand(ExecuteAddTab);
        CloseTabCommand = new RelayCommand<InstrumentTabViewModel>(ExecuteCloseTab, CanExecuteCloseTab);

        // Start with a single FTSE tab
        AddTab("FTSE");
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public ObservableCollection<InstrumentTabViewModel> Tabs
    {
        get => _tabs;
        set => SetProperty(ref _tabs, value);
    }

    public InstrumentTabViewModel? SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }

    public ICommand AddInstrumentCommand { get; }
    public ICommand AddTabCommand { get; }
    public ICommand CloseTabCommand { get; }

    private async Task ExecuteAddInstrument()
    {
        _logger.LogInformation("Executing AddInstrument");
        try
        {
            var vm = _addInstrumentWizardFactory();
            await vm.InitializeAsync();
            var addedInstrument = await _dialogService.ShowAddInstrumentWizardAsync(vm);
            if (!string.IsNullOrEmpty(addedInstrument))
            {
                AddTab(addedInstrument);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during add instrument");
            _dialogService.ShowError(ex.Message, "Exception during add instrument");
        }
    }

    private async Task ExecuteAddTab(CancellationToken ct)
    {
        _logger.LogInformation("Executing AddTabCommand");
        try
        {
            _logger.LogInformation("Fetching instrument configurations from gRPC service");
            var instrumentNames = await _modelConfigService.GetAllInstrumentsAsync(ct);
            _logger.LogInformation("Received {Count} instrument configurations", instrumentNames.Count());

            var selectedInstrument = await _dialogService.ShowInstrumentSelectorAsync(instrumentNames);
            _logger.LogInformation("User selected instrument: {Instrument}", selectedInstrument);

            if (!string.IsNullOrEmpty(selectedInstrument))
            {
                AddTab(selectedInstrument);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch instrument configurations");
            _dialogService.ShowError(ex.Message, "Failed to fetch instrument configurations");
        }
    }

    private void AddTab(string instrumentName)
    {
        _logger.LogInformation("Adding tab for instrument: {Instrument}", instrumentName);

        var instrumentViewModel = _instrumentViewModelFactory(instrumentName);
        var tabViewModel = _tabViewModelFactory(instrumentViewModel);
        Tabs.Add(tabViewModel);
        SelectedTab = tabViewModel;

        _logger.LogInformation("Starting streaming for instrument: {Instrument}", instrumentName);
        _ = instrumentViewModel.StartStreamingAsync();
    }

    private bool CanExecuteCloseTab(InstrumentTabViewModel? tab)
    {
        // Prevent closing the last tab
        return Tabs.Count > 1;
    }

    private async void ExecuteCloseTab(InstrumentTabViewModel? tab)
    {
        _logger.LogInformation("Executing CloseTabCommand for tab: {TabHeader}", tab?.Header);
        if (tab != null && Tabs.Contains(tab) && Tabs.Count > 1)
        {
            _logger.LogInformation("Stopping streaming for instrument: {Instrument}", tab.Header);
            await tab.InstrumentViewModel.StopStreamingAsync();

            // Re-check after await - tab might have been removed by another concurrent call
            if (Tabs.Contains(tab) && Tabs.Count > 1)
            {
                // If closing the selected tab, switch selection first to avoid UI binding issues
                if (SelectedTab == tab)
                {
                    var currentIndex = Tabs.IndexOf(tab);
                    // Select next tab, or previous if this is the last one
                    SelectedTab = currentIndex < Tabs.Count - 1 
                        ? Tabs[currentIndex + 1] 
                        : Tabs[currentIndex - 1];
                }
                
                Tabs.Remove(tab);
            }
        }
    }

    public async Task CloseAllTabsAsync()
    {
        _logger.LogInformation("Closing all tabs");
        foreach (var tab in Tabs)
        {
            await tab.InstrumentViewModel.StopStreamingAsync();
        }
        try 
        {
            Tabs.Clear();
        } 
        catch (NullReferenceException nre)
        {
            _logger.LogWarning(nre, "Tabs collection has already been disposed");
            // This can happen if the application is closing and the Tabs collection has already been disposed.
            // We can safely ignore this exception since we're trying to clear the tabs during shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while closing tabs");
            throw;
        }
    }
}
