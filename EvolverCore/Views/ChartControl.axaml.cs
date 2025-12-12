using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Skia;
using System.Collections.Generic;
using System.Linq;
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

    public int AddNewChartPanel()
    {
        if (DataContext == null) { return -1; }
        ChartControlViewModel? vm = (ChartControlViewModel)DataContext;
        if (vm == null) { return -1; }

        int subPanelCount = vm.SubPanelViewModels.Count;

        ChartPanelViewModel subVM = new ChartPanelViewModel { XAxis = vm.SharedXAxis };
        vm.SubPanelViewModels.Add(subVM);

        ChartPanelGrid.RowDefinitions[0].Height = new GridLength(80, GridUnitType.Star);
        ChartPanelGrid.RowDefinitions.Add(new RowDefinition(3, GridUnitType.Pixel));
        ChartPanelGrid.RowDefinitions.Add(new RowDefinition(20, GridUnitType.Star));

        GridSplitter gs = new GridSplitter();
        gs.Name = $"SubSplitter-{subPanelCount}";
        Grid.SetRow(gs, (subPanelCount*2) + 1);
        Grid.SetColumn(gs, 0);
        Grid.SetColumnSpan(gs, 2);
        ChartPanelGrid.Children.Add(gs);

        ChartPanel cp = new ChartPanel();
        cp.Name = $"SubPanel-{subPanelCount}";
        Grid.SetRow(cp, (subPanelCount * 2) + 2);
        Grid.SetColumn(cp, 0);
        cp.DataContext = subVM;
        ChartPanelGrid.Children.Add(cp);

        ChartYAxis cs = new ChartYAxis();
        cs.Name = $"SubScale-{subPanelCount}";
        Grid.SetRow(cs, (subPanelCount * 2) + 2);
        Grid.SetColumn(cs, 2);
        cs.DataContext = subVM;
        ChartPanelGrid.Children.Add(cs);

        cs.SetConnectedChartPanel(cp);
        cp.SetConnectedChartYAxis(cs);
        
        return subPanelCount;
    }

    //FIXME: give panels an ID that is independant of their container index
    //FIXME: account for the new XAxis and splitter when removing rows!

    public void RemoveSubPanel(int index)
    {
        if (DataContext == null) { return; }
        ChartControlViewModel? vm = (ChartControlViewModel)DataContext;
        if (vm == null) { return; }

        vm.SubPanelViewModels.RemoveAt(index);

        string splitterName = $"SubSplitter-{index}";
        GridSplitter? gs = null;

        string panelName = $"SubPanel-{index}";
        ChartPanel? cp = null;

        string scaleName = $"SubScale-{index}";
        ChartYAxis? cs = null;

        foreach (Control c in ChartPanelGrid.Children)
        {
            if (c.Name == splitterName) gs = c as GridSplitter;
            else if (c.Name == panelName) cp = c as ChartPanel;
            else if (c.Name == scaleName) cs = c as ChartYAxis;
        }

        if (gs != null) ChartPanelGrid.Children.Remove(gs);
        if (cp != null) ChartPanelGrid.Children.Remove(cp);
        if (cs != null) ChartPanelGrid.Children.Remove(cs);

        for (int i = 3; i < ChartPanelGrid.Children.Count; i++)
        {
            Control c = ChartPanelGrid.Children[i];
            int r = Grid.GetRow(c);
            Grid.SetRow(c, r - 2);
        }

        ChartPanelGrid.RowDefinitions.RemoveAt(ChartPanelGrid.RowDefinitions.Count - 1);
        ChartPanelGrid.RowDefinitions.RemoveAt(ChartPanelGrid.RowDefinitions.Count - 1);
    }
}