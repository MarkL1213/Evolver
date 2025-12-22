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
            _sessionHoursCollection = new SessionHoursCollection();
            _instrumentCollection = new InstrumentCollection();
            _dataManager = new DataManager();
        }

        internal void Load()
        {
            //_sessionHoursCollection.Load();

            //_instrumentCollection.Load();

            //_dataManager.Load();
        }


        SessionHoursCollection? _sessionHoursCollection;
        InstrumentCollection? _instrumentCollection;
        DataManager? _dataManager;

        public SessionHoursCollection? SessionHoursCollection { get { return _sessionHoursCollection; } }

        public InstrumentCollection? InstrumentCollection { get { return _instrumentCollection; } }

        internal DataManager? DataManager { get { return _dataManager; } }
    }
}
