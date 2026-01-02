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
    internal class DataComponent : IndicatorComponent
    {
        public DataComponent(ChartPanel panel):base(panel)
        {
            Properties = new IndicatorViewModel();
        }

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
    }
}
