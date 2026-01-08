using Avalonia.Media;
using EvolverCore.Views;

namespace EvolverCore.Models.Indicators
{
    public class Volume : Indicator
    {
        public Volume(IndicatorProperties properties) : base(properties)
        {
            Name = "Volume";
        }

        public override void Configure()
        {
            PlotProperties plotProperties = new PlotProperties();
            OutputPlot oplot = new OutputPlot("Volume", plotProperties, PlotStyle.Bar);
            Outputs.Add(oplot);
        }

        public override void OnDataUpdate()
        {
            if (CurrentBarIndex % 5 == 0)
            {
                Outputs[0].Properties[0].PlotFillBrush = Brushes.Orange;
            }

            Outputs[0][0] = Bars[0][0].Volume;
        }

    }
}