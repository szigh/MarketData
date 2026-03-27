using MarketData.Client.Wpf.ViewModels;
using System.Windows;

namespace MarketData.Client.Wpf.Views;

/// <summary>
/// Interaction logic for ModelConfigWindow.xaml
/// </summary>
public partial class ModelConfigWindow : Window
{
    public ModelConfigWindow()
    {
        InitializeComponent();
    }

    public ModelConfigWindow(ModelConfigViewModel vm) : this()
    {
        DataContext = vm;
    }
}
