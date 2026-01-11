using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace EvolverCore.Models
{
    public enum LogLevel
    {
        Info,
        Warn,
        Error
    }

    internal class LogMessage
    {
        public LogMessage() { Level = LogLevel.Info; Message = string.Empty; }
        public LogMessage(string message, LogLevel level) { Level = level; Message = message; }
        public LogLevel Level { get; private set; }
        public string Message { get; private set; }

        public override string ToString()
        {
            return $"{DateTime.Now} [{Level}]: {Message}";
        }
    }

    public class Log : IDisposable
    {
        object _queueLock = new object();
        object _logControlsLock = new object();
        Queue<LogMessage> _messages = new Queue<LogMessage>();
        volatile bool _wantExit = false;
        volatile bool _isSleeping = false;
        bool _disposedValue;
        Thread _logThread;
        StreamWriter? _logStream;
        List<LogControl> _logControls = new List<LogControl>();


        public Log()
        {
            _logThread = new Thread(logWorker);
            _logThread.Name = "Evolver Logging";
            _logThread.Start();
        }

        internal void RegisterLogControl(LogControl control)
        {
            lock (_logControlsLock)
            {
                _logControls.Add(control);
            }
        }

        internal void UnRegisterLogControl(LogControl control)
        {
            lock (_logControlsLock)
            {
                _logControls.Remove(control);
            }
        }



        public void LogMessage(string message, LogLevel level)
        {
            lock (_queueLock)
            {
                _messages.Enqueue(new LogMessage(message, level));
            }
            if (_isSleeping) { _logThread.Interrupt(); }
        }

        public void LogException(Exception e)
        {
            Exception ex = e;
            string indent = "";
            lock (_queueLock)
            {
                while (true)
                {
                    Type t = ex.GetType();
                    _messages.Enqueue(new LogMessage(indent + t.Name + ": " + ex.Message, LogLevel.Error));
                    if (!string.IsNullOrEmpty(ex.StackTrace)) _messages.Enqueue(new LogMessage(indent + ex.StackTrace, LogLevel.Error));

                    if (ex.InnerException != null)
                    {
                        indent += "    ";
                        ex = ex.InnerException;
                        continue;
                    }

                    break;
                }
            }
        }

        private void logWorker()
        {
            try
            {
                while (true)
                {
                    int queueCount = 0;
                    LogMessage message = new LogMessage();

                    lock (_queueLock)
                    {
                        queueCount = _messages.Count;
                        if (queueCount > 0)
                            message = _messages.Dequeue();
                    }

                    if (queueCount == 0)
                    {
                        if (_wantExit) break;

                        _isSleeping = true;
                        Thread.Sleep(Timeout.Infinite);
                    }
                    else
                    {
                        #region Log to File
                        try
                        {
                            if (_logStream == null)
                            {
                                FileStreamOptions options = new FileStreamOptions();
                                options.Share = FileShare.ReadWrite;
                                options.Mode = FileMode.Append;
                                options.Access = FileAccess.Write;

                                _logStream = new StreamWriter(Globals.Instance.LogFileName, options);
                            }
                        }
                        catch (ThreadAbortException) { break; }
                        catch (Exception e)
                        {
                            throw new EvolverException($"Logger failed to log message : '{message}'", e);
                        }

                        _logStream.WriteLine(message.ToString());

                        try
                        {
                            _logStream.Flush();
                        }
                        catch (ThreadAbortException) { break; }
                        catch (Exception e)
                        {
                            throw new EvolverException($"Logger failed.", e);
                        }
                        #endregion

                        #region Log to Control
                        lock (_logControlsLock)
                        {
                            foreach (LogControl control in _logControls)
                            {
                                control.AppendText(message.ToString());
                            }
                        }
                        #endregion
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
                _isSleeping = false;
            }
            catch (ThreadAbortException)
            {

            }

            if (_logStream != null)
            {
                _logStream?.Close();
                _logStream?.Dispose();
                _logStream = null;
            }
        }

        public void Shutdown()
        {
            _wantExit = true;
            if (_isSleeping && _logThread.IsAlive) _logThread.Interrupt();

            if (_logThread.IsAlive)
            {
                _logThread.Join(TimeSpan.FromSeconds(5));
            }

            if (_logStream != null)
            {
                _logStream?.Close();
                _logStream?.Dispose();
                _logStream = null;
            }
        }

        #region IDispose
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    Shutdown();
                }

                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Log()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }

}
