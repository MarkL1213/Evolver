using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using EvolverCore.ViewModels;
using System;


namespace EvolverCore.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            
            MainWindowViewModel? vm = DataContext as MainWindowViewModel;
            ExitMenuItem.Command = new RelayCommand(ExitMenuItemCommand);
            NewChartItem.Command = vm == null ? null : vm.NewChartDocumentCommand;
            RemoveChartItem.Command = vm == null ? null : vm.RemoveChartDocumentCommand;
        }
        private void ExitMenuItemCommand()
        {
            if (App.Current != null)
            {
                ClassicDesktopStyleApplicationLifetime? app = App.Current.ApplicationLifetime as ClassicDesktopStyleApplicationLifetime;
                if (app != null) app.Shutdown();
            }
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            if (DataContext == null || !(DataContext is MainWindowViewModel)) return;
            ((MainWindowViewModel)DataContext).SaveLayoutCommand.Execute("Autosave");
        }
    }
}