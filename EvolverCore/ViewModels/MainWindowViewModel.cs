using Avalonia.Controls;

namespace EvolverCore.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        public string WindowTitle { get; } = "Evolver";
        public WindowIcon? WindowIcon { get; } = new WindowIcon("D:/Evolver/EvolverCore/Assets/avalonia-logo.ico");
    }
}
