using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using EvolverCore.ViewModels;
using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace EvolverCore;

public partial class ConnectionStatusControl : Control
{
    public ConnectionStatusControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ConnectionName = string.Empty;
        
        DataContext = new ConnectionStatusViewModel();
    }

    public ConnectionStatusControl(string connectionName)
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        ConnectionName = connectionName;
        
        DataContext = new ConnectionStatusViewModel();
    }

    public string ConnectionName { get; private set; }

    private ConnectionStatusViewModel? _vm = null;

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Invoke(new Action(() => { OnDataContextChanged(sender, e); }));
            return;
        }

        if (_vm != null)
        {
            _vm.Status.CollectionChanged -= OnStatusCollectionChanged;
            foreach (var status in _vm.Status)
            {
                status.PropertyChanged -= OnStatusItemPropertyChanged;
            }
        }

        ConnectionStatusViewModel? vm = DataContext as ConnectionStatusViewModel;
        _vm = vm;
        if (_vm != null)
        {
            _vm.Status.CollectionChanged += OnStatusCollectionChanged;
            foreach (var status in _vm.Status)
            {
                status.PropertyChanged += OnStatusItemPropertyChanged;
            }
            InvalidateVisual();  // Initial render
        }
    }

    private void OnStatusCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Handle added/removed items
        if (e.NewItems != null)
        {
            foreach (ConnectionStatus newStatus in e.NewItems)
            {
                newStatus.PropertyChanged += OnStatusItemPropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (ConnectionStatus oldStatus in e.OldItems)
            {
                oldStatus.PropertyChanged -= OnStatusItemPropertyChanged;
            }
        }
        if (!Dispatcher.UIThread.CheckAccess())
            Dispatcher.UIThread.InvokeAsync(() => InvalidateVisual());
        else
            InvalidateVisual();
    }

    private void OnStatusItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConnectionStatus.State))  // Only care about State changes
        {
            if (!Dispatcher.UIThread.CheckAccess())
                Dispatcher.UIThread.InvokeAsync(() => InvalidateVisual());
            else
                InvalidateVisual();
        }
    }

    Pen _borderPen = new Pen(Brushes.Black, 1);

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        ConnectionStatusViewModel? vm = DataContext as ConnectionStatusViewModel;
        if (vm == null) return;

        IBrush fillBrush = Brushes.Transparent;

        bool transitionState = false;
        foreach (ConnectionStatus status in vm.Status)
        {
            if (!string.IsNullOrEmpty(ConnectionName) && status.Name != ConnectionName) continue;

            if (status.State == Models.ConnectionState.Error)
            {
                fillBrush = Brushes.Red;
                break;
            }
            else if (status.State == Models.ConnectionState.Connecting || status.State == Models.ConnectionState.Disconnecting)
            {
                fillBrush = Brushes.Yellow;
                transitionState = true;
            }
            else if (status.State == Models.ConnectionState.Connected && !transitionState)
            {
                fillBrush = Brushes.Green;
            }
        }

        using (DrawingContext.PushedState clipState = context.PushClip(new Rect(Bounds.Size)))
        {
            //context.FillRectangle(Brushes.LightGray, Bounds);

            double size = Math.Min(Bounds.Height, Bounds.Width) / 3;
            context.DrawEllipse(fillBrush, _borderPen, Bounds.Center, size, size);
        }
    }
}