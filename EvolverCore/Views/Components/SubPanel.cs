using Avalonia.Controls;
using EvolverCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EvolverCore.Views.Components
{
    public class SubPanel
    {
        public int Number { set; get; }
        public Guid ID { get; set; }
        
        [XmlIgnore]
        public ChartPanel? Panel { get; set; }
        
        [XmlIgnore]
        public ChartYAxis? Axis { get; set; }
        
        [XmlIgnore]
        public GridSplitter? Splitter { get; set; }
        
        public ChartPanelViewModel? ViewModel { get; set; }
    }
}
