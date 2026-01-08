using EvolverCore.ViewModels;
using EvolverCore.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Models.Indicators
{
    internal class EMA : Indicator
    {
        public EMA(IndicatorProperties properties) : base(properties) { }

        public void ConfigurePlots()
        {
            PlotProperties plotProperties = new PlotProperties();
            plotProperties.Name = "EMA";

            Outputs.Add(new OutputPlot("EMA", plotProperties, PlotStyle.Line));
        }

        public override void OnDataUpdate()
        {
            //if (Properties.Source == CalculationSource.BarData)
            //    calculateFromBarData();
            //else if (Properties.Source == CalculationSource.IndicatorPlot)
            //    calculateFromIndicator();
        }

        //
        //EMAt = (Vt * smoothScalar) + EMAy*(1-smoothScalar)

        //private void calculateFromBarData()
        //{
        //    EMAViewModel? emaVM = Properties as EMAViewModel;
        //    if (emaVM == null || emaVM.Period < 1 || emaVM.Smoothing < 1) return;

        //    BarDataSeries? inputSeries = emaVM.Data;
        //    if (inputSeries == null) return;

        //    TimeDataSeries outputSeries = emaVM.ChartPlots[0].PlotSeries;

        //    BarPriceValue priceType = emaVM.ChartPlots[0].PriceField;

        //    outputSeries.Clear();
        //    Queue<double> values = new Queue<double>();
        //    double smoothScalar = emaVM.Smoothing / (1 + emaVM.Period);

        //    for (int i = 0; i < inputSeries.Count; i++)
        //    {
        //        BarPricePoint pricePoint = new BarPricePoint(inputSeries.GetValueAt(i), priceType);

        //        if (i + 1 < emaVM.Period)
        //        {
        //            values.Enqueue(pricePoint.Y);
        //            outputSeries.Add(new TimeDataPoint(pricePoint.X, double.NaN));
        //        }
        //        else if (i + 1 == emaVM.Period)
        //        {
        //            values.Enqueue(pricePoint.Y);
        //            if (values.Count > emaVM.Period) values.Dequeue();
        //            double avg = values.Sum() / emaVM.Period;
        //            outputSeries.Add(new TimeDataPoint(pricePoint.X, avg));
        //        }
        //        else
        //        {
        //            TimeDataPoint dataPointYesterday = outputSeries.GetValueAt(i - 1);
        //            double emaToday = pricePoint.Y * smoothScalar + dataPointYesterday.Y * (1 - smoothScalar);
        //            outputSeries.Add(new TimeDataPoint(dataPointYesterday.X, emaToday));
        //        }
        //    }
        //}

        //private void calculateFromIndicator()
        //{
        //    EMAViewModel? emaVM = Properties as EMAViewModel;
        //    if (emaVM == null || emaVM.Period < 1 || emaVM.Smoothing < 1) return;

        //    IndicatorViewModel? sourceIndicatorVM = emaVM.SourceIndicator;
        //    if (sourceIndicatorVM == null) return;

        //    if (emaVM.SourcePlotIndex >= sourceIndicatorVM.ChartPlots.Count || emaVM.SourcePlotIndex < 0)
        //        return;

        //    TimeDataSeries inputSeries = sourceIndicatorVM.ChartPlots[emaVM.SourcePlotIndex].PlotSeries;
        //    TimeDataSeries outputSeries = emaVM.ChartPlots[0].PlotSeries;

        //    outputSeries.Clear();
        //    Queue<double> values = new Queue<double>();
        //    double smoothScalar = emaVM.Smoothing / (1 + emaVM.Period);

        //    for (int i = 0; i < inputSeries.Count; i++)
        //    {
        //        TimeDataPoint inputPoint = inputSeries.GetValueAt(i);

        //        if (i + 1 < emaVM.Period)
        //        {
        //            values.Enqueue(inputPoint.Y);
        //            outputSeries.Add(new TimeDataPoint(inputPoint.X, double.NaN));
        //        }
        //        else if (i + 1 == emaVM.Period)
        //        {
        //            values.Enqueue(inputPoint.Y);
        //            if (values.Count > emaVM.Period) values.Dequeue();
        //            double avg = values.Sum() / emaVM.Period;
        //            outputSeries.Add(new TimeDataPoint(inputPoint.X, avg));
        //        }
        //        else
        //        {
        //            TimeDataPoint dataPointYesterday = outputSeries.GetValueAt(i - 1);
        //            double emaToday = inputPoint.Y * smoothScalar + dataPointYesterday.Y * (1 - smoothScalar);
        //            outputSeries.Add(new TimeDataPoint(dataPointYesterday.X, emaToday));
        //        }
        //    }
        //}

    }
}
