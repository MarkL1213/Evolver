using CommunityToolkit.Mvvm.ComponentModel;

namespace EvolverCore.ViewModels
{
    internal partial class ChartComponentViewModel : ViewModelBase
    {
        [ObservableProperty] string _name = string.Empty;
        [ObservableProperty] string _description = string.Empty;
        [ObservableProperty] int _chartPanelNumber = 0;
        [ObservableProperty] int _renderOrder = 0;

        [ObservableProperty] bool _isHidden;
    }
}
