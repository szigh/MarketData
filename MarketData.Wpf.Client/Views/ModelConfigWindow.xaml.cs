using MarketData.Wpf.Client.ViewModels;
using System.Windows;

namespace MarketData.Wpf.Client.Views
{
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
}
