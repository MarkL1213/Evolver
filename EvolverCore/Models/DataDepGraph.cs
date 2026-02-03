using System;
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

        private Dictionary<string, Dictionary<DataInterval, List<DepGraphRootNode>>> _rootNodes = new Dictionary<string, Dictionary<DataInterval, List<DepGraphRootNode>>>();

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
            //_indicatorCache.Add(newIndicator);

            if (newIndicator.WaitingForDataLoad)
                source.DataChanged += newIndicator.OnSourceDataLoaded;
            else
            { }//IndicatorReadyToRun(newIndicator);

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
            DepGraphRootNode rootNode = new DepGraphRootNode(tablePointer);
            AddRootNode(rootNode);

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

            indicator.Startup();

            if (!indicator.WaitingForDataLoad)
            { }//Globals.Instance.DataManager.IndicatorReadyToRun(indicator);

            return indicator;
        }

        private void AddRootNode(DepGraphRootNode node)
        {
            string instrumentName = node.TablePointer.Instrument.Name;
            if (!_rootNodes.ContainsKey(instrumentName))
                _rootNodes.Add(instrumentName, new Dictionary<DataInterval, List<DepGraphRootNode>>());

            if (!_rootNodes[instrumentName].ContainsKey(node.TablePointer.Interval))
                _rootNodes[instrumentName].Add(node.TablePointer.Interval, new List<DepGraphRootNode>());

            _rootNodes[instrumentName][node.TablePointer.Interval].Add(node);
        }
    }


    internal class DepGraphRootNode : DepGraphNode
    {
        internal DepGraphRootNode(BarTablePointer tablePointer) : base() { TablePointer = tablePointer; }
        internal BarTablePointer TablePointer { get; init; }
        
    }

    internal class DepGraphNode
    {
        internal DepGraphNode() { }

        internal List<DepGraphNode> Parents { get; init; } = new List<DepGraphNode>();
        internal List<DepGraphNode> Children { get; init; } = new List<DepGraphNode>();

        internal List<Indicator> Indicators { get; init; } = new List<Indicator>();
    }
}
