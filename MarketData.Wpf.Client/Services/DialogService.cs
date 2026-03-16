using System.Windows;
using MarketData.Wpf.Client.ViewModels;

namespace MarketData.Wpf.Client.Services;

public class DialogService : IDialogService
{
    public void ShowWarning(string message, string title = "Warning")
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        });
    }

    public void ShowError(string message, string title = "Error")
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }

    public void ShowInformation(string message, string title = "Information")
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        });
    }

    public bool ShowConfirmation(string message, string title = "Confirm")
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, 
                MessageBoxImage.Question) == MessageBoxResult.Yes;
        });
    }

    public void ShowWindow<TWindow, TViewModel>(TViewModel viewModel) 
        where TWindow : Window, new()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var window = new TWindow { DataContext = viewModel };
            window.Show();
        });
    }

    public async Task<bool?> ShowDialogAsync<TWindow, TViewModel>(TViewModel viewModel) 
        where TWindow : Window, new()
    {
        return await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var window = new TWindow { DataContext = viewModel };
            return window.ShowDialog();
        });
    }

    public async Task<bool?> ShowDialogAsync<TWindow>()
        where TWindow : Window, new()
    {
        return await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var window = new TWindow();
            return window.ShowDialog();
        });
    }

    public async Task<string?> ShowInstrumentSelectorAsync(IEnumerable<string> instruments)
    {
        return await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var dialog = new InstrumentSelectorWindow(instruments);
            return dialog.ShowDialog() == true ? dialog.SelectedInstrument : null;
        });
    }
}
