using Dock.Model.Mvvm.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EvolverCore.ViewModels
{

    internal partial class Tool1ViewModel : Tool//Document
    {
        public Tool1ViewModel()
        {
            Id = "Tool1";
            Title = "Tool One";
            CanClose = false;
            CanFloat = true;
        }
    }

}
