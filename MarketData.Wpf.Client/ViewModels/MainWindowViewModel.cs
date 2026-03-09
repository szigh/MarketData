using MarketData.Grpc;
using MarketData.Wpf.Shared;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace MarketData.Wpf.Client.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly InstrumentViewModelFactory _instrumentViewModelFactory;
    private readonly ModelConfigurationService.ModelConfigurationServiceClient _modelConfigurationServiceClient;
    private string _title = "Market Data Client";
    private ObservableCollection<InstrumentTabViewModel> _tabs;
    private InstrumentTabViewModel? _selectedTab;

    public MainWindowViewModel(
        InstrumentViewModelFactory instrumentViewModelFactory,
        ModelConfigurationService.ModelConfigurationServiceClient modelConfigurationServiceClient)
    {
        _instrumentViewModelFactory = instrumentViewModelFactory;
        _modelConfigurationServiceClient = modelConfigurationServiceClient;
        _tabs = [];

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

    public ICommand AddTabCommand { get; }
    public ICommand CloseTabCommand { get; }

    private async Task ExecuteAddTab()
    {
        try
        {
            var response = await _modelConfigurationServiceClient
                .GetAllInstrumentsAsync(new GetAllInstrumentsRequest());

            var instrumentSelector = new InstrumentSelectorWindow(
                response.Configurations.Select(r => r.InstrumentName));

            if (instrumentSelector.ShowDialog() == true)
            {
                var selectedInstrument = instrumentSelector.SelectedInstrument;
                if (!string.IsNullOrEmpty(selectedInstrument))
                {
                    AddTab(selectedInstrument);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to fetch instrument configurations: {ex.Message}", 
                "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddTab(string instrumentName)
    {
        var instrumentViewModel = _instrumentViewModelFactory.Create(instrumentName);
        var tabViewModel = new InstrumentTabViewModel(instrumentViewModel);
        Tabs.Add(tabViewModel);
        SelectedTab = tabViewModel;

        _ = instrumentViewModel.StartStreamingAsync();
    }

    private bool CanExecuteCloseTab(InstrumentTabViewModel? tab)
    {
        // Prevent closing the last tab
        return Tabs.Count > 1;
    }

    private async void ExecuteCloseTab(InstrumentTabViewModel? tab)
    {
        if (tab != null && Tabs.Contains(tab) && Tabs.Count > 1)
        {
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
        foreach (var tab in Tabs)
        {
            await tab.InstrumentViewModel.StopStreamingAsync();
        }
        Tabs.Clear();
    }
}
