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
    internal class DataComponent : ChartComponentBase
    {
        public DataComponent(ChartPanel panel):base(panel)
        {
            Properties = new DataViewModel();
        }

        public ChartPlot? Plot { get; private set; }

        public override double MinY()
        {
            if (SnapPoints.Count == 0) CalculateSnapPoints();
            if (SnapPoints.Count == 0) return 0;

            return SnapPoints.Min(b => { TimeDataBar? x = b as TimeDataBar; if (x != null) return x.Low; else return 0; });
        }
        public override double MaxY()
        {
            if (SnapPoints.Count == 0) CalculateSnapPoints();
            if (SnapPoints.Count == 0) return 100;

            return SnapPoints.Max(b => { TimeDataBar? x = b as TimeDataBar; if (x != null) return x.High; else return 100; });
        }

        internal void AddPlot(ChartPlot plot)
        {
            DataViewModel? vm = Properties as DataViewModel;
            if (vm == null) return;
            Plot = plot;
            vm.DataPlot =plot.Properties as DataPlotViewModel;
        }

        public override void Render(DrawingContext context)
        {
            if(Plot!=null) Plot.Render(context);
        }
    }
}
