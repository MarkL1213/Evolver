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
    public interface IChartComponentRenderer
    {
        public int RenderOrder { get; set; }

        public ChartPanel Parent { get; }

        public void Render(DrawingContext context);
    }

    public class ChartComponentBase : AvaloniaObject, IChartComponentRenderer
    {
        public ChartComponentBase(ChartPanel parent)
        {
            Parent = parent;
        }

        ChartComponentViewModel _properties = new ChartComponentViewModel();
        internal ChartComponentViewModel Properties
        {
            get { return _properties; }
            set { SetDataContext(value); }
        }

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

        internal void SetDataContext(ChartComponentViewModel vm)
        {
            _properties = vm;
            Parent.InvalidateVisual();
        }

        public virtual double MinY() { return 0; }
        public virtual double MaxY() { return 100; }


        private List<IDataPoint> _snapPoints = new List<IDataPoint>();
        public List<IDataPoint> SnapPoints { get { return _snapPoints; } }

        public virtual void CalculateSnapPoints()
        {
            SnapPoints.Clear();

            ChartPanelViewModel? panelVM = Parent.DataContext as ChartPanelViewModel;
            if (panelVM == null || panelVM.XAxis == null) return;

            IndicatorViewModel? ivm = Properties as IndicatorViewModel;
            if (ivm == null || ivm.Indicator == null || ivm.Indicator.InputElementCount() == 0) return;

            IEnumerable<IDataPoint> v = ivm.Indicator.SelectInputPointsInRange(panelVM.XAxis.Min, panelVM.XAxis.Max);
            SnapPoints.AddRange(v.ToList());
        }

        public virtual void UpdateVisualRange(DateTime rangeMin, DateTime rangeMax) { }

        public virtual void Calculate() { }
        public virtual void Render(DrawingContext context) { }
    }
}
