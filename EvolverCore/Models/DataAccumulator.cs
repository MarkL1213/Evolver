using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Models
{
    public class DataAccumulator
    {
        public DataAccumulator(DataInterval interval)
        {
            Interval = interval;
            FormingBar = null;
        }

        private DateTime _endTime;
        private object _lock = new object();

        public DataInterval Interval { get; private set; }
        public TimeDataBar? FormingBar { get; private set; }

        public event EventHandler<TimeDataBar>? BarComplete;

        public void StartBar(DateTime startTime)
        {
            lock (_lock)
            {
                FormingBar = new TimeDataBar(startTime, 0, 0, 0, 0, 0, 0, 0);
                _endTime = Interval.Add(FormingBar.Time, 1);
            }
        }

        public bool AddTick()
        {
            lock (_lock)
            {
                if (FormingBar == null) return false;

                //TODO implement tick add

                return false;
            }
        }

        public bool AddBar(TimeDataBar addBar, DataInterval addInterval)
        {
            lock (_lock)
            {
                if (FormingBar == null) return false;
                if (!Interval.IsFactor(addInterval)) return false;
                if (addBar.Time > _endTime)
                {
                    fireBarComplete();
                    StartBar(_endTime);
                }

                FormingBar.Volume += addBar.Volume;
                FormingBar.Close = addBar.Close;

                if (FormingBar.High == 0 || addBar.High > FormingBar.High) FormingBar.High = addBar.High;
                if (FormingBar.Low == 0 || addBar.Low < FormingBar.Low) FormingBar.Low = addBar.Low;
                if (FormingBar.Open == 0) FormingBar.Open = addBar.Open;

                if (FormingBar.Bid == 0 || addBar.Bid > FormingBar.Bid) FormingBar.Bid = addBar.Bid;
                if (FormingBar.Ask == 0 || addBar.Ask < FormingBar.Ask) FormingBar.Ask = addBar.Ask;

                return true;
            }
        }

        private void fireBarComplete()
        {
            TimeDataBar completedBar;
            lock (_lock)
            {
                if (FormingBar == null) return;
                completedBar = FormingBar;
                FormingBar = null;
            }

            BarComplete?.Invoke(this, completedBar);
        }
    }
}
