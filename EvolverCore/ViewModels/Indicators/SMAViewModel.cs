using CommunityToolkit.Mvvm.ComponentModel;
using EvolverCore.Views.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels.Indicators
{
    internal partial class SMAViewModel : IndicatorViewModel
    {
        [ObservableProperty] int _period = 14;

        public SMAViewModel(IndicatorDataSourceRecord source, int period)
        {
            Name = "SMA";
            _period = period;
            DataRecord = source;
        }
    }
}
