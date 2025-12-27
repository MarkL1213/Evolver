using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EvolverCore.Models
{
    [Serializable]
    public class SerializableBrush : ObservableObject
    {
        private static BrushConverter _brushConverter = new BrushConverter();
        private IBrush _color = Brushes.Cyan;

        public SerializableBrush() { }
        public SerializableBrush(IBrush color) { _color = color; }

        [XmlIgnore]
        public IBrush Color
        {
            get { return _color; }
            set { SetProperty(ref _color, value); }
        }

        [XmlElement("Color")]
        [Browsable(false)]
        public string ColorSerializable
        {
            set
            {
                IBrush? b = _brushConverter.ConvertFromString(value) as IBrush;
                if (b != null) Color = b;
            }
            get
            {
                string? res = Color.ToString();
                return string.IsNullOrEmpty(res) ? string.Empty : res;
            }
        }
    }
}
