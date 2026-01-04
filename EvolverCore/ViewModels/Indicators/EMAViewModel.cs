using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels.Indicators
{
    internal partial class EMAViewModel : IndicatorViewModel
    {
        [ObservableProperty] int _period = 14;
        [ObservableProperty] int _smoothing = 2;

        public EMAViewModel(IndicatorDataSourceRecord source, int period, int smoothing)
        {
            Name = "EMA";
            _period = period;
            _smoothing = smoothing;
            DataRecord = source;
        }


    }
}
