using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore
{
    public enum CrosshairSnapMode
    {
        Free,
        NearestBarPrice
    }

    public sealed class Globals
    {
        private static readonly Globals _instance = new Globals();
        public static Globals Instance { get { return _instance; } }
        static Globals()
        {
        }

        private Globals()
        {
        }

        internal void Load()
        {
        }

        SessionHoursCollection? _sessionHoursCollection;
        InstrumentCollection? _instrumentCollection;
        InstrumentDataInfoCollection? _instrumentDatainfoCollection;

        public SessionHoursCollection? SessionHoursCollection { get { return _sessionHoursCollection; } }

        public InstrumentCollection? InstrumentCollection { get { return _instrumentCollection; } }

        public InstrumentDataInfoCollection DataInfoCollection { get { return _instrumentDatainfoCollection; } }
    }
}
