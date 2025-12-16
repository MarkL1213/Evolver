using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Skia;
using System.Collections.Generic;
using System;
using EvolverCore.ViewModels;
using EvolverCore.Views.Components;
using System.Linq;
using CommunityToolkit.Mvvm.Input;

namespace EvolverCore;


public partial class ChartControl : UserControl
{
    public ChartControl()
    {
        ChartControlViewModel vm = new ChartControlViewModel();

        InitializeComponent();
        DataContext = vm;

        PrimaryChartPanel.ClearValue(DataContextProperty);
        PrimaryChartPanel.DataContext = vm.PrimaryChartPanelViewModel;

        PrimaryYAxis.ClearValue(DataContextProperty);
        PrimaryYAxis.DataContext = vm.PrimaryChartPanelViewModel;

        PrimaryYAxis.SetConnectedChartPanel(PrimaryChartPanel);
        PrimaryChartPanel.SetConnectedChartYAxis(PrimaryYAxis);

        AddTestMenu();
    }

    #region test cases
    private void AddTestMenu()
    { 
        MenuItem testMenu = new MenuItem();
        testMenu.Header = "Test";

        MenuItem testDataClear = new MenuItem();
        testDataClear.Header = "Clear Data";
        testDataClear.Command = new RelayCommand(Test_ClearData);
        testDataClear.CommandParameter = 200;
        testMenu.Items.Add(testDataClear);

        MenuItem testDataHourlyMedium = new MenuItem();
        testDataHourlyMedium.Header = "Add Data - Hourly/Medium";
        testDataHourlyMedium.Command = new RelayCommand<int>(new Action<int>(Test_AddDataHourly));
        testDataHourlyMedium.CommandParameter = 200;
        testMenu.Items.Add(testDataHourlyMedium);

        MenuItem testDataSecondsMedium = new MenuItem();
        testDataSecondsMedium.Header = "Add Data - Seconds/Medium";
        testDataSecondsMedium.Command = new RelayCommand<int>(new Action<int>(Test_AddDataSeconds));
        testDataSecondsMedium.CommandParameter = 200;
        testMenu.Items.Add(testDataSecondsMedium);

        MenuItem testDataDailyMedium = new MenuItem();
        testDataDailyMedium.Header = "Add Data - Daily/Medium";
        testDataDailyMedium.Command = new RelayCommand<int>(new Action<int>(Test_AddDataDaily));
        testDataDailyMedium.CommandParameter = 200;
        testMenu.Items.Add(testDataDailyMedium);

        MenuItem testDataMonthlyMedium = new MenuItem();
        testDataMonthlyMedium.Header = "Add Data - Monthly/Medium";
        testDataMonthlyMedium.Command = new RelayCommand<int>(new Action<int>(Test_AddDataMonthly));
        testDataMonthlyMedium.CommandParameter = 200;
        testMenu.Items.Add(testDataMonthlyMedium);

        MenuItem testDataHourlyLarge = new MenuItem();
        testDataHourlyLarge.Header = "Add Data - Hourly/Large";
        testDataHourlyLarge.Command = new RelayCommand<int>(new Action<int>(Test_AddDataHourly));
        testDataHourlyLarge.CommandParameter = 2000;
        testMenu.Items.Add(testDataHourlyLarge);

        MenuItem testDataSecondsLarge = new MenuItem();
        testDataSecondsLarge.Header = "Add Data - Seconds/Large";
        testDataSecondsLarge.Command = new RelayCommand<int>(new Action<int>(Test_AddDataSeconds));
        testDataSecondsLarge.CommandParameter = 2000;
        testMenu.Items.Add(testDataSecondsLarge);

        MenuItem testDataDailyLarge = new MenuItem();
        testDataDailyLarge.Header = "Add Data - Daily/Large";
        testDataDailyLarge.Command = new RelayCommand<int>(new Action<int>(Test_AddDataDaily));
        testDataDailyLarge.CommandParameter = 2000;
        testMenu.Items.Add(testDataDailyLarge);

        MenuItem testDataMonthlyLarge = new MenuItem();
        testDataMonthlyLarge.Header = "Add Data - Monthly/Large";
        testDataMonthlyLarge.Command = new RelayCommand<int>(new Action<int>(Test_AddDataMonthly));
        testDataMonthlyLarge.CommandParameter = 2000;
        testMenu.Items.Add(testDataMonthlyLarge);

        testMenu.Items.Add(new Separator());

        MenuItem testAddVolume = new MenuItem();
        testAddVolume.Header = "Add Volume Indicator";
        testAddVolume.Command = new RelayCommand(Test_AddVolumeIndicator);
        testMenu.Items.Add(testAddVolume);

        ChartMenu.Items.Add(testMenu);
    }

    private void Test_AddVolumeIndicator()
    {
        ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        if (vm == null|| vm.PrimaryChartPanelViewModel.Data.Count ==0) return;

        VolumeIndicatorViewModel vivm = new VolumeIndicatorViewModel(vm.PrimaryChartPanelViewModel.Data[0]);

        SubPanel? panel = AddNewSubPanel();
        if (panel == null) return;
        if (panel.Panel == null) { RemoveSubPanel(panel.ID); return; }

        VolumeIndicator vi = new VolumeIndicator(panel.Panel);
        vi.SetDataContext(vivm);
        panel.Panel.AttachChartComponent(vi);
    }

    private void Test_RemoveVolumeIndicator()
    {

    }

    private void Test_ClearData()
    {
        ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        if (vm == null) { return; }

        vm.PrimaryChartPanelViewModel.Data.Clear();
    }

    private void Test_AddDataHourly(int size)
    {
        ///Load some random primary data
        BarDataSeries barDataSeries = new BarDataSeries();
        barDataSeries.Interval = new DataInterval(Interval.Hour, 1);
        DateTime startTime = new DateTime(2020, 1, 1, 8, 0, 0);
        Random r = new Random(DateTime.Now.Second);

        int lastClose = -1;
        for (int i = 0; i < size; i++)
        {
            int open = lastClose == -1 ? r.Next(10, 100) : lastClose;
            int close = r.Next(10, 100);
            int volume = r.Next(100, 1000);
            int high = open > close ? open + r.Next(0, 15) : close + r.Next(0, 15);
            int low = open > close ? close - r.Next(0, 15) : open - r.Next(0, 15);

            TimeDataBar bar = new TimeDataBar(startTime, open, high, low, close, volume, 0, 0);
            barDataSeries.Add(bar);
            lastClose = close;
            startTime = startTime.AddHours(1);
        }

        ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        if (vm == null) { return; }

        vm.PrimaryChartPanelViewModel.Data.Clear();
        vm.PrimaryChartPanelViewModel.Data.Add(barDataSeries);
    }

    private void Test_AddDataSeconds(int size)
    {
        ///Load some random primary data
        BarDataSeries barDataSeries = new BarDataSeries();
        barDataSeries.Interval = new DataInterval(Interval.Second, 1);
        DateTime startTime = new DateTime(2020, 1, 1, 8, 0, 0);
        Random r = new Random(DateTime.Now.Second);

        int lastClose = -1;
        for (int i = 0; i < size; i++)
        {
            int open = lastClose == -1 ? r.Next(10, 100) : lastClose;
            int close = r.Next(10, 100);
            int volume = r.Next(100, 1000);
            int high = open > close ? open + r.Next(0, 15) : close + r.Next(0, 15);
            int low = open > close ? close - r.Next(0, 15) : open - r.Next(0, 15);

            TimeDataBar bar = new TimeDataBar(startTime, open, high, low, close, volume, 0, 0);
            barDataSeries.Add(bar);
            lastClose = close;
            startTime = startTime.AddSeconds(1);
        }

        ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        if (vm == null) { return; }

        vm.PrimaryChartPanelViewModel.Data.Clear();
        vm.PrimaryChartPanelViewModel.Data.Add(barDataSeries);
    }

    private void Test_AddDataDaily(int size)
    {
        ///Load some random primary data
        BarDataSeries barDataSeries = new BarDataSeries();
        barDataSeries.Interval = new DataInterval(Interval.Day, 1);
        DateTime startTime = new DateTime(2020, 1, 1, 8, 0, 0);
        Random r = new Random(DateTime.Now.Second);

        int lastClose = -1;
        for (int i = 0; i < size; i++)
        {
            int open = lastClose == -1 ? r.Next(10, 100) : lastClose;
            int close = r.Next(10, 100);
            int volume = r.Next(100, 1000);
            int high = open > close ? open + r.Next(0, 15) : close + r.Next(0, 15);
            int low = open > close ? close - r.Next(0, 15) : open - r.Next(0, 15);

            TimeDataBar bar = new TimeDataBar(startTime, open, high, low, close, volume, 0, 0);
            barDataSeries.Add(bar);
            lastClose = close;
            startTime = startTime.AddDays(1);
        }

        ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        if (vm == null) { return; }

        vm.PrimaryChartPanelViewModel.Data.Clear();
        vm.PrimaryChartPanelViewModel.Data.Add(barDataSeries);
    }

    private void Test_AddDataMonthly(int size)
    {
        ///Load some random primary data
        BarDataSeries barDataSeries = new BarDataSeries();
        barDataSeries.Interval = new DataInterval(Interval.Month, 1);
        DateTime startTime = new DateTime(2020, 1, 1, 8, 0, 0);
        Random r = new Random(DateTime.Now.Second);

        int lastClose = -1;
        for (int i = 0; i < size; i++)
        {
            int open = lastClose == -1 ? r.Next(10, 100) : lastClose;
            int close = r.Next(10, 100);
            int volume = r.Next(100, 1000);
            int high = open > close ? open + r.Next(0, 15) : close + r.Next(0, 15);
            int low = open > close ? close - r.Next(0, 15) : open - r.Next(0, 15);

            TimeDataBar bar = new TimeDataBar(startTime, open, high, low, close, volume, 0, 0);
            barDataSeries.Add(bar);
            lastClose = close;
            startTime = startTime.AddMonths(1);
        }

        ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        if (vm == null) { return; }

        vm.PrimaryChartPanelViewModel.Data.Clear();
        vm.PrimaryChartPanelViewModel.Data.Add(barDataSeries);
    }
    #endregion

    public override void Render(DrawingContext context)
    {
        base.Render(context);
    }

    internal class SubPanel
    {
        internal int Number { set; get; }
        internal Guid ID { get; set; }
        internal ChartPanel? Panel { get; set; }
        internal ChartYAxis? Axis { get; set; }
        internal GridSplitter? Splitter { get; set; }
        internal ChartPanelViewModel? ViewModel { get; set; }
    }

    
    int _subPanelNextNumber = 1;

    internal SubPanel? AddNewSubPanel()
    {
        if (DataContext == null) { return null; }
        ChartControlViewModel? vm = (ChartControlViewModel)DataContext;
        if (vm == null) { return null; }

        int subPanelCount = vm.SubPanelViewModels.Count;

        ChartPanelViewModel subVM = new ChartPanelViewModel { XAxis = vm.SharedXAxis};
        foreach (BarDataSeries bds in vm.PrimaryChartPanelViewModel.Data)
        {
            subVM.Data.Add(bds);
        }


        SubPanel subPanel = new SubPanel();
        subPanel.ID = Guid.NewGuid();
        subPanel.Number = _subPanelNextNumber++;
        subPanel.ViewModel = subVM;

        vm.SubPanelViewModels.Add(subPanel);

        int n = ChartPanelGrid.RowDefinitions.Count - 2;

        ChartPanelGrid.RowDefinitions[0].Height = new GridLength(80, GridUnitType.Star);
        ChartPanelGrid.RowDefinitions.Insert(n, new RowDefinition(3, GridUnitType.Pixel));
        ChartPanelGrid.RowDefinitions.Insert(n+1, new RowDefinition(20, GridUnitType.Star));

        Grid.SetRow(PrimaryXAxisSplitter, Grid.GetRow(PrimaryXAxisSplitter) + 2);
        Grid.SetRow(PrimaryXAxis, Grid.GetRow(PrimaryXAxis) + 2);

        GridSplitter gs = new GridSplitter();
        Grid.SetRow(gs, (subPanelCount*2) + 1);
        Grid.SetColumn(gs, 0);
        Grid.SetColumnSpan(gs, 2);
        subPanel.Splitter = gs;
        ChartPanelGrid.Children.Add(gs);

        ChartPanel cp = new ChartPanel();
        Grid.SetRow(cp, (subPanelCount * 2) + 2);
        Grid.SetColumn(cp, 0);
        cp.DataContext = subVM;
        cp.IsSubPanel = true;
        subPanel.Panel = cp;
        ChartPanelGrid.Children.Add(cp);

        ChartYAxis cs = new ChartYAxis();
        Grid.SetRow(cs, (subPanelCount * 2) + 2);
        Grid.SetColumn(cs, 2);
        cs.DataContext = subVM;
        subPanel.Axis = cs;
        ChartPanelGrid.Children.Add(cs);

        cs.SetConnectedChartPanel(cp);
        cp.SetConnectedChartYAxis(cs);
        
        return subPanel;
    }
    internal void RemoveSubPanel(Guid id)
    {
        if (DataContext == null) { return; }
        ChartControlViewModel? vm = (ChartControlViewModel)DataContext;
        if (vm == null) { return; }

        SubPanel? subPanel = vm.SubPanelViewModels.FirstOrDefault(sp => sp.ID == id);
        if (subPanel == null) { return; }

        if (subPanel.Splitter != null)
        {
            int splitterRowValue = Grid.GetRow(subPanel.Splitter);
            RowDefinition splitterRow = ChartPanelGrid.RowDefinitions[splitterRowValue];
            ChartPanelGrid.Children.Remove(subPanel.Splitter);
            ChartPanelGrid.RowDefinitions.Remove(splitterRow);
        }

        if (subPanel.Panel != null)
        {
            int panelRowValue = Grid.GetRow(subPanel.Panel);
            RowDefinition panelRow = ChartPanelGrid.RowDefinitions[panelRowValue];
            ChartPanelGrid.Children.Remove(subPanel.Panel);
            ChartPanelGrid.RowDefinitions.Remove(panelRow);
        }

        if (subPanel.Axis != null) ChartPanelGrid.Children.Remove(subPanel.Axis);

        vm.SubPanelViewModels.Remove(subPanel);

        int oldPanelNumber = subPanel.Number;

        foreach (SubPanel rPanel in vm.SubPanelViewModels)
        {
            if (rPanel.Number > oldPanelNumber)
            {
                rPanel.Number--;

                if (rPanel.Panel != null)
                {
                    rPanel.Panel.PanelNumber = rPanel.Number;
                    if (rPanel.ViewModel != null)
                    {
                        foreach (ChartComponentViewModel ccvm in rPanel.ViewModel.ChartComponents)
                        {
                            ccvm.ChartPanelNumber = rPanel.Number;
                        }
                    }
                    Grid.SetRow(rPanel.Panel, Grid.GetRow(rPanel.Panel) - 2);
                }
                if (rPanel.Axis != null) Grid.SetRow(rPanel.Axis, Grid.GetRow(rPanel.Axis) - 2);
                if (rPanel.Splitter != null) Grid.SetRow(rPanel.Splitter, Grid.GetRow(rPanel.Splitter) - 2);
            }
        }
        _subPanelNextNumber--;

        Grid.SetRow(PrimaryXAxisSplitter, Grid.GetRow(PrimaryXAxisSplitter) - 2);
        Grid.SetRow(PrimaryXAxis, Grid.GetRow(PrimaryXAxis) - 2);
    }
}