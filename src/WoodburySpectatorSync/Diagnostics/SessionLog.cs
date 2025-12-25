using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;

namespace WoodburySpectatorSync.Diagnostics
{
    public sealed class SessionLog : IDisposable
    {
        private readonly ManualLogSource _logger;
        private readonly object _lock = new object();
        private StreamWriter _writer;
        private string _path;

        public SessionLog(ManualLogSource logger)
        {
            _logger = logger;
            InitializeWriter();
            Write("Session start");
        }

        public string Path => _path;

        public void Write(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            lock (_lock)
            {
                if (_writer == null) return;
                var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var lines = message.Replace("\r\n", "\n").Split('\n');
                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    _writer.WriteLine(stamp + " " + line);
                }
            }
        }

        public void WriteException(string context, Exception ex)
        {
            if (ex == null) return;
            Write(context + ": " + ex);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_writer != null)
                {
                    _writer.Flush();
                    _writer.Dispose();
                    _writer = null;
                }
            }
        }

        private void InitializeWriter()
        {
            try
            {
                var logDir = System.IO.Path.Combine(Paths.BepInExRootPath, "logs");
                Directory.CreateDirectory(logDir);
                _path = System.IO.Path.Combine(logDir, "WoodburySpectatorSync_session_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log");
                _writer = new StreamWriter(_path) { AutoFlush = true };
                _logger?.LogInfo("Session log: " + _path);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Session log setup failed: " + ex.Message);
                _writer = null;
            }
        }
    }
}
