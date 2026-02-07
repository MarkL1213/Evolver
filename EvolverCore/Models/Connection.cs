using Parquet.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EvolverCore.Models
{
    public enum ConnectionState { Disconnected, Connecting, Connected, Disconnecting, Error};

    public enum DataEvent { Bid, Ask, Last, Settlement };

    internal class DataDownloadJobDoneArgs : EventArgs
    {
        public DataDownloadJobDoneArgs(DataDownloadJob sourceJob) { SourceJob = sourceJob; }
        
        public DataDownloadJob SourceJob { get; init; }
    }

    internal class DataDownloadSaveJob : DataSaveJob
    {
        public DataDownloadSaveJob(BarTable table, DataDownloadJob sourceJob ): base(table) { SourceJob = sourceJob; }

        public DataDownloadJob SourceJob { get; init; }
    }

    internal class DataDownloadJob
    {
        public DataDownloadJob(DataLoadJob loadJob, DateTime start, DateTime end) { LoadJob = loadJob; StartTime = start; EndTime = end; }

        public event EventHandler<DataDownloadJobDoneArgs>? JobDone;

        public DataLoadJob LoadJob { get; init; }
        public DateTime StartTime { get; init; }
        public DateTime EndTime { get; init; }

        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError { get { return !string.IsNullOrEmpty(ErrorMessage); } }



        public void FireJobDone()
        {
            JobDone?.Invoke(this,new DataDownloadJobDoneArgs(this));
        }
    }

    public class ConnectionDataUpdateEventArgs : EventArgs
    {
        private ConnectionDataUpdateEventArgs(Instrument instrument, DataEvent dataEvent, DateTime time)
        {
            Time = time;
            Event = dataEvent;
            Instrument = instrument;
        }
        public Instrument Instrument { get; private set; }
        public DataEvent Event { get; private set; }
        public DateTime Time { get; private set; }
        
        public double Bid { get; private set; } = 0;
        public double Ask { get; private set; } = 0;
        public double Price { get; private set; } = 0;
        public long Volume { get; private set; } = 0;

        public static ConnectionDataUpdateEventArgs CreateSettlementArgs(Instrument instrument, DateTime time, double price)
        {
            ConnectionDataUpdateEventArgs args = new ConnectionDataUpdateEventArgs(instrument, DataEvent.Settlement, time);
            args.Price = price;
            return args;
        }
        public static ConnectionDataUpdateEventArgs CreateLastArgs(Instrument instrument, DateTime time, double price, long volume)
        {
            ConnectionDataUpdateEventArgs args = new ConnectionDataUpdateEventArgs(instrument, DataEvent.Last, time);
            args.Price = price;
            args.Volume = volume;
            return args;
        }
    }

    public class ConnectionStateChangeEventArgs : EventArgs
    {
        public ConnectionStateChangeEventArgs(ConnectionState oldState, ConnectionState newState) { OldState = oldState; NewState = newState; }
        public ConnectionState OldState { get; private set; }
        public ConnectionState NewState { get; private set; }
    }

    public class ConnectionSettings
    {
        public ConnectionSettings(string name) { Name = name; }
        public ConnectionSettings(ConnectionSettings src) { Name = src.Name; }

        public string Name { get; set; }

        public bool IsPrimaryDataSource { get; set; }
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
        private bool _isShutdown = false;
        private int _connectRetryCounter = 0;

        public int MaxConnectionRetryCount { get; set; } = 3;

        public bool WantConnect { get; private set; } = false;
        public ConnectionSettings Properties { get; private set; }
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

        public event EventHandler<ConnectionStateChangeEventArgs>? StateChange;

        public event EventHandler<ConnectionDataUpdateEventArgs>? DataUpdate;

        private BlockingCollection<Func<CancellationToken, Task>> _dataJobQueue = new BlockingCollection<Func<CancellationToken, Task>>();
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public void Connect()
        {
            if (State == ConnectionState.Connecting || State == ConnectionState.Connected) return;
            ConnectionState oldState = State;
            State = ConnectionState.Connecting;
            WantConnect = true;
            StateChange?.Invoke(this, new ConnectionStateChangeEventArgs(oldState, State));

            wakeup();
        }

        public void Disconnect()
        {
            if (State != ConnectionState.Connecting && State != ConnectionState.Connected) return;
            ConnectionState oldState = State;
            State = ConnectionState.Disconnecting;
            WantConnect = false;
            StateChange?.Invoke(this, new ConnectionStateChangeEventArgs(oldState, State));

            wakeup();
        }

        internal void EnqueueDataDownloadJob(DataDownloadJob job)
        {

            if (_isShutdown) return;
            _dataJobQueue.Add((token) => ExecuteDataDownloadJob(token, job));
        }


        internal static string DownloadKey(Instrument i, DataInterval iv, DateTime s, DateTime e) => $"{i.Name}|{iv}|{s:O}|{e:O}";

        private readonly ConcurrentDictionary<string, List<(DataDownloadJob,DataSaveJob)>> _waitingDownloadJobs = new();
        private int _randomSeed = 420;
        private async Task ExecuteDataDownloadJob(CancellationToken token, DataDownloadJob job)
        {
            Globals.Instance.Log.LogMessage($"Connection.ExecuteDataDownloadJob: start={job.StartTime} end={job.EndTime}", LogLevel.Info);

            //////////////////
            //TEMPORARY FAKE DATA AND DELAY
            DataInterval interval = job.LoadJob.Interval;
            TimeSpan span = job.EndTime - job.StartTime;

            int size = (int)(span.Ticks / interval.Ticks)+1;

            BarTable bt = BarTable.GenerateRandomData(job.LoadJob.Instrument, interval, job.StartTime, size, _randomSeed++);
            
            Random r = new Random(_randomSeed++);
            await Task.Delay(r.Next(1000, 10000));
            //////////////////

            DataDownloadSaveJob savejob = new DataDownloadSaveJob(bt, job);
            savejob.JobDone += OnDataSaveComplete;

            string key = Connection.DownloadKey(job.LoadJob.Instrument, job.LoadJob.Interval, job.StartTime, job.EndTime);
            _waitingDownloadJobs.GetOrAdd(key, _ => new()).Add((job,savejob));

            Globals.Instance.DataTableManager.DataWarehouse.EnqueueDataSaveJob(savejob);
        }

        private void OnDataSaveComplete(object? sender, DataSaveJobDoneArgs args)
        {
            DataDownloadSaveJob? savejob = args.SourceJob as DataDownloadSaveJob;
            if (savejob == null) return;

            string key = Connection.DownloadKey(savejob.SourceJob.LoadJob.Instrument, savejob.SourceJob.LoadJob.Interval, savejob.SourceJob.StartTime, savejob.SourceJob.EndTime);

            _waitingDownloadJobs.TryRemove(key, out _);

            if (args.HasError) { savejob.SourceJob.ErrorMessage = args.ErrorMessage; }
            
            savejob.SourceJob.FireJobDone();
        }


        private void connectionWorker()
        {
            CancellationToken token = _cts.Token;

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

                            ConnectionState oldState = State;
                            State = ConnectionState.Disconnected;
                            Thread.MemoryBarrier();
                            StateChange?.Invoke(this, new ConnectionStateChangeEventArgs(oldState, State));
                            continue;
                        }
                        else if (State == ConnectionState.Connecting || (State != ConnectionState.Connected && WantConnect && _connectRetryCounter < MaxConnectionRetryCount))
                        {
                            //TODO: perform connect

                            ConnectionState oldState = State;
                            State = ConnectionState.Connected;
                            Thread.MemoryBarrier();
                            StateChange?.Invoke(this, new ConnectionStateChangeEventArgs(oldState, State));
                            continue;
                        }
                        else if (State == ConnectionState.Connected)
                        {
                            Func<CancellationToken, Task> action;

                            if (_dataJobQueue.Count > 0)
                            {
                                action = _dataJobQueue.Take(token);

                                try
                                {
                                    Task task = action(token);
                                    task.Wait(token);
                                }
                                catch (Exception e)
                                {
                                    Globals.Instance.Log.LogMessage("ConnectionWorker task exception:", LogLevel.Error);
                                    Globals.Instance.Log.LogException(e);
                                }
                            }

                            //TODO: do connection stuff...

                            ///////////
                            /// Fake connection for testing
                            _isSleeping = true;
                            Thread.Sleep(5000);
                            _isSleeping = false;

                            Random r = new Random(DateTime.Now.Second);
                            double p = r.Next(20, 100);
                            long v = r.Next(100, 1000);
                            Instrument? i = Globals.Instance.InstrumentCollection.Lookup("Random");
                            if (i == null)
                                Globals.Instance.Log.LogMessage("Connection data stream failed to find Random instrument.", LogLevel.Error);
                            else
                                DataUpdate?.Invoke(this, ConnectionDataUpdateEventArgs.CreateLastArgs(i, DateTime.Now, p, v));
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
            _isShutdown = true;
            _wantExit = true;

            _dataJobQueue.CompleteAdding();

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

        public Connection? GetDataConnection()
        {
            if (_activeConnections.Count == 1)
                return _activeConnections[_activeConnections.Keys.ToList()[0]];
            else if (_knownConnections.Count > 1)
            {
                //return connection marked as primary
                foreach (Connection connection in _activeConnections.Values)
                {
                    if(connection.Properties.IsPrimaryDataSource) return connection;
                }
                
                //if no primary return first connection
                return _activeConnections[_activeConnections.Keys.ToList()[0]];
            }

            return null;
        }

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
                Globals.Instance.DataTableManager.SubscribeToConnection(connection);

            if (connection.State == ConnectionState.Disconnected || connection.State == ConnectionState.Error)
            {
                Globals.Instance.DataTableManager.UnsubscribeFromConnection(connection);
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
