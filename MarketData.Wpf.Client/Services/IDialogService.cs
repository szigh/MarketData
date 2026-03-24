using MarketData.Client.Wpf.ViewModels.AddInstrument;
using System.Windows;

namespace MarketData.Wpf.Client.Services;

public interface IDialogService
{
    void ShowWarning(string message, string title = "Warning");
    void ShowError(string message, string title = "Error");
    void ShowInformation(string message, string title = "Information");
    bool ShowConfirmation(string message, string title = "Confirm");
    void ShowWindow<TWindow, TViewModel>(TViewModel viewModel) 
        where TWindow : Window, new();
    Task<bool?> ShowDialogAsync<TWindow, TViewModel>(TViewModel viewModel) where TWindow : Window, new();
    Task<bool?> ShowDialogAsync<TWindow>() where TWindow : Window, new();
    Task<string?> ShowInstrumentSelectorAsync(IEnumerable<string> instruments);
    Task<string?> ShowAddInstrumentWizardAsync(AddInstrumentWizardViewModel viewModel);
}
