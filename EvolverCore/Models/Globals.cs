using EvolverCore.Models;
using NP.Ava.UniDock;
using NP.Ava.UniDock.Factories;
using NP.DependencyInjection.Interfaces;
using NP.IoCy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace EvolverCore
{
    public enum CrosshairSnapMode
    {
        Free,
        NearestBarPrice
    }

    [Serializable]
    public class EvolverProperties
    {
        public string LastUsedLayout = string.Empty;
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
            _connections = new ConnectionCollection();
            _log = new Log();
        }

        internal void Load()
        {
            LoadProperties();

            //_sessionHoursCollection.Load();

            _instrumentCollection.LoadRandomInstrument();
            _dataManager.LoadRandomInstrumentRecords();
        }


        ConnectionCollection _connections;
        SessionHoursCollection _sessionHoursCollection;
        InstrumentCollection _instrumentCollection;
        DataManager _dataManager;
        Log _log;

        public string LogFileName { get; } = "D:\\Evolver\\Evolver.log";
        public string PropertiesFileName { get; } = "D:\\Evolver\\EvolverProperties.xml";
        public string LayoutDirectory { get; } = "D:\\Evolver\\Layouts";

        public string DataDirectory { get; } = "D:\\Evolver\\MarketData";


        public EvolverProperties Properties = new EvolverProperties();

        public ConnectionCollection Connections { get { return _connections; } }

        public SessionHoursCollection SessionHoursCollection { get { return _sessionHoursCollection; } }

        public InstrumentCollection InstrumentCollection { get { return _instrumentCollection; } }

        internal DataManager DataManager { get { return _dataManager; } }

        internal Log Log { get { return _log; } }

        public void SaveProperties()
        {//serialize the EvolverProperties
            if (!File.Exists(PropertiesFileName)) File.Create(PropertiesFileName);

            using (FileStream fs = new FileStream(PropertiesFileName, FileMode.Truncate))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(EvolverProperties));
                serializer.Serialize(fs, Properties);
            }
        }

        public void LoadProperties()
        {//deserialize the EvolverProperties
            if (!File.Exists(PropertiesFileName)) return;

            using (FileStream fs = File.OpenRead(PropertiesFileName))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(EvolverProperties));
                EvolverProperties? props = serializer.Deserialize(fs) as EvolverProperties;
                Properties = props != null ? props : new EvolverProperties();
            }
        }
    }

    public static class MyContainer
    {
        public static IDependencyInjectionContainer<object?> TheContainer { get; }

        public static DockManager TheDockManager { get; } = new DockManager();

        static MyContainer()
        {
            var containerBuilder = new ContainerBuilder();

            containerBuilder.RegisterSingletonType<IFloatingWindowFactory, MyCustomFloatingWindowFactory>();
            containerBuilder.RegisterSingletonInstance<DockManager>(TheDockManager);
            //TheContainer.MapSingleton<IUniDockService, DockManager>(TheDockManager, null, true);

            TheContainer = containerBuilder.Build();
        }
    }

    public class MyCustomFloatingWindowFactory : IFloatingWindowFactory
    {
        public virtual FloatingWindow CreateFloatingWindow()
        {
            // create the window

            FloatingWindow dockWindow = new FloatingWindow();

            dockWindow.Classes.Add("PlainFloatingWindow");
            dockWindow.Classes.Add("MyFloatingWindow");

            dockWindow.TitleClasses = "WindowTitle";

            return dockWindow;
        }
    }
}
