using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using EvolverCore.ViewModels;
using NP.Ava.UniDock;
using NP.Ava.Visuals.Controls;
using NP.UniDockService;
using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;

namespace EvolverCore.Views
{
    public partial class MainWindow : Window
    {
        private DockManager _dockManager;

        public MainWindow()
        {
            InitializeComponent();
            _dockManager = MyContainer.TheDockManager;
            _dockManager.DockItemsViewModels = new ObservableCollection<DockItemViewModelBase>();
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