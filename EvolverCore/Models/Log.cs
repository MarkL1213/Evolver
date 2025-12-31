using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        Queue<LogMessage> _messages = new Queue<LogMessage>();
        volatile bool _wantExit = false;
        volatile bool _isSleeping = false;
        bool _disposedValue;
        Thread _logThread;
        StreamWriter? _logStream;


        public Log()
        {
            _logThread = new Thread(logWorker);
            _logThread.Name = "Evolver Logging";
            _logThread.Start();
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

                    if(ex.InnerException != null)
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
            //Console.WriteLine($"Thread '{Thread.CurrentThread.Name}' about to sleep indefinitely.");
            while (true)
            {
                try
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
                        try
                        {
                            if (_logStream == null)
                            {
                                _logStream = new StreamWriter(Globals.Instance.LogFileName, true);
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
                    }
                }
                catch (ThreadInterruptedException)
                {
                    _isSleeping = false;
                    //Console.WriteLine($"Thread '{Thread.CurrentThread.Name}' awoken.");
                }
                catch (ThreadAbortException)
                {
                    break;
                }
            }

            if (_logStream != null)
            {
                _logStream.Close();
                _logStream.Dispose();
                _logStream = null;
            }
        }

        public void Shutdown()
        {
            _wantExit = true;
            if (_isSleeping) _logThread.Interrupt();

            if (_logStream != null)
            {
                _logStream.Close();
                _logStream.Dispose();
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
                    _wantExit = true;
                    if (_isSleeping) { _logThread.Interrupt(); }
                    _logThread.Join();

                    if (_logStream != null)
                    {
                        _logStream.Close();
                        _logStream.Dispose();
                        _logStream = null;
                    }
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
