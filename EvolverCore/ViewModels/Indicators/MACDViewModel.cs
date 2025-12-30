using CommunityToolkit.Mvvm.ComponentModel;
using EvolverCore.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EvolverCore.ViewModels.Indicators
{
    internal partial class MACDViewModel : IndicatorViewModel
    {
        [ObservableProperty] int _fast = 12;
        [ObservableProperty] int _slow = 26;
        [ObservableProperty] int _smoothing = 9;

        public MACDViewModel(IndicatorDataSlice source, int fast, int slow, int smoothing)
        {
            Name = "MACD";
            _fast = fast;
            _slow = slow;
            _smoothing = smoothing;
            Data = source;
        }

    }
}
