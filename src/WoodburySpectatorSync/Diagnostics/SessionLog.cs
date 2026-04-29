using System;
using System.Diagnostics;
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

        public SessionLog(ManualLogSource logger, string label = null)
        {
            _logger = logger;
            InitializeWriter(label);
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

        private void InitializeWriter(string label)
        {
            try
            {
                var logDir = System.IO.Path.Combine(Paths.BepInExRootPath, "logs");
                Directory.CreateDirectory(logDir);

                var safeLabel = MakeSafeFilePart(label);
                var labelPart = string.IsNullOrEmpty(safeLabel) ? string.Empty : "_" + safeLabel;
                var pid = Process.GetCurrentProcess().Id;
                var baseName = "WoodburySpectatorSync_session_" +
                               DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") +
                               labelPart +
                               "_pid" + pid;

                for (var attempt = 0; attempt < 100; attempt++)
                {
                    var suffix = attempt == 0 ? string.Empty : "_" + attempt;
                    var candidate = System.IO.Path.Combine(logDir, baseName + suffix + ".log");
                    try
                    {
                        var stream = new FileStream(candidate, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                        _path = candidate;
                        _writer = new StreamWriter(stream) { AutoFlush = true };
                        break;
                    }
                    catch (IOException)
                    {
                        if (attempt == 99)
                        {
                            throw;
                        }
                    }
                }

                _logger?.LogInfo("Session log: " + _path);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Session log setup failed: " + ex.Message);
                _writer = null;
            }
        }

        private static string MakeSafeFilePart(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var chars = value.Trim().ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0 || char.IsWhiteSpace(chars[i]))
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }
    }
}
