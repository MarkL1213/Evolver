using Avalonia;
using Avalonia.Styling;
using System;
using System.Threading.Tasks;

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
            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Globals.Instance.Log.LogMessage("***** Evolver Unobserved Task Exception *****", Models.LogLevel.Error);
                Globals.Instance.Log.LogException(e.Exception);
                e.SetObserved();
            };

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

            try
            {
                Globals.Instance.Connections.ShutdownAll();
            }
            catch (Exception ex)
            {
                Globals.Instance.Log.LogMessage("***** Evolver Exception *****", Models.LogLevel.Error);
                Globals.Instance.Log.LogException(ex);
            }

            try
            {
                Globals.Instance.DataManager.Shutdown();
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
                .With(new Win32PlatformOptions { RenderingMode = new[] { Win32RenderingMode.Wgl } })
                .With(new MacOSPlatformOptions { ShowInDock = true })
                .WithInterFont()
                .LogToTrace();
    }

    public static class ThemeService
    {
        private static ThemeVariant? _currentTheme;

        public static void ToggleTheme()
        {
            var app = Application.Current!;
            var newTheme = _currentTheme == ThemeVariant.Dark
                ? ThemeVariant.Light
                : ThemeVariant.Dark;

            app.RequestedThemeVariant = newTheme;
            _currentTheme = newTheme;
        }

        public static void SetTheme(bool isDark)
        {
            var app = Application.Current!;
            var theme = isDark ? ThemeVariant.Dark : ThemeVariant.Light;

            if (_currentTheme != theme)
            {
                app.RequestedThemeVariant = theme;
                _currentTheme = theme;
            }
        }

        public static bool IsDark => _currentTheme == ThemeVariant.Dark;
    }
}
