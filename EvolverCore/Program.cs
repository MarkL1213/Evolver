using System;
using System.Collections.Generic;
using Avalonia;

namespace EvolverCore
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            Globals.Instance.Log.LogMessage("===== Evolver Started =====", Models.LogLevel.Info);
            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Globals.Instance.Log.LogMessage("***** Evolver Exception *****", Models.LogLevel.Error);
                Globals.Instance.Log.LogException(ex);
            }
            Globals.Instance.Log.LogMessage("===== Evolver Stopped =====", Models.LogLevel.Info);
            Globals.Instance.Log.Shutdown();
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new X11PlatformOptions { RenderingMode = new[] { X11RenderingMode.Glx } })
                .With(new AvaloniaNativePlatformOptions { RenderingMode = new[] { AvaloniaNativeRenderingMode.OpenGl } })
                .With(new Win32PlatformOptions { RenderingMode= new[] { Win32RenderingMode.Wgl } })
                .With(new MacOSPlatformOptions {  ShowInDock = true })
                .WithInterFont()
                .LogToTrace();
    }
}
