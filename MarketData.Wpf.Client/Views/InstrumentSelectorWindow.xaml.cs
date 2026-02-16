using System.Windows;
using System.Windows.Controls;

namespace MarketData.Wpf.Client.ViewModels;

public partial class InstrumentSelectorWindow : Window
{
    public string? SelectedInstrument { get; private set; }

    public InstrumentSelectorWindow()
    {
        InitializeComponent();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (InstrumentComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            SelectedInstrument = selectedItem.Content.ToString();
            DialogResult = true;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
