using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using EvolverCore.ViewModels;

namespace EvolverCore.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();

            ExitMenuItem.Command = new RelayCommand(ExitMenuItemCommand);
        }

        private void ExitMenuItemCommand()
        {
            ClassicDesktopStyleApplicationLifetime app = new ClassicDesktopStyleApplicationLifetime();
            app.Shutdown();
        }
    }
}