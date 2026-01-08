using Avalonia.Controls;
using Avalonia.Threading;
using EvolverCore.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace EvolverCore;

public partial class LogControl : UserControl
{
    public LogControl()
    {
        InitializeComponent();
        DataContext = new LogControlViewModel();

        Globals.Instance.Log.RegisterLogControl(this);

        Loaded += LogControlLoaded;
    }

    private void LogControlLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.InvokeAsync(() => loadExistingLogFile());
            return;
        }

        loadExistingLogFile();
    }

    ~LogControl()
    {
        Globals.Instance.Log.UnRegisterLogControl(this);
    }

    private void loadExistingLogFile()
    {
        try
        {
            LogControlViewModel? viewModel = DataContext as LogControlViewModel;
            if (viewModel == null)
                throw new EvolverException("LogControl does not have the correct ViewModel.");

            viewModel.LogLines.Clear();

            FileStreamOptions options = new FileStreamOptions();
            options.Mode = FileMode.OpenOrCreate;
            options.Access = FileAccess.Read;
            options.Share = FileShare.ReadWrite;

            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(Globals.Instance.LogFileName, options))
            {
                string? s = reader.ReadToEnd();
                if (s == null) return;
                lines.Add(s);
            }

            viewModel.LoadExistingLog(lines);
        }
        catch (Exception ex)
        {
            Globals.Instance.Log.LogException(ex);
        }
    }

    public void AppendText(string text)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.InvokeAsync(() => AppendText(text));
            return;
        }

        LogControlViewModel? viewModel = DataContext as LogControlViewModel;
        if (viewModel == null)
            throw new EvolverException("LogControl does not have the correct ViewModel.");

        viewModel.AppendLines(text);

        if (LogListBox.ItemCount > 0)
        {
            LogListBox.ScrollIntoView(LogListBox.ItemCount - 1);
        }
    }


}