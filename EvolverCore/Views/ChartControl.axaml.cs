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
using EvolverCore.Views;
using EvolverCore.Views.Components.Indicators;
using EvolverCore.ViewModels.Indicators;
using Dock.Model.Mvvm.Controls;
using Dock.Model.Core;
using Dock.Avalonia.Controls;

namespace EvolverCore;


internal partial class ChartControl : UserControl
{
    public ChartControl()
    {
        InitializeComponent();
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

        MenuItem testAddSMA = new MenuItem();
        testAddSMA.Header = "Add SMA to Price";
        testAddSMA.Command = new RelayCommand(Test_AddSMAToPrice);
        testMenu.Items.Add(testAddSMA);

        MenuItem testAddSMAToV = new MenuItem();
        testAddSMAToV.Header = "Add SMA to Volume";
        testAddSMAToV.Command = new RelayCommand(Test_AddSMAToVolume);
        testMenu.Items.Add(testAddSMAToV);
        ChartMenu.Items.Add(testMenu);
    }
    private void AddDataPlotToPrimary(BarDataSeries barDataSeries)
    {
        PrimaryChartPanel.DetachAllChartComponents();
        Data dataComponent = new Data(PrimaryChartPanel);
        dataComponent.ChartPanelNumber = 0;
        dataComponent.Properties.Data = barDataSeries;
        dataComponent.Properties.RenderOrder = 0;

        DataPlotViewModel dataProperties = new DataPlotViewModel();
        dataProperties.Component = dataComponent.Properties;
        dataProperties.Style = PlotStyle.Candlestick;

        ChartPlot dataPlot = new ChartPlot(dataComponent);
        dataPlot.Properties = dataProperties;
        dataComponent.AddPlot(dataPlot);

        PrimaryChartPanel.AttachChartComponent(dataComponent);
        PrimaryChartPanel.UpdateXAxisRange();
    }

    private void Test_AddSMAToVolume()
    {
        ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        if (vm == null || vm.PrimaryChartPanelViewModel.ChartComponents.Count == 0) return;

        Data? dataComponent = PrimaryChartPanel.GetFirstDataComponent();
        if (dataComponent == null || dataComponent.Properties.Data == null) return;

        SMAViewModel vivm = new SMAViewModel(dataComponent.Properties.Data, 30);

        //FIXME: temporarily using captured values, should be lookup based
        ChartPanel volumePanel = _volPanel;
        Volume volumeIndicator = _volIndicator;
        int volumePlotIndex = 0;

        SMA vi = new SMA(volumePanel);
        vi.SetDataContext(vivm);
        vivm.Source = CalculationSource.IndicatorPlot;
        vivm.SourceIndicator = volumeIndicator.Properties as IndicatorViewModel;
        vivm.RenderOrder = 1;
        vivm.SourcePlotIndex = volumePlotIndex;
        vivm.ChartPlots[0].PlotLineBrush = Brushes.Orange;
        vivm.ChartPlots[0].PlotLineThickness = 3;


        vi.Calculate();
        
        
        
        volumePanel.AttachChartComponent(vi);

        //panel.Panel.UpdateYAxisRange();
    }

    private void Test_AddSMAToPrice()
    {
        ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        if (vm == null || vm.PrimaryChartPanelViewModel.ChartComponents.Count == 0) return;

        Data? dataComponent = PrimaryChartPanel.GetFirstDataComponent();
        if (dataComponent == null || dataComponent.Properties.Data == null) return;

        SMAViewModel vivm = new SMAViewModel(dataComponent.Properties.Data, 30);

        SMA vi = new SMA(PrimaryChartPanel);
        vi.SetDataContext(vivm);
        vivm.ChartPlots[0].PriceField = BarPriceValue.HLC;
        
        vi.Calculate();
        PrimaryChartPanel.AttachChartComponent(vi);

        //panel.Panel.UpdateYAxisRange();
    }

    ChartPanel _volPanel;
    Volume _volIndicator;

    private void Test_AddVolumeIndicator()
    {
        ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        if (vm == null|| vm.PrimaryChartPanelViewModel.ChartComponents.Count == 0) return;

        Data? dataComponent = PrimaryChartPanel.GetFirstDataComponent();
        if(dataComponent == null || dataComponent.Properties.Data == null) return;

        VolumeViewModel vivm = new VolumeViewModel(dataComponent.Properties.Data);

        SubPanel? panel = AddNewSubPanel();
        if (panel == null) return;
        if (panel.Panel == null || panel.Panel.DataContext == null) { RemoveSubPanel(panel.ID); return; }
        ChartPanelViewModel? panelVM = panel.Panel.DataContext as ChartPanelViewModel;
        if (panelVM == null) { RemoveSubPanel(panel.ID); return; }

        Volume vi = new Volume(panel.Panel);
        vi.SetDataContext(vivm);

        vi.Calculate();
        panel.Panel.AttachChartComponent(vi);
        panel.Panel.UpdateYAxisRange();

        _volPanel = panel.Panel;
        _volIndicator = vi;
    }

    private void Test_RemoveVolumeIndicator()
    {

    }

    private void Test_ClearData()
    {
        PrimaryChartPanel.DetachAllChartComponents();
    }

    private void Test_AddDataHourly(int size)
    {
        ///Load some random primary data
        BarDataSeries? barDataSeries = BarDataSeries.RandomSeries(new DateTime(2020, 1, 1, 8, 0, 0),new DataInterval(Interval.Hour, 1), size);

        if (barDataSeries != null)
        {
            AddDataPlotToPrimary(barDataSeries);
        }
    }
        private void Test_AddDataSeconds(int size)
    {
        ///Load some random primary data
        BarDataSeries? barDataSeries = BarDataSeries.RandomSeries(new DateTime(2020, 1, 1, 8, 0, 0),new DataInterval(Interval.Second, 1), size);

        if (barDataSeries != null)
        {
            AddDataPlotToPrimary(barDataSeries);
        }
    }

    private void Test_AddDataDaily(int size)
    {
        ///Load some random primary data
        BarDataSeries? barDataSeries = BarDataSeries.RandomSeries(new DateTime(2020, 1, 1, 8, 0, 0),new DataInterval(Interval.Day, 1), size);

        if (barDataSeries != null)
        {
            AddDataPlotToPrimary(barDataSeries);
        }
    }

    private void Test_AddDataMonthly(int size)
    {
        ///Load some random primary data
        BarDataSeries? barDataSeries = BarDataSeries.RandomSeries(new DateTime(2020, 1, 1, 8, 0, 0),new DataInterval(Interval.Month, 1), size);

        if (barDataSeries != null)
        {
            AddDataPlotToPrimary(barDataSeries);
        }
    }
    #endregion

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        //ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        
        if (DataContext is ChartControlViewModel vm)
        {
            PrimaryChartPanel.ClearValue(DataContextProperty);
            PrimaryChartPanel.DataContext = vm.PrimaryChartPanelViewModel;

            PrimaryYAxis.ClearValue(DataContextProperty);
            PrimaryYAxis.DataContext = vm.PrimaryChartPanelViewModel;
        }
        PrimaryYAxis.SetConnectedChartPanel(PrimaryChartPanel);
        PrimaryChartPanel.SetConnectedChartYAxis(PrimaryYAxis);

        PrimaryXAxis.DataPanel = PrimaryChartPanel;

        PrimaryChartPanel.InvalidateVisual();
    }

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
        //foreach (BarDataSeries bds in vm.PrimaryChartPanelViewModel.Data)
        //{
        //    subVM.Data.Add(bds);
        //}


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
        //cp.IsSubPanel = true;
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