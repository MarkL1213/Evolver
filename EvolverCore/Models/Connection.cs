using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EvolverCore.Models
{
    public enum ConnectionState { Disconnected, Connecting, Connected, Disconnecting, Error};

    public class ConnectionDataUpdateEventArgs : EventArgs
    {
    }

    public class ConnectionStateChangeEventArgs : EventArgs
    {
    }

    public class ConnectionSettings
    {
        public ConnectionSettings(string name) { Name = name; }
        public ConnectionSettings(ConnectionSettings src) { Name = src.Name; }

        public string Name { get; set; }
    }


    public class Connection : IDisposable
    {
        internal Connection(ConnectionSettings properties)
        {
            _connectionWorker = new Thread(connectionWorker);
            _connectionWorker.Name = "Connection Worker";
            _connectionWorker.Start();
            Properties = properties;
        }

        private bool disposedValue;
        private Thread _connectionWorker;
        private bool _wantExit = false;
        private bool _isSleeping = false;
        private int _connectRetryCounter = 0;

        public int MaxConnectionRetryCount { get; set; } = 3;

        public bool WantConnect { get; private set; } = false;
        public ConnectionSettings Properties { get; private set; }
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        public event EventHandler<ConnectionStateChangeEventArgs>? StateChange;

        public event EventHandler<ConnectionDataUpdateEventArgs>? DataUpdate;

        public void Connect()
        {
            if (State == ConnectionState.Connecting || State == ConnectionState.Connected) return;
            State = ConnectionState.Connecting;
            WantConnect = true;
            if (StateChange != null) StateChange(this, new ConnectionStateChangeEventArgs());

            wakeup();
        }

        public void Disconnect()
        {
            if (State != ConnectionState.Connecting && State != ConnectionState.Connected) return;
            State = ConnectionState.Disconnecting;
            WantConnect = false;
            if (StateChange != null) StateChange(this, new ConnectionStateChangeEventArgs());
            wakeup();
        }


        private void connectionWorker()
        {
            try
            {
                while (true)
                {
                    if (_wantExit) break;

                    try
                    {
                        if ((State == ConnectionState.Disconnected || State == ConnectionState.Error) && (!WantConnect || _connectRetryCounter >= MaxConnectionRetryCount))
                        {
                            _isSleeping = true;
                            Thread.Sleep(Timeout.Infinite);
                        }
                        else if (State == ConnectionState.Disconnecting)
                        {
                            //TODO: perform disconnect

                            State = ConnectionState.Disconnected;
                            Thread.MemoryBarrier();
                            if (StateChange != null) StateChange(this, new ConnectionStateChangeEventArgs());
                            continue;
                        }
                        else if (State == ConnectionState.Connecting || (State != ConnectionState.Connected && WantConnect && _connectRetryCounter < MaxConnectionRetryCount))
                        {
                            //TODO: perform connect

                            State = ConnectionState.Connected;
                            Thread.MemoryBarrier();
                            if (StateChange != null) StateChange(this, new ConnectionStateChangeEventArgs());
                            continue;
                        }
                        else if (State == ConnectionState.Connected)
                        {
                            //TODO: do connection stuff...

                            ///////////
                            /// Fake connection for testing
                            Thread.Sleep(5000);
                            if(DataUpdate != null) DataUpdate(this,new ConnectionDataUpdateEventArgs());
                            ///////////
                        }

                    }
                    catch (ThreadInterruptedException)
                    {
                        _isSleeping = false;
                    }
                    catch (ThreadAbortException)
                    {

                    }
                    catch (Exception e)
                    {
                        Globals.Instance.Log.LogMessage("Connection.connectionWorker thread exception:", LogLevel.Error);
                        Globals.Instance.Log.LogException(e);
                    }
                }
            }
            catch (ThreadAbortException)
            {

            }
            catch (Exception e)
            {
                Globals.Instance.Log.LogMessage("Connection.connectionWorker thread exception:", LogLevel.Error);
                Globals.Instance.Log.LogException(e);
            }
        }

        private void wakeup()
        {
            if (_isSleeping && _connectionWorker.IsAlive) _connectionWorker.Interrupt();
        }

        internal void Shutdown()
        {
            _wantExit = true;
            wakeup();

            if (_connectionWorker.IsAlive)
            {
                if (!_connectionWorker.Join(TimeSpan.FromSeconds(3)))
                {
                    Globals.Instance.Log.LogMessage("Connection.connectionWorker failed to shutdown.", LogLevel.Error);
                }
            }
            else
            {
                Globals.Instance.Log.LogMessage("Connection.connectionWorker was already terminated.", LogLevel.Warn);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Shutdown();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }


    public class ConnectionCollection
    {
        Dictionary<string, Connection> _activeConnections = new Dictionary<string, Connection>();

        Dictionary<string, ConnectionSettings> _knownConnections = new Dictionary<string, ConnectionSettings>();

        private object _lock = new object();

        public ConnectionCollection()
        {
            _knownConnections.Add("test", new ConnectionSettings("test"));
        }

        public List<string> GetKnownConnections() { return _knownConnections.Keys.ToList(); }

        public ConnectionState GetConnectionState(string cName)
        {
            lock (_lock)
            {
                if (_activeConnections.ContainsKey(cName)) return _activeConnections[cName].State;
                return ConnectionState.Disconnected;
            }
        }

        public Connection? CreateConnection(string connectionName)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(connectionName) || !_knownConnections.ContainsKey(connectionName)) return null;

                Connection connection = new Connection(new ConnectionSettings(_knownConnections[connectionName]));
                connection.StateChange += ConnectionStateChange;

                _activeConnections.Add(connectionName, connection);

                connection.Connect();
                return connection;
            }
        }

        public void TeardownConnection(string connectionName)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(connectionName) || !_activeConnections.ContainsKey(connectionName)) return;
                _activeConnections[connectionName].Disconnect();
                return;
            }
        }

        public void ShutdownAll()
        {
            lock (_lock)
            {
                foreach (Connection connection in _activeConnections.Values)
                {
                    connection.Shutdown();
                    connection.Dispose();
                }
                _activeConnections.Clear();
            }
        }

        private void ConnectionStateChange(object? sender, ConnectionStateChangeEventArgs e)
        {
            Connection? connection = sender as Connection;
            if (connection == null) return;

            if (connection.State == ConnectionState.Connected)
                connection.DataUpdate += Globals.Instance.DataManager.OnConnectionDataUpdate;

            if (connection.State == ConnectionState.Disconnected || connection.State == ConnectionState.Error)
            {
                connection.DataUpdate -= Globals.Instance.DataManager.OnConnectionDataUpdate;
                if (!connection.WantConnect)
                {
                    Connection c;
                    lock (_lock)
                    {
                        c = _activeConnections[connection.Properties.Name];
                        _activeConnections.Remove(connection.Properties.Name);
                    }

                    c.Shutdown();
                    c.Dispose();
                }
            }
        }
    }
}
