using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvolverCore.Models;

namespace EvolverCore.Tests
{
    internal static class DataIntervalTests
    {
        public static bool RunAll()
        {
            if(!RoundingTests()) return false;
            if(!AddTests()) return false;

            return true;
        }

        public static bool RoundingTests()
        {
            int fail = 0;
            #region month interval
            DataInterval monthInterval = new DataInterval(IntervalSpan.Month, 1);
            DateTime roundTest = monthInterval.RoundUp(new DateTime(2024, 1, 1));
            if (roundTest.Month != 1 || roundTest.Day != 31 || roundTest.Year != 2024)
                fail++;

            roundTest = monthInterval.RoundUp(new DateTime(2024, 2, 1));
            if (roundTest.Month != 2 || roundTest.Day != 29 || roundTest.Year != 2024)
                fail++;

            roundTest = monthInterval.RoundUp(new DateTime(2025, 2, 1));
            if (roundTest.Month != 2 || roundTest.Day != 28 || roundTest.Year != 2025)
                fail++;

            roundTest = monthInterval.RoundDown(new DateTime(2024, 1, 1));
            if (roundTest.Month != 12 || roundTest.Day != 31 || roundTest.Year != 2023)
                fail++;

            roundTest = monthInterval.RoundDown(new DateTime(2024, 3, 1));
            if (roundTest.Month != 2 || roundTest.Day != 29 || roundTest.Year != 2024)
                fail++;

            roundTest = monthInterval.RoundDown(new DateTime(2025, 3, 1));
            if (roundTest.Month != 2 || roundTest.Day != 28 || roundTest.Year != 2025)
                fail++;
            #endregion

            #region hour interval
            DataInterval hourInterval = new DataInterval(IntervalSpan.Hour, 1);
            
            roundTest = hourInterval.RoundUp(new DateTime(2024, 1, 1, 1, 15, 0));
            if (roundTest.Month != 1 || roundTest.Day != 1 || roundTest.Year != 2024 ||
                roundTest.Hour != 2 || roundTest.Minute != 0 || roundTest.Second != 0)
                fail++;

            roundTest = hourInterval.RoundDown(new DateTime(2024, 1, 1, 1, 15, 0));
            if (roundTest.Month != 1 || roundTest.Day != 1 || roundTest.Year != 2024 ||
                roundTest.Hour != 1 || roundTest.Minute != 0 || roundTest.Second != 0)
                fail++;

            roundTest = hourInterval.RoundUp(new DateTime(2024, 1, 1, 1, 0, 0));
            if (roundTest.Month != 1 || roundTest.Day != 1 || roundTest.Year != 2024 ||
                roundTest.Hour != 1 || roundTest.Minute != 0 || roundTest.Second != 0)
                fail++;

            roundTest = hourInterval.RoundDown(new DateTime(2024, 1, 1, 1, 0, 0));
            if (roundTest.Month != 1 || roundTest.Day != 1 || roundTest.Year != 2024 ||
                roundTest.Hour != 1 || roundTest.Minute != 0 || roundTest.Second != 0)
                fail++;
            #endregion


            return fail == 0;
        }

        public static bool AddTests()
        {
            #region month interval
            DataInterval monthInterval = new DataInterval(IntervalSpan.Month, 1);

            #region leap year
            DateTime testTime = new DateTime(2024, 1, 31);
            DateTime resTime = monthInterval.Add(testTime, 1);
            if (resTime.Month != 2 || resTime.Day != 29 || resTime.Year != 2024)
                return false;

            DateTime res2Time = monthInterval.Add(resTime, 1);
            if (res2Time.Month != 3 || res2Time.Day != 31 || res2Time.Year != 2024)
                return false;

            DateTime res3Time = monthInterval.Add(testTime, 2);
            if (res2Time.Month != res3Time.Month || res2Time.Day != res3Time.Day || res2Time.Year != res3Time.Year)
                return false;
            #endregion

            #region non-leap year
            testTime = new DateTime(2025, 1, 31);
            resTime = monthInterval.Add(testTime, 1);
            if (resTime.Month != 2 || resTime.Day != 28 || resTime.Year != 2025)
                return false;

            res2Time = monthInterval.Add(resTime, 1);
            if (res2Time.Month != 3 || res2Time.Day != 31 || res2Time.Year != 2025)
                return false;

            res3Time = monthInterval.Add(testTime, 2);
            if (res2Time.Month != res3Time.Month || res2Time.Day != res3Time.Day || res2Time.Year != res3Time.Year)
                return false;
            #endregion
            #endregion


            return true;
        }
    }
}
