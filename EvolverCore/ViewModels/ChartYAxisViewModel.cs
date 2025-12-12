using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    internal partial class ChartYAxisViewModel : ViewModelBase
    {
        [ObservableProperty] double _min = 0;
        [ObservableProperty] double _max = 100;
    }
}
