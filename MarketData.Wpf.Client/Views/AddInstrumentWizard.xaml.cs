using MarketData.Client.Wpf.ViewModels.AddInstrument;
using System.ComponentModel;
using System.Windows;

namespace MarketData.Client.Wpf.Views;

/// <summary>
/// Interaction logic for AddInstrumentWizard.xaml
/// </summary>
public partial class AddInstrumentWizard : Window
{
    public AddInstrumentWizard()
    {
        InitializeComponent();

        // Wire up DialogResult property to close the window
        DataContextChanged += OnDataContextChanged;

        // Handle the X (close) button to execute Cancel logic
        Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DialogResult.HasValue)
            return;

        e.Cancel = true;

        if (DataContext is AddInstrumentWizardViewModel vm && vm.CancelCommand.CanExecute(null))
        {
            vm.CancelCommand.Execute(null);
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is AddInstrumentWizardViewModel oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;

        if (e.NewValue is AddInstrumentWizardViewModel newVm)
            newVm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AddInstrumentWizardViewModel.DialogResult) 
            && sender is AddInstrumentWizardViewModel vm 
            && vm.DialogResult.HasValue)
        {
            DialogResult = vm.DialogResult;
        }
    }
}
