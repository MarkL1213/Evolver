using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    internal partial class ChartXAxisViewModel : ViewModelBase
    {
        [ObservableProperty] DateTime _min = DateTime.Today;
        [ObservableProperty] DateTime _max = DateTime.Today.AddDays(1);
    }
}
