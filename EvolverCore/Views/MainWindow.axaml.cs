using Avalonia.Controls;
using EvolverCore.ViewModels;

namespace EvolverCore.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}