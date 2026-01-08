using Avalonia.Media;
using EvolverCore.ViewModels;
using EvolverCore.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Models.Indicators
{
    [Serializable]
    public class SMAProperties : IndicatorProperties
    {
        public SMAProperties() { }
        public int Period { set; get; } = 14;
        public BarPriceValue PriceField { get; set; } = BarPriceValue.Close;
    }

    public class SMA : Indicator
    {
        public SMA(IndicatorProperties properties) : base(properties) { }

        public new SMAProperties Properties { get { return  (SMAProperties)base.Properties; } }

        public override void Configure()
        {
            PlotProperties plotProperties = new PlotProperties();
            plotProperties.Name = "SMA";
            plotProperties.PlotLineBrush = Brushes.Red;

            Outputs.Add(new OutputPlot("SMA",plotProperties, PlotStyle.Line));
        }

        double _sum = 0;

        public override void OnDataUpdate()
        {
            if (SourceRecord!.SourceType == CalculationSource.BarData)
            {
                BarPricePoint p = new BarPricePoint(Bars[0][0], Properties.PriceField);
                _sum += p.Y;

                if (CurrentBarIndex >= Properties.Period)
                {
                    BarPricePoint oldP = new BarPricePoint(Bars[0][Properties.Period], Properties.PriceField);
                    _sum -= oldP.Y;

                    Outputs[0][0] = _sum / Properties.Period;
                }
            }
            else if (SourceRecord!.SourceType == CalculationSource.IndicatorPlot)
            {
                _sum += Inputs[0][0];
                if (CurrentInputIndex >= Properties.Period)
                {
                    _sum -= Inputs[0][Properties.Period];
                    Outputs[0][0] = _sum / Properties.Period;
                }
            }
        }
    }
}
