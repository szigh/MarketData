using System.Collections.ObjectModel;
using System.Windows.Input;
using MarketData.Wpf.Shared;

namespace MarketData.Wpf.Client.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly InstrumentViewModelFactory _instrumentViewModelFactory;
        private string _title = "Market Data Client";
        private ObservableCollection<InstrumentTabViewModel> _tabs;
        private InstrumentTabViewModel? _selectedTab;

        public MainWindowViewModel(InstrumentViewModelFactory instrumentViewModelFactory)
        {
            _instrumentViewModelFactory = instrumentViewModelFactory;
            _tabs = [];

            AddTabCommand = new RelayCommand(ExecuteAddTab);
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
}
