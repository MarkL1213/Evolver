using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using EvolverCore.ViewModels;
using EvolverCore.Models;
using NP.Ava.UniDock;
using NP.Ava.Visuals.Controls;
using NP.UniDockService;
using System;
using System.Collections.ObjectModel;
using System.Security.Cryptography.X509Certificates;
using System.Linq;

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

            Closing += OnClosing;
        }

        private void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            Globals.Instance.SaveProperties();

            if (DataContext == null || !(DataContext is MainWindowViewModel)) return;

            ((MainWindowViewModel)DataContext).SaveLayoutCommand.Execute(new Layout() { Name = "Autosave" });
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            MainWindowViewModel? vm= DataContext as MainWindowViewModel;
            if (vm != null)
            {
                vm.AvailableLayouts.CollectionChanged -= RefreshAvailableLayouts;
            }

            ExitMenuItem.Command = new RelayCommand(ExitMenuItemCommand);
            NewChartItem.Command = vm == null ? null : vm.NewChartDocumentCommand;
            RemoveChartItem.Command = vm == null ? null : vm.RemoveChartDocumentCommand;
            SaveLayoutMenuItem.Command = new RelayCommand(SaveCurrentLayoutMenuCommand);
            if (vm != null)
            {
                vm.AvailableLayouts.CollectionChanged += RefreshAvailableLayouts;
            }

            RefreshAvailableLayoutsWork();
        }

        private void RefreshAvailableLayouts(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            RefreshAvailableLayoutsWork();
        }
        
        private void RefreshAvailableLayoutsWork()
        {
            MainWindowViewModel? vm = DataContext as MainWindowViewModel;
            if (vm == null) return;

            //HD work - async await
            //vm.LoadAvailableLayouts();

            //UI work - invoke ui thread
            LayoutMenuItem.Items.Clear();
            foreach (var layout in vm.AvailableLayouts)
            {
                MenuItem layoutItem = new MenuItem();
                layoutItem.Header = layout.Name;
                layoutItem.Command = new RelayCommand<Layout>(new Action<Layout?>(LoadLayoutMenuCommand));
                layoutItem.CommandParameter = layout;
                
                LayoutMenuItem.Items.Add(layoutItem);
            }
            LayoutMenuItem.Items.Add(new Separator());
            LayoutMenuItem.Items.Add(SaveLayoutMenuItem);
        }

        private void SaveCurrentLayoutMenuCommand()
        {
            MainWindowViewModel? vm = DataContext as MainWindowViewModel;
            if (vm == null) return;

            if (vm.CurrentLayout == null)
            {
                vm.CurrentLayout = new Layout();
                vm.CurrentLayout.Name = "Default";
                if (!vm.CurrentLayout.DirectoryExists) vm.CurrentLayout.CreateDirectory();
            }

            vm.SaveLayout(vm.CurrentLayout);
            Globals.Instance.Properties.LastUsedLayout = vm.CurrentLayout.Name;
        }

        private void LoadLayoutMenuCommand(Layout? layout)
        {
            MainWindowViewModel? vm = DataContext as MainWindowViewModel;
            if (vm == null || layout == null) return;

            vm.LoadLayout(layout);
            vm.CurrentLayout = layout;
        }

        private void ExitMenuItemCommand()
        {
            if (App.Current != null)
            {
                ClassicDesktopStyleApplicationLifetime? app = App.Current.ApplicationLifetime as ClassicDesktopStyleApplicationLifetime;
                if (app != null) app.Shutdown();
            }
        }

        public void LoadLastUsedLayout()
        {
            if (string.IsNullOrEmpty(Globals.Instance.Properties.LastUsedLayout)) return;
            MainWindowViewModel? vm = DataContext as MainWindowViewModel;
            if (vm == null) return;
            Layout? layout=vm.AvailableLayouts.FirstOrDefault(p=>p.Name == Globals.Instance.Properties.LastUsedLayout);
            if (layout != null) vm.LoadLayout(layout);
        }

    }
}