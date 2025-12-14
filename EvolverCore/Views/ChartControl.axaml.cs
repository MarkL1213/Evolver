using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Skia;
using System.Collections.Generic;
using System;
using EvolverCore.ViewModels;
using System.Linq;
using CommunityToolkit.Mvvm.Input;

namespace EvolverCore;


public partial class ChartControl : UserControl
{
    public ChartControl()
    {
        ChartControlViewModel vm =  new ChartControlViewModel();
        
        InitializeComponent();
        DataContext = vm;

        PrimaryChartPanel.ClearValue(DataContextProperty);
        PrimaryChartPanel.DataContext = vm.PrimaryChartPanelViewModel;
        
        PrimaryYAxis.ClearValue(DataContextProperty);
        PrimaryYAxis.DataContext = vm.PrimaryChartPanelViewModel;

        PrimaryYAxis.SetConnectedChartPanel(PrimaryChartPanel);
        PrimaryChartPanel.SetConnectedChartYAxis(PrimaryYAxis);

        MenuItem testMenu= new MenuItem();
        testMenu.Header = "Test - Add Random BarData";
        testMenu.Command = new RelayCommand(Test_AddRandomBarData);
        ChartMenu.Items.Add(testMenu);
    }

    private void Test_AddRandomBarData()
    {
        ///Load some random primary data
        BarDataSeries barDataSeries = new BarDataSeries();
        DateTime startTime = new DateTime(2020,1,1,8,0,0);
        Random r = new Random(DateTime.Now.Second);

        int lastClose = -1;
        for (int i = 0; i < 200; i++)
        {
            startTime.AddHours(1);

            int open = lastClose==-1?r.Next(10, 100):lastClose;
            int close = r.Next(10, 100);
            int volume = r.Next(100, 1000);
            int high = open > close ? open + r.Next(0,15) : close + r.Next(0,15);
            int low = open > close ? close - r.Next(0, 15) : open - r.Next(0, 15);

            TimeDataBar bar = new TimeDataBar(startTime, open, high, low, close, volume, 0, 0);
            barDataSeries.Add(bar);
            lastClose = close;
        }

        ChartControlViewModel? vm = DataContext as ChartControlViewModel;
        if (vm == null) { return; }

        vm.PrimaryChartPanelViewModel.Data.Clear();
        vm.PrimaryChartPanelViewModel.Data.Add(barDataSeries);
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

        ChartPanelViewModel subVM = new ChartPanelViewModel { XAxis = vm.SharedXAxis };

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