using Avalonia.Media;
using EvolverCore.Views;
using System;

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

        public new SMAProperties Properties { get { return (SMAProperties)base.Properties; } }

        public override void Configure()
        {
            PlotProperties plotProperties = new PlotProperties();
            plotProperties.Name = "SMA";
            plotProperties.PlotLineBrush = Brushes.Red;

            AddOutput(new OutputPlot("SMA", plotProperties, PlotStyle.Line));
        }

        double _sum = 0;

        public override void OnDataUpdate()
        {
            if (SourceRecord!.SourceType == CalculationSource.BarData)
            {
                double p = Bars[0].CalculatePriceField(0, Properties.PriceField);
                _sum += p;

                if (CurrentBarIndex + 1 >= Properties.Period)
                {
                    double oldP = Bars[0].CalculatePriceField(Properties.Period - 1, Properties.PriceField);
                    _sum -= oldP;

                    Outputs[0][0] = _sum / Properties.Period;
                }
            }
            else if (SourceRecord!.SourceType == CalculationSource.IndicatorPlot)
            {
                _sum += Inputs[0][0];
                if (CurrentInputIndex + 1 >= Properties.Period)
                {
                    _sum -= Inputs[0][Properties.Period - 1];
                    Outputs[0][0] = _sum / Properties.Period;
                }
            }
        }
    }
}
