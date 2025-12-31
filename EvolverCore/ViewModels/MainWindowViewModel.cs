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
using Avalonia.OpenGL;
using EvolverCore.Models;

namespace EvolverCore.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public string WindowTitle { get; } = "Evolver";
        public WindowIcon? WindowIcon { get; } = new WindowIcon("D:/Evolver/EvolverCore/Assets/avalonia-logo.ico");

        [ObservableProperty] ObservableCollection<Layout> _availableLayouts = new();
        [ObservableProperty] Layout? _currentLayout = null;

        internal ChartControlViewModel DefaultChartViewModel { get; } = new();

        public MainWindowViewModel()
        {
            LoadAvailableLayouts();
        }



        public void LoadAvailableLayouts()
        {
            AvailableLayouts.Clear();
            try
            {
                if (!Directory.Exists(Globals.Instance.LayoutDirectory))
                {
                    Directory.CreateDirectory(Globals.Instance.LayoutDirectory);
                }

                string[] dirs=Directory.GetDirectories(Globals.Instance.LayoutDirectory);
                
                foreach (string dir in dirs)
                {
                    Layout newLayout = new Layout();

                    string? path = Path.GetFileName(dir);
                    if (path == null) continue;

                    newLayout.Name = path;
                    if(newLayout.SerializationFileExists && newLayout.VMSerializationFileExists)
                        AvailableLayouts.Add(newLayout);
                }

            }
            catch (Exception e)
            {
                Globals.Instance.Log.LogMessage("Failed to load available layout. " + e.Message, LogLevel.Error);
            }
        }



        [RelayCommand]
        internal void SaveLayout(Layout layout)
        {
            if (string.IsNullOrWhiteSpace(layout.Name)) return;

            if (!layout.DirectoryExists) layout.CreateDirectory();

            DockManager dockManager = MyContainer.TheDockManager;

            dockManager.SaveToFile(layout.SerializationFileName);
            dockManager.SaveViewModelsToFile(layout.VMSerializationFileName);

            LoadAvailableLayouts();  // Refresh list
        }


        [RelayCommand]
        internal void LoadLayout(Layout layout)
        {
            if (string.IsNullOrWhiteSpace(layout.Name)) return;

            DockManager dockManager = MyContainer.TheDockManager;

            dockManager.DockItemsViewModels = null;
            dockManager.RestoreFromFile(layout.SerializationFileName);

            dockManager
                .RestoreViewModelsFromFile
                (
                    layout.VMSerializationFileName,
                    typeof(ChartControlDockItemViewModel)
                  );

            dockManager.DockItemsViewModels?.OfType<ChartControlDockItemViewModel>().FirstOrDefault()?.Select();
        }


        private static int _docCount = 2;

        [RelayCommand]
        private void NewChartDocument()
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

        [RelayCommand]
        private void RemoveChartDocument()
        {
            if (MyContainer.TheDockManager.DockItemsViewModels == null) return;

            DockItemViewModelBase? target = MyContainer.TheDockManager.DockItemsViewModels.FirstOrDefault(x => x.IsSelected);
            if (target == null) return;

            MyContainer.TheDockManager.DockItemsViewModels.Remove(target);
        }
    }

    public class ChartControlDockItemViewModel : DockItemViewModel<ChartControlViewModel>
    {
    }
}
