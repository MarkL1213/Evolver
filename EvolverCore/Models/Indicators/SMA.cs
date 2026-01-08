using EvolverCore.ViewModels;
using EvolverCore.ViewModels.Indicators;
using EvolverCore.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Models.Indicators
{
    public class SMA : Indicator
    {
        public SMA(IndicatorProperties properties) : base(properties) { }

        public void ConfigurePlots()
        {
            PlotProperties plotProperties = new PlotProperties();
            plotProperties.Name = "SMA";

            Outputs.Add(new OutputPlot("SMA",plotProperties, PlotStyle.Line));
        }

        //public override void Calculate()
        //{
        //    if (Properties.Source == CalculationSource.BarData)
        //        calculateFromBarData();
        //    else if (Properties.Source == CalculationSource.IndicatorPlot)
        //        calculateFromIndicator();
        //}   

        //private void calculateFromBarData()
        //{
        //    SMAViewModel? smaVM = Properties as SMAViewModel;
        //    if (smaVM == null || smaVM.Period < 1) return;

        //    BarDataSeries? inputSeries = smaVM.Data;
        //    if (inputSeries == null) return;

        //    TimeDataSeries outputSeries = smaVM.ChartPlots[0].PlotSeries;

        //    BarPriceValue priceType = smaVM.ChartPlots[0].PriceField;

        //    outputSeries.Clear();
        //    Queue<double> values = new Queue<double>();
        //    for (int i = 0; i < inputSeries.Count; i++)
        //    {
        //        BarPricePoint outputPoint = new BarPricePoint(inputSeries.GetValueAt(i), priceType);
        //        values.Enqueue(outputPoint.Y);

        //        if (i + 1 < smaVM.Period)
        //        {
        //            outputSeries.Add(new TimeDataPoint(outputPoint.X, double.NaN));
        //        }
        //        else
        //        {
        //            if (values.Count > smaVM.Period) values.Dequeue();
        //            double avg = values.Sum() / smaVM.Period;
        //            outputSeries.Add(new TimeDataPoint(outputPoint.X, avg));
        //        }
        //    }
        //}

        //private void calculateFromIndicator()
        //{
        //    SMAViewModel? smaVM = Properties as SMAViewModel;
        //    if (smaVM == null || smaVM.Period < 1) return;

        //    IndicatorViewModel? sourceIndicatorVM = smaVM.SourceIndicator;
        //    if (sourceIndicatorVM == null) return;

        //    if (smaVM.SourcePlotIndex >= sourceIndicatorVM.ChartPlots.Count || smaVM.SourcePlotIndex < 0)
        //        return;

        //    TimeDataSeries inputSeries = sourceIndicatorVM.ChartPlots[smaVM.SourcePlotIndex].PlotSeries;
        //    TimeDataSeries outputSeries = smaVM.ChartPlots[0].PlotSeries;

        //    outputSeries.Clear();
        //    Queue<double> values = new Queue<double>();
        //    for (int i = 0; i < inputSeries.Count; i++)
        //    {
        //        TimeDataPoint inputPoint = inputSeries.GetValueAt(i);

        //        if (i + 1 < smaVM.Period)
        //        {
        //            outputSeries.Add(new TimeDataPoint(inputPoint.Time, double.NaN));
        //            values.Enqueue(inputPoint.Value);
        //        }
        //        else
        //        {
        //            values.Enqueue(inputPoint.Value);
        //            if(values.Count > smaVM.Period) values.Dequeue();
        //            double avg = values.Sum() / smaVM.Period;
        //            outputSeries.Add(new TimeDataPoint(inputPoint.Time, avg));
        //        }
        //    }
        //}


    }
}
