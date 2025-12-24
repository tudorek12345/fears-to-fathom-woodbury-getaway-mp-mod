using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BepInEx.Logging;
using WoodburySpectatorSync.Config;

namespace WoodburySpectatorSync.Net
{
    public sealed class HostServer
    {
        private readonly ManualLogSource _logger;
        private readonly Settings _settings;
        private readonly ConcurrentQueue<byte[]> _outgoing = new ConcurrentQueue<byte[]>();
        private readonly object _clientLock = new object();

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _acceptThread;
        private Thread _sendThread;
        private volatile bool _running;

        private volatile bool _hasCamera;
        private CameraState _latestCamera;
        private string _lastSceneName = string.Empty;
        private string _lastProgressMarker = string.Empty;

        public HostServer(ManualLogSource logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public bool IsRunning => _running;
        public bool IsClientConnected => _client != null && _client.Connected;

        public void Start()
        {
            if (_running) return;

            _running = true;
            var bindIp = ParseBindIp(_settings.HostBindIP.Value);
            _listener = new TcpListener(bindIp, _settings.HostPort.Value);
            _listener.Start();

            _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "WSS-Accept" };
            _sendThread = new Thread(SendLoop) { IsBackground = true, Name = "WSS-Send" };
            _acceptThread.Start();
            _sendThread.Start();

            _logger.LogInfo("Host server started");
        }

        public void Stop()
        {
            if (!_running) return;

            _running = false;
            try { _listener?.Stop(); } catch { }
            DisconnectClient();
            _logger.LogInfo("Host server stopped");
        }

        public void SetLatestCamera(CameraState state)
        {
            _latestCamera = state;
            _hasCamera = true;
        }

        public void QueueSceneChange(string sceneName)
        {
            _lastSceneName = sceneName ?? string.Empty;
            _outgoing.Enqueue(Protocol.BuildFrame(Protocol.BuildSceneChange(_lastSceneName)));
        }

        public void QueueProgressMarker(string marker)
        {
            _lastProgressMarker = marker ?? string.Empty;
            _outgoing.Enqueue(Protocol.BuildFrame(Protocol.BuildProgressMarker(_lastProgressMarker)));
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    if (_client != null && _client.Connected)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    var client = _listener.AcceptTcpClient();
                    client.NoDelay = true;

                    lock (_clientLock)
                    {
                        _client = client;
                        _stream = client.GetStream();
                    }

                    _logger.LogInfo("Spectator connected");

                    if (!string.IsNullOrEmpty(_lastSceneName))
                    {
                        _outgoing.Enqueue(Protocol.BuildFrame(Protocol.BuildSceneChange(_lastSceneName)));
                    }

                    if (!string.IsNullOrEmpty(_lastProgressMarker))
                    {
                        _outgoing.Enqueue(Protocol.BuildFrame(Protocol.BuildProgressMarker(_lastProgressMarker)));
                    }

                    if (_hasCamera)
                    {
                        _outgoing.Enqueue(Protocol.BuildFrame(Protocol.BuildCameraState(_latestCamera)));
                    }
                }
                catch (SocketException)
                {
                    if (_running) Thread.Sleep(100);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Host accept loop error: " + ex.Message);
                    Thread.Sleep(200);
                }
            }
        }

        private void SendLoop()
        {
            var stopwatch = Stopwatch.StartNew();
            long nextSendMs = 0;

            while (_running)
            {
                try
                {
                    if (_client == null || !_client.Connected)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    while (_outgoing.TryDequeue(out var frame))
                    {
                        SendFrame(frame);
                    }

                    var sendHz = Math.Max(1, _settings.SendHz.Value);
                    var intervalMs = (long)Math.Max(1, 1000.0 / sendHz);

                    if (_hasCamera && stopwatch.ElapsedMilliseconds >= nextSendMs)
                    {
                        var payload = Protocol.BuildCameraState(_latestCamera);
                        SendFrame(Protocol.BuildFrame(payload));
                        nextSendMs = stopwatch.ElapsedMilliseconds + intervalMs;
                    }

                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Host send loop error: " + ex.Message);
                    DisconnectClient();
                    Thread.Sleep(200);
                }
            }
        }

        private void SendFrame(byte[] frame)
        {
            if (frame == null || frame.Length == 0) return;

            lock (_clientLock)
            {
                if (_stream == null) return;
                _stream.Write(frame, 0, frame.Length);
            }
        }

        private void DisconnectClient()
        {
            lock (_clientLock)
            {
                try { _stream?.Close(); } catch { }
                try { _client?.Close(); } catch { }
                _stream = null;
                _client = null;
            }
        }

        private static IPAddress ParseBindIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return IPAddress.Any;
            if (IPAddress.TryParse(ip, out var address)) return address;
            return IPAddress.Any;
        }
    }
}
