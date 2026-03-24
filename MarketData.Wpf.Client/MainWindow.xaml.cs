using System.Windows;
using MarketData.Client.Wpf.ViewModels;
using Microsoft.Extensions.Logging;

namespace MarketData.Client.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(MainWindowViewModel viewModel, ILogger<MainWindow> logger)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _logger = logger;
        DataContext = _viewModel;

        Closed += OnClosed;
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        _logger.LogInformation("MainWindow is closing. Closing all tabs...");
        await _viewModel.CloseAllTabsAsync();
    }
}