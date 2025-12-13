using Avalonia;
using Avalonia.Media;
using EvolverCore.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.Views.Components
{
    internal interface IChartComponentRenderer
    {
        public int RenderOrder { get; set; }

        public ChartPanel Parent { get; }

        public void Render(DrawingContext context);
    }

    internal class ChartComponentBase : AvaloniaObject, IChartComponentRenderer
    {
        public ChartComponentBase(ChartPanel parent)
        {
            Parent = parent;
        }

        internal ChartComponentViewModel Properties { get; set; } = new ChartComponentViewModel();

        public ChartPanel Parent { get; private set; }

        #region Name property
        public static readonly StyledProperty<string> NameProperty =
            AvaloniaProperty.Register<ChartComponentBase, string>(nameof(Name), "ChartComponent");
        public string Name
        {
            get { return GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }
        #endregion
        
        #region Description property
        public static readonly StyledProperty<string> DescriptionProperty =
            AvaloniaProperty.Register<ChartComponentBase, string>(nameof(Description), string.Empty);
        public string Description
        {
            get { return GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }
        #endregion

        #region ChartPanelNumber property
        public static readonly StyledProperty<int> ChartPanelNumberProperty =
            AvaloniaProperty.Register<ChartComponentBase, int>(nameof(ChartPanelNumber), 0);
        public int ChartPanelNumber
        {
            get { return GetValue(ChartPanelNumberProperty); }
            set { SetValue(ChartPanelNumberProperty, value); }
        }
        #endregion

        #region RenderOrder property
        public static readonly StyledProperty<int> RenderOrderProperty =
            AvaloniaProperty.Register<ChartComponentBase, int>(nameof(RenderOrder), 0);
        public int RenderOrder
        {
            get { return GetValue(RenderOrderProperty); }
            set { SetValue(RenderOrderProperty, value); }
        }
        #endregion


        public virtual void Render(DrawingContext context) { }
    }
}
