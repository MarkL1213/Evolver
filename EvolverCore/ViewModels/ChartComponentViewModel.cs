using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{
    internal class ChartComponentViewModel : ViewModelBase
    {
        public string Name { set; get; } = string.Empty;
        public string Description { set; get; } = string.Empty;
        public int ChartPanelNumber { get; set; } = 0;
    }
}
