using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NP.Ava.UniDock;
using NP.Ava.UniDock.Factories;
using NP.UniDockService;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System;
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

        private const string LastLayoutKey = "LastLayoutName";
        private const string LayoutsDir = "Layouts";

        [ObservableProperty]
        private List<string> _availableLayouts = new();
        [ObservableProperty] string _currentLayoutName = string.Empty;

        internal ChartControlViewModel DefaultChartViewModel { get; } = new();

        public MainWindowViewModel() { }



        private const string LayoutFile = "dock_layout.json";
        private void LoadAvailableLayouts()
        {
            try
            {
                AvailableLayouts = Directory.GetFiles(LayoutsDir, "*.json")
                    .Select<string, string>(Path.GetFileNameWithoutExtension)
                    .ToList();
            }
            catch (Exception)
            {
                AvailableLayouts.Clear();
            }
        }

        [RelayCommand]
        private async Task SaveLayout(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            //var serializer = new DockSerializer(typeof(IDock));
            //var filePath = Path.Combine(LayoutsDir, $"{name}.json");
            //await using var stream = new FileStream(filePath, FileMode.Create);
            //serializer.Save(stream, Layout);

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
            //if (_factory == null || string.IsNullOrWhiteSpace(name)) return;

            //var filePath = Path.Combine(LayoutsDir, $"{name}.json");
            //if (!File.Exists(filePath)) return;

            //var serializer = new DockSerializer(typeof(IDock));
            //await using var stream = new FileStream(filePath, FileMode.Open);
            //var loaded = serializer.Load<RootDock>(stream);

            //if (loaded != null)
            //{
            //    Layout = loaded;
            //    _factory.InitLayout(Layout);  // Re-init after load
            //}
            //else
            //{
            //    defaultLayout();
            //}


            // Update last used
            //Preferences.Default.Set(LastLayoutKey, name);
        }

        private void LoadLastUsedLayout()
        {
            //string lastName = Preferences.Default.Get(LastLayoutKey, "Default");
            //LoadLayout(lastName);  // Async, but fire-and-forget for startup

            //defaultLayout();
        }

        private static int _docCount = 2;



        private bool _isCreatingChart = false;

        [RelayCommand]
        private void NewChartDocument()
        {
            if (_isCreatingChart) return;

            _isCreatingChart = true;
            try
            {
                string name = $"DefaultChart-{_docCount}";
                ChartControlViewModel vm = new ChartControlViewModel()
                {
                    Name = name
                };

                var newTabVm = new ChartControlDockItemViewModel()
                {
                    DockId = name,
                    DefaultDockGroupId = "ChartTabGroup",
                    DefaultDockOrderInGroup = _docCount,
                    Header = name,
                    //ContentTemplateResourceKey = "ChartContolViewModelTemplate",
                    HeaderContentTemplateResourceKey = "ChartControlHeaderTemplate",
                    TheVM = vm,
                    IsPredefined = false,
                    CanFloat = true,
                    CanClose = true
                };

                MyContainer.TheDockManager.DockItemsViewModels!.Add(newTabVm);
                _docCount++;

                newTabVm.IsSelected = true;
            }
            finally { _isCreatingChart = false; }
        }

        [RelayCommand]
        private void RemoveChartDocument()
        {
            if (MyContainer.TheDockManager.DockItemsViewModels == null) return;

            DockItemViewModelBase? target = MyContainer.TheDockManager.DockItemsViewModels.FirstOrDefault(x => x.IsSelected);
            if (target == null) return;

            MyContainer.TheDockManager.DockItemsViewModels.Remove(target);
        }
    }

    internal class ChartControlDockItemViewModel : DockItemViewModel<ChartControlViewModel>
    {
    }
}
