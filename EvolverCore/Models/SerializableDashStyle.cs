using Avalonia.Controls.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
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
    public class SerializableDashStyle : ObservableObject
    {
        //private IDashStyle? _dashStyle;

        public SerializableDashStyle() { }
        public SerializableDashStyle(IDashStyle style)
        {
            Style = style;
        }

        [XmlIgnore]
        public IDashStyle? Style
        {
            get { return (_dashes != null && _dashes.Count > 0) ? new DashStyle(_dashes, _offset) : null; }
            set {
                SetProperty(ref _offset, (value != null) ? value.Offset : 0);
                SetProperty(ref _dashes, (value != null && value.Dashes != null) ? value.Dashes.ToList() : null);
            }
        }

        List<double>? _dashes = null;
        double _offset = 0;

        [Browsable(false)]
        public List<double>? Dashes
        {
            set
            {
                _dashes = value;
            }
            get
            {
                return (Style != null && Style.Dashes != null) ? Style.Dashes.ToList() : null;
            }
        }

        [Browsable(false)]
        public double Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }

    }
}
