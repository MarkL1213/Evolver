using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvolverCore.Data;

namespace EvolverCore.ViewModels
{
    public abstract class DataViewModel : ViewModelBase
    { }

    internal class BarDataViewModel : DataViewModel
    {
        public BarDataSeries? BarDataSeries { get; set; }
    }
}
