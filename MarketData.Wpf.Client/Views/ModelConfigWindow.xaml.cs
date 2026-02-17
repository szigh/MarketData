using MarketData.Wpf.Client.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MarketData.Wpf.Client.Views
{
    /// <summary>
    /// Interaction logic for ModelConfigWindow.xaml
    /// </summary>
    public partial class ModelConfigWindow : Window
    {
        public ModelConfigWindow(ModelConfigViewModel vm)
        {
            DataContext = vm;
            InitializeComponent();
        }
    }
}
