using System.Windows;
using System.Windows.Controls;

namespace MarketData.Client.Wpf.Views;

public partial class InstrumentSelectorWindow : Window
{
    public string? SelectedInstrument { get; private set; }

    public InstrumentSelectorWindow(IEnumerable<string> availableInstruments)
    {
        InitializeComponent();
        InstrumentComboBox.Items.Add(new ComboBoxItem { Content = "FTSE" }); // default option
        foreach (var instrument in availableInstruments.Distinct())
        {
            if (instrument != "FTSE") // avoid adding duplicate
                InstrumentComboBox.Items.Add(new ComboBoxItem { Content = instrument });
        }
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
