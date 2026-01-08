using Avalonia.Controls;
using Avalonia.Controls.Templates;
using EvolverCore.ViewModels;
using System;
using System.Diagnostics.CodeAnalysis;

namespace EvolverCore
{
    /// <summary>
    /// Given a view model, returns the corresponding view if possible.
    /// </summary>
    [RequiresUnreferencedCode(
        "Default implementation of ViewLocator involves reflection which may be trimmed away.",
        Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
    public class ViewLocator : IDataTemplate
    {
        public Control? Build(object? param)
        {
            if (param is null)
                return null;



            var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(name);

            if (param.GetType().Name == "ChartControlViewModel")
                type = typeof(ChartControl);

            if (param.GetType().Name == "LogControlViewModel")
                type = typeof(LogControl);


            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object? data)
        {
            return data is ViewModelBase;
        }
    }
}
