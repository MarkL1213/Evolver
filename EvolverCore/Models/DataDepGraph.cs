using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Models
{
    internal class DataDepGraph
    {
        internal DataDepGraph() { }

        private Dictionary<string, Dictionary<DataInterval, List<DepGraphNode>>> _rootNodes = new Dictionary<string, Dictionary<DataInterval, List<DepGraphNode>>>();

        public Indicator? CreateIndicator(Type indicatorType, IndicatorProperties properties, Indicator source, CalculationSource sourceType, int sourcePlotIndex = -1)
        {
            if (source.SourceRecord == null)
            {
                Globals.Instance.Log.LogMessage("CreateIndicator failed: source indicator has no record.", LogLevel.Error);
                return null;
            }

            if (indicatorType.BaseType != typeof(Indicator))
            {
                Globals.Instance.Log.LogMessage("CreateIndicator failed: type is not an indicator.", LogLevel.Error);
                return null;
            }

            ConstructorInfo? iConstructor = indicatorType.GetConstructor(new Type[] { typeof(IndicatorProperties) });
            if (iConstructor == null)
            {
                Globals.Instance.Log.LogMessage("CreateIndicator failed: failed to locate constructor.", LogLevel.Error);
                return null;
            }

            Indicator? newIndicator = iConstructor.Invoke(new object[] { properties }) as Indicator;
            if (newIndicator == null)
            {
                Globals.Instance.Log.LogMessage("CreateIndicator failed: constructor failed", LogLevel.Error);
                return null;
            }

            IndicatorDataSourceRecord newSourceRecord = new IndicatorDataSourceRecord();
            newSourceRecord.SourceBarData = source.SourceRecord.SourceBarData;
            newSourceRecord.SourceIndicator = source;
            newSourceRecord.SourcePlotIndex = sourcePlotIndex;
            newSourceRecord.SourceType = sourceType;
            newSourceRecord.StartDate = source.SourceRecord.StartDate;
            newSourceRecord.EndDate = source.SourceRecord.EndDate;

            newIndicator.SetSourceData(newSourceRecord);
            newIndicator.Startup();
            
            AddIndicatorNode(newIndicator);

            if (newIndicator.WaitingForDataLoad)
                source.DataChanged += newIndicator.OnSourceDataLoaded;
            else
            { _indicatorsReadyToRun.Add(newIndicator); }

            return newIndicator;
        }

        public Indicator CreateDataIndicator(Instrument instrument, DataInterval interval, DateTime start)
        {//create live data indicator
            return CreateDataIndicator(instrument, interval, start, DateTime.Now, true);
        }

        public Indicator CreateDataIndicator(Instrument instrument, DataInterval interval, DateTime start, DateTime end, bool isLive=false)
        {//create fixed-window data indicator
            //InstrumentDataSliceRecord sliceRecord = new InstrumentDataSliceRecord()
            //{
            //    Instrument = instrument,
            //    Interval = interval,
            //    StartDate = start,
            //    EndDate = end
            //};

            BarTablePointer tablePointer = Globals.Instance.DataTableManager.DataWarehouse.CreateTablePointer(instrument, interval, start, end, isLive);

            IndicatorDataSourceRecord iSliceRecord = new IndicatorDataSourceRecord()
            {
                SourceBarData = tablePointer,
                SourceType = CalculationSource.BarData,
                StartDate = start,
                EndDate = end
            };

            Indicator indicator = new Indicator(new IndicatorProperties());
            indicator.Name = instrument.Name;
            indicator.IsDataOnly = true;
            indicator.SetSourceData(iSliceRecord);
            if (indicator.WaitingForDataLoad)
                tablePointer.LoadStateChange += indicator.OnSourceDataLoaded;

            DepGraphNode rootNode = new DepGraphNode();
            rootNode.Indicators.Add(indicator);
            AddRootNode(rootNode);


            indicator.Startup();

            if (!indicator.WaitingForDataLoad)
            { _indicatorsReadyToRun.Add(indicator); }

            return indicator;
        }

        private void AddRootNode(DepGraphNode node)
        {
            BarTablePointer btp = node.Indicators[0].Bars[0];

            string instrumentName = btp.Instrument.Name;
            if (!_rootNodes.ContainsKey(instrumentName))
                _rootNodes.Add(instrumentName, new Dictionary<DataInterval, List<DepGraphNode>>());

            if (!_rootNodes[instrumentName].ContainsKey(btp.Interval))
                _rootNodes[instrumentName].Add(btp.Interval, new List<DepGraphNode>());

            _rootNodes[instrumentName][btp.Interval].Add(node);
        }

        private void AddIndicatorNode(Indicator indicator)
        {
            DepGraphNode? indicatorNode = FindNodeByDependency(indicator);
            if (indicatorNode != null)
            {
                indicatorNode.Indicators.Add(indicator);
                return;
            }


            indicatorNode = new DepGraphNode();
            indicatorNode.Indicators.Add(indicator);


            //FIXME : link node into dep graph, adding any parent deps not already preset
        }

        DepGraphNode? FindNodeByDependency(Indicator indicator)
        {
            //FIXME : find a dep graph node whose dependency tree matches the indicator
            return null;
        }

        //FIXME : need thread/pool for 1) executing history tasks
        ////      and 2) processing incoming data into live indicators
        ////
        ////      when incoming data arrives deliver in parallel as much as possible
        ////      based on dependency inter-relations and reasonable max thread count


        private BlockingCollection<Indicator> _indicatorsReadyToRun = new BlockingCollection<Indicator>();

        internal void EnqueueIndicatorReadyToRun(Indicator indicator)
        {
            _indicatorsReadyToRun.Add(indicator);
        }

        private async Task ExecuteHistory(Indicator indicator)
        {
            await Task.Run(()=>indicator.RunHistory());
        }
    }


    internal class DepGraphNode
    {
        internal DepGraphNode() { }

        internal List<DepGraphNode> Parents { get; init; } = new List<DepGraphNode>();
        internal List<DepGraphNode> Children { get; init; } = new List<DepGraphNode>();

        internal List<Indicator> Indicators { get; init; } = new List<Indicator>();
    }
}
