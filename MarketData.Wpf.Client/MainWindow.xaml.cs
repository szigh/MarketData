using System.Windows;
using MarketData.Wpf.Client.ViewModels;

namespace MarketData.Wpf.Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Closed += OnClosed;
        }

        private async void OnClosed(object? sender, EventArgs e)
        {
            await _viewModel.CloseAllTabsAsync();
        }
    }
}