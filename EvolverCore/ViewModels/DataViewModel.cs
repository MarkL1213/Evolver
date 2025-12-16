using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    internal class DataViewModel : ChartComponentViewModel
    {
        internal DataPlotViewModel? DataPlot { get; set; } 
    }
}
