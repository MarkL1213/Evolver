using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using EvolverCore.Models;
using EvolverCore.Models.DataV2;
using EvolverCore.ViewModels;
using NP.Ava.UniDock;
using NP.UniDockService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
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
            MainWindowViewModel? vm = DataContext as MainWindowViewModel;
            if (vm != null)
            {
                vm.AvailableLayouts.CollectionChanged -= RefreshAvailableLayouts;
            }

            ArrowTestItem.Command = new RelayCommand(ArrowTestItemCommand);
            ExitMenuItem.Command = new RelayCommand(ExitMenuItemCommand);
            NewLogWindow.Command = vm == null ? null : vm.NewLogDocumentCommand;
            NewChartItem.Command = vm == null ? null : vm.NewChartDocumentCommand;
            RemoveChartItem.Command = vm == null ? null : vm.RemoveChartDocumentCommand;
            ToggleDarkTheme.Command = vm == null ? null : vm.ToggleDarkModeCommand;
            SaveLayoutMenuItem.Command = new RelayCommand(SaveCurrentLayoutMenuCommand);
            if (vm != null)
            {
                vm.AvailableLayouts.CollectionChanged += RefreshAvailableLayouts;
            }

            RefreshAvailableLayoutsWork();
            RefreshConnections();
        }

        private void RefreshConnections()
        {
            ConnectionsMenuItem.Items.Clear();
            List<string> knownConnections = Globals.Instance.Connections.GetKnownConnections();

            ConnectionStatusViewModel? vm = ConnectionStatusControl.DataContext as ConnectionStatusViewModel;
            if (vm == null) return;

            foreach (string connection in knownConnections)
            {
                MenuItem cMenuItem = new MenuItem();
                cMenuItem.Header = connection;
                cMenuItem.Command = new RelayCommand<string>(vm.ConnectionMenuItemClicked);
                cMenuItem.CommandParameter = connection;

                ConnectionStatusControl icon = new ConnectionStatusControl(connection) { Width = 16, Height = 16 };
                icon.DataContext = ConnectionStatusControl.DataContext;

                cMenuItem.Icon = icon;

                ConnectionsMenuItem.Items.Add(cMenuItem);
            }
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

        private async void ArrowTestItemCommand()
        {
            try
            {
                Instrument? randomInstrument = Globals.Instance.InstrumentCollection.Lookup("Random");
                if (randomInstrument == null)
                {
                    Globals.Instance.Log.LogMessage("Random instrument not found.", LogLevel.Error);
                    return;
                }
                DataInterval interval = new DataInterval(Interval.Hour, 1);
                DateTime startTime = DateTime.Now;
                int n = 72;

                InstrumentDataSeries? series = InstrumentDataSeries.RandomSeries(randomInstrument, startTime, interval, n);
                if (series == null)
                {
                    Globals.Instance.Log.LogMessage("Unable to generate random data.", LogLevel.Error);
                    return;
                }

                //TODO: <-- write it to disk


                //read it back
                ICurrentTable table = await DataWarehouse.ReadToTableAsync(randomInstrument, interval, startTime, interval.Add(startTime, n));
                BarTable? barTable = table as BarTable;
                if (barTable == null)
                {
                    Globals.Instance.Log.LogMessage("Result ICurrentTable is not of type BarTable.", LogLevel.Error);
                    return;
                }

                //TODO: <-- compare table v series for validation of disk roudtrip


                barTable.AddColumnTest();
            }
            catch (Exception ex)
            {
                Globals.Instance.Log.LogException(ex);
            }

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
            Layout? layout = vm.AvailableLayouts.FirstOrDefault(p => p.Name == Globals.Instance.Properties.LastUsedLayout);
            if (layout != null) vm.LoadLayout(layout);
        }

    }
}