using NP.Ava.UniDock;
using NP.Ava.UniDock.Factories;
using NP.DependencyInjection.Interfaces;
using NP.IoCy;

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
