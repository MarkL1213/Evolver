using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Models
{
    public enum IntervalSpan
    {
        Tick,
        Second,
        Minute,
        Hour,
        Day,
        Week,
        Month,
        Year
    }

    public struct DataInterval
    {
        public IntervalSpan Type;
        public int Value;

        public DataInterval(IntervalSpan type, int value) { Type = type; Value = value; }

        public TimeSpan GetTimeSpan()
        {
            switch (Type)
            {
                case IntervalSpan.Second: return new TimeSpan(0, 0, Value);
                case IntervalSpan.Minute: return new TimeSpan(0, Value, 0);
                case IntervalSpan.Hour: return new TimeSpan(Value, 0, 0);
                case IntervalSpan.Day: return new TimeSpan(Value, 0, 0, 0);
                case IntervalSpan.Week: return new TimeSpan(Value * 7, 0, 0, 0);
                default:
                    throw new EvolverException($"GetTimeSpan not supported for interval type {Type}");
            }
        }

        public DateTime Add(DateTime dateTime, int n)
        {
            switch (Type)
            {
                case IntervalSpan.Second: return dateTime.AddSeconds(Value * n);
                case IntervalSpan.Minute: return dateTime.AddMinutes(Value * n);
                case IntervalSpan.Hour: return dateTime.AddHours(Value * n);
                case IntervalSpan.Day: return dateTime.AddDays(Value * n);
                case IntervalSpan.Week: return dateTime.AddDays(Value * n * 7);
                case IntervalSpan.Month: return AddToLastDayOfMonth(dateTime, Value * n);
                case IntervalSpan.Year: return AddToLastDayOfYear(dateTime, Value * n);
                default:
                    throw new EvolverException($"Unknown interval type in interval.Add() : type={Type}");
            }
        }

        private static DateTime AddToLastDayOfMonth(DateTime dateTime, int months)
        {
            // Best practice: Go to first of the month → add months → go to last day
            DateTime firstOfMonth = new DateTime(dateTime.Year, dateTime.Month, 1);
            DateTime targetFirstOfMonth = firstOfMonth.AddMonths(months);

            int daysInTargetMonth = DateTime.DaysInMonth(targetFirstOfMonth.Year, targetFirstOfMonth.Month);

            return new DateTime(
                targetFirstOfMonth.Year,
                targetFirstOfMonth.Month,
                daysInTargetMonth,
                dateTime.Hour,
                dateTime.Minute,
                dateTime.Second,
                dateTime.Millisecond,
                dateTime.Kind);
        }

        private static DateTime AddToLastDayOfYear(DateTime dateTime, int years)
        {
            DateTime targetYearStart = new DateTime(dateTime.Year + years, 1, 1);
            return new DateTime(targetYearStart.Year, 12, 31,
                                dateTime.Hour, dateTime.Minute, dateTime.Second,
                                dateTime.Millisecond, dateTime.Kind);
        }

        public bool IsFactor(DataInterval subInterval)
        {//is the subInterval and factor of this?

            if (subInterval.Type > Type) return false;

            double thisSpan = GetTimeSpan().TotalSeconds;
            double subSpan = subInterval.GetTimeSpan().TotalSeconds;

            if ((thisSpan % subSpan) == 0) return true;

            return false;
        }

        public long Ticks
        {
            get
            {
                DateTime now = DateTime.Now;
                DateTime then = Add(now, 1);
                return (then - now).Ticks;
            }
        }

        public bool IsTimeBased
        {
            get
            {
                switch (Type)
                {
                    case IntervalSpan.Tick: return false;
                    default: return true;
                }
            }
        }


        public DateTime GetBarTime(DateTime time)
        {
            if (!IsTimeBased) return time;
            return RoundUp(time);
        }


        #region rounding

        private long GetFixedTickCount()
        {
            return Type switch
            {
                IntervalSpan.Second => (long)Value * TimeSpan.TicksPerSecond,
                IntervalSpan.Minute => (long)Value * TimeSpan.TicksPerMinute,
                IntervalSpan.Hour => (long)Value * TimeSpan.TicksPerHour,
                IntervalSpan.Day => (long)Value * TimeSpan.TicksPerDay,
                IntervalSpan.Week => (long)Value * 7 * TimeSpan.TicksPerDay,
                _ => throw new NotSupportedException($"Interval {Type} has no fixed tick length")
            };
        }


        // --------------------------------------------------------------------
        // Round UP (ceiling) to the interval boundary
        // --------------------------------------------------------------------
        public DateTime RoundUp(DateTime dateTime)
        {
            DateTime down = RoundDown(dateTime);

            // If already exactly on a boundary → return it, otherwise go to next boundary
            if (down == dateTime)
                return dateTime;

            return Add(down, 1);
        }

        // --------------------------------------------------------------------
        // Round DOWN (floor) to the interval boundary
        // --------------------------------------------------------------------
        public DateTime RoundDown(DateTime dateTime)
        {
            return Type switch
            {
                // Global alignment to year 1 (consistent grid for any Value)
                IntervalSpan.Month => RoundDownMonth(dateTime),
                IntervalSpan.Year => RoundDownYear(dateTime),
                _ => // All fixed-length intervals (Tick → Week)
                    RoundDownFixed(dateTime)
            };
        }

        private DateTime RoundDownFixed(DateTime dateTime)
        {
            long intervalTicks = GetFixedTickCount();
            if (intervalTicks <= 0) throw new InvalidOperationException("Interval value must be > 0");

            long remainder = dateTime.Ticks % intervalTicks;
            long flooredTicks = dateTime.Ticks - remainder;
            return new DateTime(flooredTicks, dateTime.Kind);
        }

        private DateTime RoundDownMonth(DateTime dateTime)
        {
            int totalMonths = (dateTime.Year - 1) * 12 + dateTime.Month - 1;
            int flooredMonths = (totalMonths / Value) * Value;

            int targetYear = 1 + flooredMonths / 12;
            int targetMonth = (flooredMonths % 12) + 1;

            DateTime start = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, dateTime.Kind);
            return start.AddDays(-1);
        }

        private DateTime RoundDownYear(DateTime dateTime)
        {
            int totalYears = dateTime.Year - 1;
            int flooredYears = (totalYears / Value) * Value;
            int targetYear = 1 + flooredYears;

            DateTime start = new DateTime(targetYear, 1, 1, 0, 0, 0, dateTime.Kind);
            return start.AddDays(-1);
        }
        #endregion



        public static int operator /(TimeSpan span, DataInterval interval)
        {
            double n = span / interval.GetTimeSpan();

            return (int)Math.Ceiling(n);
        }

        public static bool operator !=(DataInterval a, DataInterval b)
        {
            return !(a == b);
        }

        public static bool operator ==(DataInterval a, DataInterval b)
        {
            return (a.Type == b.Type && a.Value == b.Value);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is DataInterval)) return false;
            DataInterval b = (DataInterval)obj;
            return this == b;
        }

        public override int GetHashCode()
        {
            string s = Type.ToString() + Value.ToString();
            return s.GetHashCode();
        }

        public override string ToString()
        {
            return $"{Value}{Type.ToString()}";
        }

        public static DataInterval? TryParseString(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            string input = s.Trim();
            int digitEnd = 0;
            while (digitEnd < input.Length && char.IsDigit(input[digitEnd]))
                digitEnd++;

            int value = 1; // default when no number
            if (digitEnd > 0)
            {
                if (!int.TryParse(input.Substring(0, digitEnd), out value) || value <= 0)
                    return null;
            }

            string typePart = input.Substring(digitEnd);
            if (!Enum.TryParse<IntervalSpan>(typePart, ignoreCase: true, out IntervalSpan interval))
                return null;

            return new DataInterval(interval, value);
        }
    }
}
