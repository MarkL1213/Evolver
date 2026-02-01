//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace EvolverCore.Models
//{
//    public class DataAccumulator
//    {
//        public DataAccumulator(DataTable parentTable)
//        {
//            ParentTable = parentTable;
//        }

//        public DataTable ParentTable { get; private set; }

//        public void StartBar(DateTime barTime)
//        {
//            ParentTable.StartNewRow(barTime);
//        }

//        public bool AddTick(DateTime time, double bid, double ask, long volume)
//        {
//            DateTime tickBarTime = ParentTable.Interval.GetBarTime(time);
//            bool fireComplete = false;
//            DateTime newBarTime = DateTime.MinValue;
//            lock (_lock)
//            {
//                if (FormingBar == null) return false;

//                if (FormingBar.Time != tickBarTime)
//                {
//                    fireComplete = true;
//                    newBarTime = ParentTable.Interval.Add(FormingBar.Time, 1);
//                }
//            }

//            if (fireComplete)
//            {
//                fireBarClose();
//                StartBar(newBarTime);
//            }

//            lock (_lock)
//            {
//                //TODO add values to bar
//            }

//            if (fireComplete)
//            {
//                //TODO fire BarOpen
//            }

//            //TODO fire TickDataEvent

//            return true;

//        }

//        public bool AddBar(TimeDataBar addBar, DataInterval addInterval)
//        {
//            lock (_lock)
//            {
//                if (FormingBar == null) return false;
//                if (!ParentTable.Interval.IsFactor(addInterval)) return false;
//                if (addBar.Time < _startTime) return false;

//                if (addBar.Time > FormingBar.Time)
//                {
//                    fireBarClose();
//                    StartBar(ParentTable.Interval.Add(FormingBar.Time,1));
//                }

//                FormingBar.Volume += addBar.Volume;
//                FormingBar.Close = addBar.Close;

//                if (FormingBar.High == 0 || addBar.High > FormingBar.High) FormingBar.High = addBar.High;
//                if (FormingBar.Low == 0 || addBar.Low < FormingBar.Low) FormingBar.Low = addBar.Low;
//                if (FormingBar.Open == 0) FormingBar.Open = addBar.Open;

//                if (FormingBar.Bid == 0 || addBar.Bid > FormingBar.Bid) FormingBar.Bid = addBar.Bid;
//                if (FormingBar.Ask == 0 || addBar.Ask < FormingBar.Ask) FormingBar.Ask = addBar.Ask;

//                return true;
//            }
//        }
//    }
//}
