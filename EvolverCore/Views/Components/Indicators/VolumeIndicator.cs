using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using EvolverCore.ViewModels;
using EvolverCore.Views.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using static EvolverCore.ChartControl;

namespace EvolverCore.Views.Components
{
    public class VolumeIndicator : ChartComponentBase
    {
        private VolumeIndicatorViewModel? _vm;

        private Pen? _bullPen;
        private Pen? _bearPen;

        public VolumeIndicator(ChartPanel panel) : base(panel)
        {
        }

        public override double MinY(DateTime rangeMin, DateTime rangeMax)
        {
            return 0;
        }
        public override double MaxY(DateTime rangeMin, DateTime rangeMax)
        {
            if (_vm == null || _vm.Data == null) return 100;

            List<TimeDataBar> vBars = _vm.Data.
                Where(p => p.X >= rangeMin && p.X <= rangeMax)
                .ToList();

            return  vBars.Count >0 ? vBars.Max(p => p.Volume) : 100;
        }

        internal void SetDataContext(VolumeIndicatorViewModel vm)
        {
            if (_vm != null)
                _vm.PropertyChanged -= (_, __) => UpdateCache();

            _vm = vm;

            UpdateCache();
            _vm.PropertyChanged += (_, __) => UpdateCache();

            ChartPanelViewModel? panelVM = Parent.DataContext as ChartPanelViewModel;
            if (panelVM != null && _vm.Data != null && _vm.Data.Count > 0)
            {
                panelVM.YAxis.Min = 0;
                panelVM.YAxis.Max = _vm.Data.Max(p => p.Volume);
                if (Parent.ConnectedChartYAxis != null)
                {
                    ChartPanelViewModel? parentYAxisVM = Parent.ConnectedChartYAxis.DataContext as ChartPanelViewModel;
                    Parent.ConnectedChartYAxis.InvalidateVisual();
                }
            }

            Parent.InvalidateVisual();
        }

        private void UpdateCache()
        {
            if (_vm != null)
            {
                _bullPen = new Pen(_vm.BullBrush, 1);
            }
        }


        private void DrawHistogram(DrawingContext context)
        {
            if (_vm == null || _vm.Data == null || _vm.Data.Count == 0) return;

            ChartPanelViewModel? panelVM = Parent.DataContext as ChartPanelViewModel;
            if (panelVM == null || panelVM.XAxis == null) return;
            Rect bounds = Parent.Bounds;

            TimeSpan xSpan = panelVM.XAxis.Max - panelVM.XAxis.Min;
            double yRange = panelVM.YAxis.Max - panelVM.YAxis.Min;
            if (xSpan <= TimeSpan.Zero || yRange <= 0) return;
            double pixelsPerTick = bounds.Width / xSpan.Ticks;

            double halfBarWidth = Math.Max(2, Math.Min(12, pixelsPerTick * BarDataSeries.IntervalTicks(_vm.Data) / 2)); // auto-scale width

            foreach (var dataPoint in _vm.Data)
            {
                TimeDataBar? bar = dataPoint as TimeDataBar;
                if (bar == null) continue;

                if (bar.Time < panelVM.XAxis.Min || bar.Time > panelVM.XAxis.Max) continue;

                double xCenter = ChartPanel.MapXToScreen(panelVM.XAxis, bar.Time, bounds);
                double zeroY = ChartPanel.MapYToScreen(panelVM.YAxis, 0, bounds);
                double volumeY = ChartPanel.MapYToScreen(panelVM.YAxis, bar.Volume, bounds);

                var bodyBrush = _vm.BullBrush;
                Pen? pen = _bullPen;
                if (pen == null) { return; }

                var rect = new Rect(xCenter - halfBarWidth, volumeY, halfBarWidth * 2.0, zeroY - volumeY);

                context.FillRectangle(bodyBrush, rect);
                context.DrawRectangle(pen, rect);
            }
        }


        public override void Render(DrawingContext context)
        {
            DrawHistogram(context);
        }
    }
}