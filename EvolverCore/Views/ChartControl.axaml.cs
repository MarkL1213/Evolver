using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Skia;
using System.Collections.Generic;
using System;
using EvolverCore.ViewModels;

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

    Dictionary<Guid, SubPanel> _subPanels = new Dictionary<Guid, SubPanel>();
    int _subPanelNextNumber = 1;

    internal SubPanel? AddNewChartPanel()
    {
        if (DataContext == null) { return null; }
        ChartControlViewModel? vm = (ChartControlViewModel)DataContext;
        if (vm == null) { return null; }

        int subPanelCount = vm.SubPanelViewModels.Count;

        ChartPanelViewModel subVM = new ChartPanelViewModel { XAxis = vm.SharedXAxis };
        vm.SubPanelViewModels.Add(subVM);


        int n = ChartPanelGrid.RowDefinitions.Count - 2;

        ChartPanelGrid.RowDefinitions[0].Height = new GridLength(80, GridUnitType.Star);
        ChartPanelGrid.RowDefinitions.Insert(n, new RowDefinition(3, GridUnitType.Pixel));
        ChartPanelGrid.RowDefinitions.Insert(n+1, new RowDefinition(20, GridUnitType.Star));

        Grid.SetRow(PrimaryXAxisSplitter, Grid.GetRow(PrimaryXAxisSplitter) + 2);
        Grid.SetRow(PrimaryXAxis, Grid.GetRow(PrimaryXAxis) + 2);

        SubPanel subPanel = new SubPanel();
        subPanel.ID = Guid.NewGuid();
        subPanel.Number = _subPanelNextNumber;
        subPanel.ViewModel = subVM;

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
        
        _subPanels.Add(subPanel.ID,subPanel);
        _subPanelNextNumber++;

        return subPanel;
    }
    internal void RemoveSubPanel(Guid id)
    {
        if (DataContext == null) { return; }
        ChartControlViewModel? vm = (ChartControlViewModel)DataContext;
        if (vm == null) { return; }

        if (!_subPanels.ContainsKey(id)) { return; }
        SubPanel subPanel = _subPanels[id];

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

        if (subPanel.ViewModel != null) vm.SubPanelViewModels.Remove(subPanel.ViewModel);

        int oldPanelNumber = subPanel.Number;
        _subPanels.Remove(id);
        foreach (Guid remainingPanelID in _subPanels.Keys)
        {
            SubPanel rPanel = _subPanels[remainingPanelID];

            if(rPanel.Number > oldPanelNumber) rPanel.Number--;

            if (rPanel.Panel != null) Grid.SetRow(rPanel.Panel, Grid.GetRow(rPanel.Panel) - 2);
            if (rPanel.Axis != null) Grid.SetRow(rPanel.Axis, Grid.GetRow(rPanel.Axis) - 2);
            if (rPanel.Splitter != null) Grid.SetRow(rPanel.Splitter, Grid.GetRow(rPanel.Splitter) - 2);
        }
        _subPanelNextNumber--;

        Grid.SetRow(PrimaryXAxisSplitter, Grid.GetRow(PrimaryXAxisSplitter) - 2);
        Grid.SetRow(PrimaryXAxis, Grid.GetRow(PrimaryXAxis) - 2);
    }
}