using System.Collections.ObjectModel;
using System.Windows.Input;
using MarketData.Grpc;
using MarketData.Wpf.Shared;

namespace MarketData.Wpf.Client.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly MarketDataService.MarketDataServiceClient _grpcClient;
        private string _title = "Market Data Client";
        private ObservableCollection<InstrumentTabViewModel> _tabs;
        private InstrumentTabViewModel? _selectedTab;

        public MainWindowViewModel(MarketDataService.MarketDataServiceClient grpcClient)
        {
            _grpcClient = grpcClient;
            Tabs = new ObservableCollection<InstrumentTabViewModel>();

            AddTabCommand = new RelayCommand(ExecuteAddTab);
            CloseTabCommand = new RelayCommand<InstrumentTabViewModel>(ExecuteCloseTab);

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

        private void ExecuteAddTab()
        {
            var instrumentSelector = new InstrumentSelectorWindow();
            if (instrumentSelector.ShowDialog() == true)
            {
                var selectedInstrument = instrumentSelector.SelectedInstrument;
                if (!string.IsNullOrEmpty(selectedInstrument))
                {
                    AddTab(selectedInstrument);
                }
            }
        }

        private void AddTab(string instrumentName)
        {
            var instrumentViewModel = new InstrumentViewModel(_grpcClient, instrumentName);
            var tabViewModel = new InstrumentTabViewModel(instrumentViewModel);
            Tabs.Add(tabViewModel);
            SelectedTab = tabViewModel;

            _ = instrumentViewModel.StartStreamingAsync();
        }

        private async void ExecuteCloseTab(InstrumentTabViewModel? tab)
        {
            if (tab != null && Tabs.Contains(tab))
            {
                await tab.InstrumentViewModel.StopStreamingAsync();
                Tabs.Remove(tab);
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
}
