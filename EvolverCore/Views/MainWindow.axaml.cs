using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using EvolverCore.Models;
using EvolverCore.ViewModels;
using NP.Ava.UniDock;
using NP.UniDockService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Threading;
using EvolverCore.Tests;

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

            RunTestItem.Command = new AsyncRelayCommand(TestItemCommand);
            //RunTestItem.Command = new RelayCommand(TestItemCommand);
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

        private async Task TestItemCommand()
        {
            try
            {
                if(!DataIntervalTests.RunAll())
                {
                    Globals.Instance.Log.LogMessage("DataInteralTest.RunAll failed.", LogLevel.Error);
                    return;
                }

                Instrument? randomInstrument = Globals.Instance.InstrumentCollection.Lookup("Random");
                if (randomInstrument == null)
                {
                    Globals.Instance.Log.LogMessage("Random instrument not found.", LogLevel.Error);
                    return;
                }
                DataInterval interval = new DataInterval(IntervalSpan.Hour, 1);
                DateTime startTime = DateTime.UtcNow;
                int numBarsToGenerate = 72;
                int seed = 69;

                BarTable originalBarTable = BarTable.GenerateRandomData(randomInstrument, interval, startTime, numBarsToGenerate, seed);
                await DataWarehouse.WritePartitionedBars(originalBarTable.Table!);

                DateTime endTime = interval.Add(startTime, numBarsToGenerate);
                BarTable readbackbarTable = await DataWarehouse.ReadToDataTableAsync(new CancellationToken(), randomInstrument, interval, startTime, endTime);

                DateTime fileStartDate = readbackbarTable.Time.GetValueAt(0);
                DateTime fileEndDate = readbackbarTable.Time.GetValueAt((int)readbackbarTable.RowCount - 1);
                Globals.Instance.Log.LogMessage($"FullTable: Start={fileStartDate} End={fileEndDate} Rows={readbackbarTable.RowCount}", LogLevel.Info);

                //if (!Test_CompareData(series, barTable))
                //{
                //    Globals.Instance.Log.LogMessage("Test compare failed.", LogLevel.Error);
                //    return;
                //}
                //else
                //    Globals.Instance.Log.LogMessage("Test compare passed.", LogLevel.Error);
            }
            catch (Exception ex)
            {
                Globals.Instance.Log.LogException(ex);
            }

        }

        //private bool Test_CompareData(InstrumentDataSeries original, BarTable readTable)
        //{
        //    if (original.Count != readTable.RowCount)
        //    {
        //        Globals.Instance.Log.LogMessage($"Row count mismatch: original {original.Count}, read {readTable.RowCount}",LogLevel.Error);
        //        return false;
        //    }

        //    for (int i = 0; i < original.Count; i++)
        //    {
        //        try
        //        {
        //            TimeDataBar origBar = original[i];

        //            if (origBar.Open != (double)readTable.Open.GetValueAt(i))
        //            {
        //                Globals.Instance.Log.LogMessage($"Mismatch at index {i}: original {origBar.Volume}, read {readTable.Volume.GetValueAt(i)}", LogLevel.Error);
        //                return false;
        //            }
        //            if (origBar.High != (double)readTable.High.GetValueAt(i))
        //            {
        //                Globals.Instance.Log.LogMessage($"Mismatch at index {i}: original {origBar.High}, read {readTable.High.GetValueAt(i)}", LogLevel.Error);
        //                return false;
        //            }
        //            if (origBar.Low != (double)readTable.Low.GetValueAt(i))
        //            {
        //                Globals.Instance.Log.LogMessage($"Mismatch at index {i}: original {origBar.Low}, read {readTable.Low.GetValueAt(i)}", LogLevel.Error);
        //                return false;
        //            }
        //            if (origBar.Close != (double)readTable.Close.GetValueAt(i))
        //            {
        //                Globals.Instance.Log.LogMessage($"Mismatch at index {i}: original {origBar.Close}, read {readTable.Close.GetValueAt(i)}", LogLevel.Error);
        //                return false;
        //            }
        //            if (origBar.Volume != (double)readTable.Volume.GetValueAt(i))
        //            {
        //                Globals.Instance.Log.LogMessage($"Mismatch at index {i}: original {origBar.Volume}, read {readTable.Volume.GetValueAt(i)}", LogLevel.Error);
        //                return false;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            Globals.Instance.Log.LogMessage($"Exception at index {i}", LogLevel.Error);
        //            Globals.Instance.Log.LogException(e);
        //        }
        //    }

        //    return true;
        //}

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