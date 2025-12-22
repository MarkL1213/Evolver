using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dock.Avalonia;
using Dock.Avalonia.Controls;
using Dock.Model;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Dock.Serializer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public string WindowTitle { get; } = "Evolver";
        public WindowIcon? WindowIcon { get; } = new WindowIcon("D:/Evolver/EvolverCore/Assets/avalonia-logo.ico");

        public IRootDock Layout { get; internal set; }

        private const string LastLayoutKey = "LastLayoutName";
        private const string LayoutsDir = "Layouts";  

        [ObservableProperty]
        private List<string> _availableLayouts = new();
        [ObservableProperty] string _currentLayoutName = string.Empty;


        //public IList<IDockable> Documents { get; set; } = new List<IDockable>();

        public MainWindowViewModel()
        {
            Directory.CreateDirectory(LayoutsDir);
            LoadAvailableLayouts();

            // Create factory (required)
            _factory ??= new Factory();
            if (_factory == null) return;

            if (_availableLayouts.Count > 0)
                LoadLastUsedLayout();
            else
                defaultLayout();
        }

        private Factory? _factory = null;
        private IDocumentDock? _documentDock;
        private void defaultLayout()
        {
            if (_factory == null) return;

            // Create root
            Layout = _factory.CreateRootDock();

            // Create main document
            var mainChart = new ChartControlViewModel();
            //mainChart.Context = mainChart;
            var tool1 = new Tool1ViewModel();
            var tool2 = new Tool1ViewModel()
            {
                Id = "Tool2",
                Title = "Tool Two"
            };


            var leftToolDock = _factory.CreateToolDock();
            leftToolDock.VisibleDockables = _factory.CreateList<IDockable>(tool1);
            leftToolDock.Proportion = 0.15;
            leftToolDock.Alignment = Alignment.Left;

            _documentDock = _factory.CreateDocumentDock();
            
            _documentDock.VisibleDockables = _factory.CreateList<IDockable>(mainChart);

            _documentDock.CanCreateDocument = true;
            _documentDock.CanCloseLastDockable = true;
            _documentDock.CanClose = false;
            _documentDock.CreateDocument = NewChartDocumentCommand;
            

            //_factory.AddDockable(_documentDock, mainChart);
            _factory.SetActiveDockable(mainChart);
            _factory.SetFocusedDockable(_documentDock, mainChart);

            var rightToolDock = _factory.CreateToolDock();
            rightToolDock.VisibleDockables = _factory.CreateList<IDockable>(tool2);
            rightToolDock.Proportion = 0.15;
            rightToolDock.Alignment = Alignment.Right;

            // Main proportional layout
            IProportionalDock proportionalDock = _factory.CreateProportionalDock();
            proportionalDock.Orientation = Orientation.Horizontal;
            proportionalDock.VisibleDockables = _factory.CreateList<IDockable>(
                leftToolDock,
                _factory.CreateProportionalDockSplitter(),
                _documentDock,
                _factory.CreateProportionalDockSplitter(),
                rightToolDock
            );

            // Set root
            Layout.VisibleDockables = _factory.CreateList<IDockable>(proportionalDock);
            Layout.DefaultDockable = proportionalDock;
            Layout.ActiveDockable = proportionalDock;

            // Initialize factory with layout
            _factory.InitLayout(Layout);
        }

        private const string LayoutFile = "dock_layout.json";
        private void LoadAvailableLayouts()
        {
            AvailableLayouts = Directory.GetFiles(LayoutsDir, "*.json")
                .Select<string,string>(Path.GetFileNameWithoutExtension)
                .ToList();
        }

        [RelayCommand]
        private async Task SaveLayout(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            var serializer = new DockSerializer(typeof(IDock));
            var filePath = Path.Combine(LayoutsDir, $"{name}.json");
            await using var stream = new FileStream(filePath, FileMode.Create);
            serializer.Save(stream, Layout);

            // Update last used
            //Preferences.Default.Set(LastLayoutKey, name);  // Use Avalonia Preferences or similar

            LoadAvailableLayouts();  // Refresh list
        }

        [RelayCommand]
        private async Task SaveLayoutDialog()
        {
            var name = "";// await ShowInputDialog("Save Layout As", "Enter name");
            if (!string.IsNullOrWhiteSpace(name))
                await SaveLayout(name);
        }

        [RelayCommand]
        private async Task LoadLayout(string name)
        {
            if (_factory == null || string.IsNullOrWhiteSpace(name)) return;

            var filePath = Path.Combine(LayoutsDir, $"{name}.json");
            if (!File.Exists(filePath)) return;

            var serializer = new DockSerializer(typeof(IDock));
            await using var stream = new FileStream(filePath, FileMode.Open);
            var loaded = serializer.Load<RootDock>(stream);

            if (loaded != null)
            {
                Layout = loaded;
                _factory.InitLayout(Layout);  // Re-init after load
            }
            else
            {
                defaultLayout();
            }


            // Update last used
            //Preferences.Default.Set(LastLayoutKey, name);
        }
        
        private void LoadLastUsedLayout()
        {
            //string lastName = Preferences.Default.Get(LastLayoutKey, "Default");
            //LoadLayout(lastName);  // Async, but fire-and-forget for startup

            defaultLayout();
        }

        [RelayCommand]
        private void NewChartDocument()
        {
            if(_factory==null || _documentDock==null) return;


            if (_documentDock.VisibleDockables == null || _documentDock.VisibleDockables.Count == 0)
            {
                var parent = _documentDock.Owner as IProportionalDock;
                if (parent != null && parent.VisibleDockables!=null)
                {
                    var index = parent.VisibleDockables.IndexOf(_documentDock);
                    if (index < 0)
                    {
                        int found = 0;
                        for (int i = 0; i < parent.VisibleDockables.Count; i++)
                        {
                            if (parent.VisibleDockables[i] is ToolDock) found++;
                        }
                        // Re-insert if somehow removed (rare)
                        if (found > 0)
                        {
                            int n = found == 1 ? 1 : 1;
                            parent.VisibleDockables.Insert(n, _factory.CreateProportionalDockSplitter());
                            parent.VisibleDockables.Insert(n, _documentDock);
                            parent.VisibleDockables.Insert(n, _factory.CreateProportionalDockSplitter());
                        }
                        else
                        {
                            parent.VisibleDockables.Add(_factory.CreateProportionalDockSplitter());
                            parent.VisibleDockables.Add(_documentDock);
                            parent.VisibleDockables.Add(_factory.CreateProportionalDockSplitter());
                        }
                    }
                }
            }

            ChartControlViewModel newVM = new ChartControlViewModel();

            _factory.AddDockable(_documentDock, newVM);
            _factory.SetActiveDockable(newVM);
            _factory.SetFocusedDockable(_documentDock,newVM);

        }
        
        [RelayCommand]
        private void RemoveChartDocument()
        {
            if (_factory!=null && _documentDock?.ActiveDockable is IDockable dockable)
            {
                _factory.CloseDockable(dockable);
            }
        }
    }


}
