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
using EvolverCore.Models.Indicators;
using EvolverCore.Models;
using Avalonia.Threading;

namespace EvolverCore;


internal partial class ChartControl : UserControl
{
    public ChartControl()
    {
        InitializeComponent();
        AddTestMenu();
    }

    internal bool AxisPositionTrackingData { set; get; } = true;

    public void OnDataChanged(object? sender, EventArgs args)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(new Action(() => { OnDataChanged(sender, args); }));
            return;
        }

        Indicator? indicator = sender as Indicator;
        if (indicator == null) return;


        if (PrimaryChartPanel.ContainsIndicator(indicator))
        {
            if(indicator.IsDataOnly && AxisPositionTrackingData)
                PrimaryChartPanel.UpdateXAxisRange();
            
            PrimaryChartPanel.OnDataUpdate();
        }
        else
        {
            ChartControlViewModel? vm = DataContext as ChartControlViewModel;
            if (vm == null) return;
            foreach (SubPanel panel in vm.SubPanelViewModels)
            {
                if (panel.Panel != null && panel.Panel.ContainsIndicator(indicator))
                {
                    panel.Panel.OnDataUpdate();
                    break;
                }
            }
        }

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
    private void AddDataPlotToPrimary(Indicator indicator)
    {
        indicator.DataChanged += OnDataChanged;

        PrimaryChartPanel.DetachAllChartComponents();
        DataComponent dataComponent = new DataComponent(PrimaryChartPanel);
        dataComponent.ChartPanelNumber = 0;
        
        IndicatorViewModel ? ivm = dataComponent.Properties as IndicatorViewModel;
        if (ivm == null)
        {
            throw new EvolverException("New DataComponent does not have an IndicatorViewModel");
        }
        ivm.Indicator = indicator;
        ivm.RenderOrder = 0;

        DataPlotViewModel dataProperties = new DataPlotViewModel();
        dataProperties.Indicator = ivm;
        dataProperties.Style = PlotStyle.Candlestick;

        ChartPlot dataPlot = new ChartPlot(dataComponent);
        dataPlot.Properties = dataProperties;
        dataComponent.AddPlot(dataPlot);

        PrimaryChartPanel.AttachChartComponent(dataComponent);
        PrimaryChartPanel.UpdateXAxisRange();
    }

    private void Test_AddSMAToVolume()
    {
        if (_volPanel == null || _volIndicator == null) return;

        SMAProperties smaProperties = new SMAProperties();
        smaProperties.PriceField = BarPriceValue.Close;
        smaProperties.Period = 14;

        SMA? vi = Globals.Instance.DataManager.CreateIndicator(typeof(SMA), smaProperties, _volIndicator, CalculationSource.IndicatorPlot, 0) as SMA;
        if (vi == null)
        {
            Globals.Instance.Log.LogMessage("Failed to create indicator: SMA", LogLevel.Error);
            return;
        }
        vi.DataChanged += OnDataChanged;

        IndicatorViewModel vivm = new IndicatorViewModel();
        vivm.Indicator = vi;

        IndicatorComponent component = new IndicatorComponent(_volPanel);
        component.SetDataContext(vivm);

        for (int i = 0; i < vi.Outputs.Count; i++)
        {
            OutputPlot oPlot = vi.Outputs[i];
            ChartPlotViewModel plotVM = new ChartPlotViewModel();
            plotVM.PlotIndex = i;
            plotVM.Indicator = vivm;
            plotVM.Style = oPlot.Style;

            ChartPlot plot = new ChartPlot(component);
            plot.Properties = plotVM;
            component.AddPlot(plot);
        }

        _volPanel.AttachChartComponent(component);
        _volPanel.UpdateYAxisRange();
    }

    private void Test_AddSMAToPrice()
    {
        ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        if (vm == null || vm.PrimaryChartPanelViewModel.ChartComponents.Count == 0) return;

        DataComponent? dataComponent = PrimaryChartPanel.GetFirstDataComponent();
        IndicatorViewModel? ivm = dataComponent?.Properties as IndicatorViewModel;
        if (dataComponent == null || ivm == null || ivm.Indicator == null || ivm.Indicator.InputElementCount() == 0) return;

        SMAProperties smaProperties = new SMAProperties();
        smaProperties.PriceField = BarPriceValue.Close;
        smaProperties.Period = 14;

        SMA? vi = Globals.Instance.DataManager.CreateIndicator(typeof(SMA), smaProperties, ivm.Indicator, CalculationSource.BarData) as SMA;
        if (vi == null)
        {
            Globals.Instance.Log.LogMessage("Failed to create indicator: SMA", LogLevel.Error);
            return;
        }
        vi.DataChanged += OnDataChanged;

        IndicatorViewModel vivm = new IndicatorViewModel();
        vivm.Indicator = vi;

        IndicatorComponent component = new IndicatorComponent(PrimaryChartPanel);
        component.SetDataContext(vivm);

        for (int i = 0; i < vi.Outputs.Count; i++)
        {
            OutputPlot oPlot = vi.Outputs[i];
            ChartPlotViewModel plotVM = new ChartPlotViewModel();
            plotVM.PlotIndex = i;
            plotVM.Indicator = vivm;
            plotVM.Style = oPlot.Style;

            ChartPlot plot = new ChartPlot(component);
            plot.Properties = plotVM;
            component.AddPlot(plot);
        }

        PrimaryChartPanel.AttachChartComponent(component);
        PrimaryChartPanel.UpdateYAxisRange();
    }

    ChartPanel? _volPanel;
    Volume? _volIndicator;

    private void Test_AddVolumeIndicator()
    {
        ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        if (vm == null || vm.PrimaryChartPanelViewModel.ChartComponents.Count == 0) return;

        DataComponent? dataComponent = PrimaryChartPanel.GetFirstDataComponent();
        IndicatorViewModel? ivm = dataComponent?.Properties as IndicatorViewModel;
        if (dataComponent == null || ivm == null || ivm.Indicator == null || ivm.Indicator.InputElementCount() == 0) return;

        SubPanel? panel = AddNewSubPanel();
        if (panel == null) return;
        if (panel.Panel == null || panel.Panel.DataContext == null) { RemoveSubPanel(panel.ID); return; }
        ChartPanelViewModel? panelVM = panel.Panel.DataContext as ChartPanelViewModel;
        if (panelVM == null) { RemoveSubPanel(panel.ID); return; }

        IndicatorProperties properties = new IndicatorProperties();

        Volume? vi = Globals.Instance.DataManager.CreateIndicator(typeof(Volume), properties, ivm.Indicator, CalculationSource.BarData) as Volume;
        if (vi == null)
        {
            Globals.Instance.Log.LogMessage("Failed to create indicator: Volume", LogLevel.Error);
            RemoveSubPanel(panel.ID);
            return;
        }
        vi.DataChanged += OnDataChanged;

        IndicatorViewModel vivm = new IndicatorViewModel();
        vivm.Indicator = vi;

        IndicatorComponent component = new IndicatorComponent(panel.Panel);
        component.SetDataContext(vivm);

        for (int i=0;i <vi.Outputs.Count;i++)
        {
            OutputPlot oPlot = vi.Outputs[i];
            ChartPlotViewModel plotVM = new ChartPlotViewModel();
            plotVM.PlotIndex = i;
            plotVM.Indicator = vivm;
            plotVM.Style = oPlot.Style;

            ChartPlot plot = new ChartPlot(component);
            plot.Properties = plotVM;
            component.AddPlot(plot);
        }

        panel.Panel.AttachChartComponent(component);
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
        Instrument? instrument = Globals.Instance.InstrumentCollection.Lookup("Random");
        if (instrument == null)
        {
            Globals.Instance.Log.LogMessage("Unable to locate the Random instrument.",LogLevel.Error);
            return;
        }

        DataInterval interval = new DataInterval(Interval.Hour, 1);
        DateTime startTime = new DateTime(2020, 1, 1, 8, 0, 0);
        DateTime endTime = interval.Add(startTime, size);

        Indicator? indicator = Globals.Instance.DataManager.CreateDataIndicator(instrument, interval, startTime, endTime);
        if (indicator == null)
        {
            Globals.Instance.Log.LogMessage("Unable to create data indicator.", LogLevel.Error);
            return;
        }

        AddDataPlotToPrimary(indicator);
    }
    private void Test_AddDataSeconds(int size)
    {
        ///Load some random primary data
        //BarDataSeries? barDataSeries = BarDataSeries.RandomSeries(new DateTime(2020, 1, 1, 8, 0, 0), new DataInterval(Interval.Second, 1), size);

        //if (barDataSeries != null)
        //{
        //    //AddDataPlotToPrimary(barDataSeries);
        //}
    }

    private void Test_AddDataDaily(int size)
    {
        ///Load some random primary data
        //BarDataSeries? barDataSeries = BarDataSeries.RandomSeries(new DateTime(2020, 1, 1, 8, 0, 0),new DataInterval(Interval.Day, 1), size);

        //if (barDataSeries != null)
        //{
        //    //AddDataPlotToPrimary(barDataSeries);
        //}
    }

    private void Test_AddDataMonthly(int size)
    {
        ///Load some random primary data
        //BarDataSeries? barDataSeries = BarDataSeries.RandomSeries(new DateTime(2020, 1, 1, 8, 0, 0),new DataInterval(Interval.Month, 1), size);

        //if (barDataSeries != null)
        //{
        //    //AddDataPlotToPrimary(barDataSeries);
        //}
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