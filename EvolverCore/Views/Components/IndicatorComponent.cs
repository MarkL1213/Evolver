using Avalonia.Media;
using EvolverCore.Models;
using EvolverCore.ViewModels;
using EvolverCore.Views.Components;
using System;
using System.Collections.Generic;

namespace EvolverCore.Views
{
    public class IndicatorComponent : ChartComponentBase
    {
        internal IndicatorComponent(ChartPanel parent) : base(parent) { }
        internal List<ChartPlot> ChartPlots { get; } = new List<ChartPlot>();

        internal void AddAllPlots(IndicatorViewModel vivm)
        {
            if (vivm.Indicator == null) return;

            for (int i = 0; i < vivm.Indicator.Plots.Count; i++)
            {
                OutputPlot oPlot = vivm.Indicator.Plots[i];
                ChartPlotViewModel plotVM = new ChartPlotViewModel();
                plotVM.PlotIndex = i;
                plotVM.Indicator = vivm;
                plotVM.Style = oPlot.Style;

                ChartPlot plot = new ChartPlot(this);
                plot.Properties = plotVM;
                AddPlot(plot);
            }
        }

        internal void AddPlot(ChartPlot plot)
        {
            IndicatorViewModel? vm = Properties as IndicatorViewModel;
            if (vm == null) return;
            ChartPlots.Add(plot);
            vm.ChartPlots.Add(plot.Properties);
        }

        double _minY = 0;
        double _maxY = 100;
        public override double MinY()
        {
            return _minY;
        }
        public override double MaxY()
        {
            return _maxY;
        }

        public override void UpdateVisualRange(DateTime rangeMin, DateTime rangeMax)
        {
            IndicatorViewModel? vm = Properties as IndicatorViewModel;
            if (vm == null) return;
            ChartPanelViewModel? panelVM = Parent.DataContext as ChartPanelViewModel;
            if (panelVM == null || panelVM.XAxis == null) return;

            _minY = double.MaxValue;
            _maxY = double.MinValue;
            foreach (ChartPlot plot in ChartPlots)
            {
                double plotMin = plot.MinY(rangeMin, rangeMax);
                double plotMax = plot.MaxY(rangeMin, rangeMax);

                _minY = plotMin < _minY ? plotMin : _minY;
                _maxY = plotMax > _maxY ? plotMax : _maxY;
            }
        }


        internal bool ContainsIndicator(Indicator indicator)
        {
            IndicatorViewModel? vm = Properties as IndicatorViewModel;
            if (vm == null) return false;

            return (vm.Indicator == indicator);
        }


        public override void Render(DrawingContext context)
        {
            foreach (ChartPlot plot in ChartPlots) { plot.Render(context); }
        }
    }
}
