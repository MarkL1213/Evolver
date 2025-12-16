using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EvolverCore.ViewModels;
using EvolverCore.Views.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Views
{
    public class Indicator : ChartComponentBase
    {
        internal Indicator(ChartPanel parent):base(parent) { }
        internal List<ChartPlot> ChartPlots { get; } = new List<ChartPlot>();

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

        public override void Render(DrawingContext context)
        {
            foreach (ChartPlot plot in ChartPlots) { plot.Render(context); }
        }
    }
}
