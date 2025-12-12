using Avalonia.Media;
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
        public static IBrush DefaultLabelColor = Brushes.White;
        public static IBrush DefaultBackgroundColor = Brushes.Black;
        public static int DefaultFontSize = 12;

        [ObservableProperty] DateTime _min = DateTime.Today;
        [ObservableProperty] DateTime _max = DateTime.Today.AddDays(1);

        [ObservableProperty] IBrush _backgroundColor = DefaultBackgroundColor;
        [ObservableProperty] IBrush _labelColor = DefaultLabelColor;
        [ObservableProperty] int _fontSize = DefaultFontSize;


    }
}
