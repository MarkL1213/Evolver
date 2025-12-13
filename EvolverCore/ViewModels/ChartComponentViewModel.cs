using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    internal partial class ChartComponentViewModel : ViewModelBase
    {
        [ObservableProperty] string _name = string.Empty;
        [ObservableProperty] string _description = string.Empty;
        [ObservableProperty] int _chartPanelNumber = 0;
        [ObservableProperty] int _renderOrder = 0;
    }
}
