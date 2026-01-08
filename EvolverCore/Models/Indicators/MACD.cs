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
    internal class MACD : Indicator
    {
        public MACD(IndicatorProperties properties) : base(properties) { }

        public void ConfigurePlots()
        {
            PlotProperties plotProperties = new PlotProperties();
            Outputs.Add(new OutputPlot("MACD",plotProperties, PlotStyle.Line));
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
        //    MACDViewModel? macdVM = Properties as MACDViewModel;
        //    if (macdVM == null || macdVM.Fast < 1 || macdVM.Slow < 1 || macdVM.Smoothing < 1) return;

        //    BarDataSeries? inputSeries = macdVM.Data;
        //    if (inputSeries == null) return;

        //    TimeDataSeries fastSeries = macdVM.ChartPlots[0].PlotSeries;

        //    BarPriceValue priceType = macdVM.ChartPlots[0].PriceField;

        //    fastSeries.Clear();
        //    Queue<double> fastValues = new Queue<double>();
        //    Queue<double> slowValues = new Queue<double>();
        //    double smoothScalar = macdVM.Smoothing / (1 + macdVM.Fast);

        //    for (int i = 0; i < inputSeries.Count; i++)
        //    {
        //        BarPricePoint pricePoint = new BarPricePoint(inputSeries.GetValueAt(i), priceType);

        //        if (i + 1 < macdVM.Fast)
        //        {
        //            fastValues.Enqueue(pricePoint.Y);
        //            fastSeries.Add(new TimeDataPoint(pricePoint.X, double.NaN));
        //        }
        //        else if (i + 1 == macdVM.Fast)
        //        {
        //            fastValues.Enqueue(pricePoint.Y);
        //            if (fastValues.Count > macdVM.Fast) fastValues.Dequeue();
        //            double avg = fastValues.Sum() / macdVM.Fast;
        //            fastSeries.Add(new TimeDataPoint(pricePoint.X, avg));
        //        }
        //        else
        //        {
        //            TimeDataPoint dataPointYesterday = fastSeries.GetValueAt(i - 1);
        //            double emaToday = pricePoint.Y * smoothScalar + dataPointYesterday.Y * (1 - smoothScalar);
        //            fastSeries.Add(new TimeDataPoint(dataPointYesterday.X, emaToday));
        //        }
        //    }
        //}

        //private void calculateFromIndicator()
        //{
        //    MACDViewModel? macdVM = Properties as MACDViewModel;
        //    if (macdVM == null || macdVM.Fast < 1 || macdVM.Slow < 1 || macdVM.Smoothing < 1) return;

        //    IndicatorViewModel? sourceIndicatorVM = macdVM.SourceIndicator;
        //    if (sourceIndicatorVM == null) return;

        //    if (macdVM.SourcePlotIndex >= sourceIndicatorVM.ChartPlots.Count || macdVM.SourcePlotIndex < 0)
        //        return;

        //    TimeDataSeries inputSeries = sourceIndicatorVM.ChartPlots[macdVM.SourcePlotIndex].PlotSeries;
        //    TimeDataSeries fastSeries = macdVM.ChartPlots[0].PlotSeries;

        //    fastSeries.Clear();
        //    Queue<double> fastValues = new Queue<double>();
        //    Queue<double> slowValues = new Queue<double>();
        //    double smoothScalar = macdVM.Smoothing / (1 + macdVM.Fast);

        //    for (int i = 0; i < inputSeries.Count; i++)
        //    {
        //        TimeDataPoint inputPoint = inputSeries.GetValueAt(i);

        //        if (i + 1 < macdVM.Fast)
        //        {
        //            fastValues.Enqueue(inputPoint.Y);
        //            fastSeries.Add(new TimeDataPoint(inputPoint.X, double.NaN));
        //        }
        //        else if (i + 1 == macdVM.Fast)
        //        {
        //            fastValues.Enqueue(inputPoint.Y);
        //            if (fastValues.Count > macdVM.Fast) fastValues.Dequeue();
        //            double avg = fastValues.Sum() / macdVM.Fast;
        //            fastSeries.Add(new TimeDataPoint(inputPoint.X, avg));
        //        }
        //        else
        //        {
        //            TimeDataPoint dataPointYesterday = fastSeries.GetValueAt(i - 1);
        //            double emaToday = inputPoint.Y * smoothScalar + dataPointYesterday.Y * (1 - smoothScalar);
        //            fastSeries.Add(new TimeDataPoint(dataPointYesterday.X, emaToday));
        //        }
        //    }
        //}
    }
}
