using System.Linq;

namespace EvolverCore.Views
{
    internal class DataComponent : IndicatorComponent
    {
        public DataComponent(ChartPanel panel) : base(panel) { }

        public override double MinY()
        {
            if (SnapPoints == null || SnapPoints.RowCount == 0) CalculateSnapPoints();
            if (SnapPoints == null || SnapPoints.RowCount == 0) return 0;

            return SnapPoints.Low.Min();
        }
        public override double MaxY()
        {
            if (SnapPoints == null || SnapPoints.RowCount == 0) CalculateSnapPoints();
            if (SnapPoints == null || SnapPoints.RowCount == 0) return 100;

            return SnapPoints.High.Max();
        }
    }
}
