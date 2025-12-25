using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using BepInEx.Logging;
using WoodburySpectatorSync.Config;

namespace WoodburySpectatorSync.Net
{
    public sealed class SpectatorClient
    {
        private readonly ManualLogSource _logger;
        private readonly Settings _settings;
        private readonly ConcurrentQueue<Message> _incoming = new ConcurrentQueue<Message>();
        private readonly object _sendLock = new object();

        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _connectThread;
        private Thread _receiveThread;
        private volatile bool _running;
        private volatile bool _connected;
        private UdpChannel _udpChannel;
        private const int ConnectTimeoutMs = 3000;
        private const int RetryDelayMs = 2000;

        public SpectatorClient(ManualLogSource logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
            Status = "Idle";
        }

        public bool IsConnected => _connected;
        public string Status { get; private set; }

        public void Connect()
        {
            if (_running) return;
            _running = true;
            Status = "Connecting";
            _connectThread = new Thread(ConnectLoop) { IsBackground = true, Name = "WSS-Connect" };
            _connectThread.Start();
        }

        public void Disconnect()
        {
            _running = false;
            _connected = false;
            Status = "Disconnected";
            Cleanup();
        }

        public bool TryDequeue(out Message message)
        {
            if (_incoming.TryDequeue(out message))
            {
                return true;
            }

            return TryDequeueUdp(out message);
        }

        private void ConnectLoop()
        {
            while (_running)
            {
                try
                {
                    Status = "Connecting";
                    _client = new TcpClient();
                    _client.NoDelay = true;
                    ConnectWithTimeout(_client, _settings.SpectatorHostIP.Value, _settings.HostPort.Value, ConnectTimeoutMs);
                    _stream = _client.GetStream();
                    _connected = true;
                    Status = "Connected";
                    _logger.LogInfo("Connected to host");

                    StartUdp();

                    _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "WSS-Receive" };
                    _receiveThread.Start();
                    _receiveThread.Join();
                }
                catch (Exception ex)
                {
                    Status = "Retrying";
                    _logger.LogWarning("Connect failed: " + ex.Message);
                }
                finally
                {
                    _connected = false;
                    Cleanup();
                }

                if (_running)
                {
                    Thread.Sleep(RetryDelayMs);
                }
            }
        }

        private void ReceiveLoop()
        {
            try
            {
                var lengthBuffer = new byte[4];
                while (_running && _client != null && _client.Connected)
                {
                    var readResult = TcpFraming.TryReadFrame(_stream, lengthBuffer, out var payload);
                    if (readResult == FrameReadResult.Disconnected) break;
                    if (readResult == FrameReadResult.BadFrame)
                    {
                        Status = "Bad frame";
                        break;
                    }

                    if (Protocol.TryParsePayload(payload, out var message, out var error))
                    {
                        if (message is PingMessage)
                        {
                            SendFrame(Protocol.BuildFrame(Protocol.BuildPong()));
                        }
                        else if (message is UdpInfoMessage udpInfo)
                        {
                            ConfigureUdp(udpInfo.Port);
                            SendUdpPing();
                        }
                        else
                        {
                            _incoming.Enqueue(message);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Protocol error: " + error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Receive loop error: " + ex.Message);
            }
            finally
            {
                _connected = false;
                Status = "Disconnected";
                Cleanup();
            }
        }

        private void SendFrame(byte[] frame)
        {
            if (frame == null || frame.Length == 0) return;
            lock (_sendLock)
            {
                try
                {
                    _stream?.Write(frame, 0, frame.Length);
                }
                catch { }
            }
        }

        private void Cleanup()
        {
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null;
            _client = null;
            _udpChannel?.Stop();
            _udpChannel = null;
        }

        private void StartUdp()
        {
            if (!_settings.UdpEnabled.Value || _udpChannel != null) return;

            try
            {
                _udpChannel = new UdpChannel(_logger, 0);
                ConfigureUdp(_settings.UdpPort.Value);
                SendUdpPing();
            }
            catch (Exception ex)
            {
                _udpChannel = null;
                _logger.LogWarning("UDP setup failed: " + ex.Message);
            }
        }

        private void ConfigureUdp(int port)
        {
            if (_udpChannel == null) return;
            if (port <= 0) return;
            _udpChannel.SetRemote(_settings.SpectatorHostIP.Value, port);
        }

        private void SendUdpPing()
        {
            if (_udpChannel == null) return;
            if (!_udpChannel.HasRemoteEndpoint) return;
            _udpChannel.Send(Protocol.BuildPing());
        }

        private bool TryDequeueUdp(out Message message)
        {
            message = null;
            if (_udpChannel == null) return false;

            while (_udpChannel.TryDequeue(out message))
            {
                if (message is PingMessage || message is PongMessage || message is UdpInfoMessage)
                {
                    continue;
                }

                return true;
            }

            message = null;
            return false;
        }

        private static void ConnectWithTimeout(TcpClient client, string host, int port, int timeoutMs)
        {
            var result = client.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(timeoutMs);
            if (!success)
            {
                client.Close();
                throw new TimeoutException("Connect timed out.");
            }
            client.EndConnect(result);
        }
    }
}
