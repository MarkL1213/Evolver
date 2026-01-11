using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using EvolverCore.Models;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;
using System.Net.Http.Headers;
using CommunityToolkit.Mvvm.ComponentModel;


namespace EvolverCore.ViewModels
{
    public partial class ConnectionStatus : ObservableObject
    {
        public ConnectionStatus(string name, ConnectionState state)  { Name = name; State = state; }

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private ConnectionState _state = ConnectionState.Disconnected;
    }

    public partial class ConnectionStatusViewModel : ViewModelBase
    {
        public ObservableCollection<ConnectionStatus> Status { get; } = new ObservableCollection<ConnectionStatus>();

        public void OnConnectionStatusChange(object? sender, ConnectionStateChangeEventArgs args)
        {
            Connection? c = sender as Connection; 
            if (c == null) return;

            ConnectionStatus? s = Status.FirstOrDefault(x => x.Name == c.Properties.Name);
            if (s==null)
            {
                Globals.Instance.Log.LogMessage($"Received status update for unknown connection {c.Properties.Name}.", LogLevel.Error);
                return;
            }

            s.State = c.State;
        }

        [RelayCommand]
        public void ConnectionMenuItemClicked(string? cName)
        {
            if (string.IsNullOrEmpty(cName)) return;

            var currentState = Globals.Instance.Connections.GetConnectionState(cName);
            if (currentState == ConnectionState.Connected || currentState == ConnectionState.Connecting)
               Globals.Instance.Connections.TeardownConnection(cName);
            else
                CreateConnection(cName);
        }

        public void CreateConnection(string cName)
        {
            ConnectionState state = Globals.Instance.Connections.GetConnectionState(cName);
            if (state != ConnectionState.Disconnected)
            {
                Globals.Instance.Log.LogMessage($"Unable to create connection. Connection {cName} is in state {state}.", LogLevel.Error);
                return;
            }

            Connection? c=Globals.Instance.Connections.CreateConnection(cName);
            if (c == null)
            {
                Globals.Instance.Log.LogMessage($"Failed to create connection {cName}.", LogLevel.Error);
                return;
            }

            ConnectionStatus? status = Status.FirstOrDefault(x => x.Name ==  cName);
            if (status == null)
                Status.Add(new ConnectionStatus(cName, c.State));
            else
                status.State = c.State;

            c.StateChange -= OnConnectionStatusChange;
            c.StateChange += OnConnectionStatusChange;
        }
    }
}
