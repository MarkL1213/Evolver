using Avalonia;
using Avalonia.Media;
using EvolverCore.ViewModels;
using EvolverCore.Views.Components;
using System;
using System.Collections.Generic;
using System.Linq;

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

        internal void SetDataContext(VolumeIndicatorViewModel vm)
        {
            if (_vm != null)
                _vm.PropertyChanged -= (_, __) => UpdateCache();

            _vm = vm;

            UpdateCache();
            _vm.PropertyChanged += (_, __) => UpdateCache();

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
                double volumeY = ChartPanel.MapYToScreen(panelVM.YAxis, 50, bounds);//TEMP set volume to 50. get drawing working before worrying about scale problem

                var bodyBrush = _vm.BullBrush;
                Pen? pen = _bullPen;
                if (pen == null) { return; }

                var rect = new Rect(xCenter - halfBarWidth, bounds.Height - zeroY, halfBarWidth * 2.0, zeroY - volumeY);

                context.FillRectangle(bodyBrush, rect);
                context.DrawRectangle(pen, rect);
            }
        }


        public override void Render(DrawingContext context)
        {
            using (DrawingContext.PushedState p = context.PushClip(Parent.Bounds))
            {
                DrawHistogram(context);
            }
        }
    }
}