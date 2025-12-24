using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using BepInEx.Logging;
using WoodburySpectatorSync.Config;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    public sealed class CoopClient
    {
        private readonly ManualLogSource _logger;
        private readonly Settings _settings;
        private readonly ConcurrentQueue<Message> _incoming = new ConcurrentQueue<Message>();
        private readonly ConcurrentQueue<byte[]> _outgoing = new ConcurrentQueue<byte[]>();
        private readonly object _sendLock = new object();

        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _connectThread;
        private Thread _receiveThread;
        private Thread _sendThread;
        private volatile bool _running;
        private volatile bool _connected;
        private UdpChannel _udpChannel;
        private const int ConnectTimeoutMs = 3000;
        private const int RetryDelayMs = 2000;

        public CoopClient(ManualLogSource logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
            Status = "Idle";
        }

        public bool IsConnected => _connected;
        public string Status { get; private set; }
        public bool HasUdp => _udpChannel != null && _udpChannel.HasRemoteEndpoint;

        public void Connect()
        {
            if (_running) return;
            _running = true;
            Status = "Connecting";
            _connectThread = new Thread(ConnectLoop) { IsBackground = true, Name = "WSS-CoopConnect" };
            _connectThread.Start();
        }

        public void Disconnect()
        {
            _running = false;
            _connected = false;
            Status = "Disconnected";
            Cleanup();
        }

        public void Enqueue(Message message)
        {
            if (message == null) return;
            var payload = BuildPayload(message);
            if (payload == null) return;
            _outgoing.Enqueue(Protocol.BuildFrame(payload));
        }

        public bool TryDequeue(out Message message)
        {
            if (TryDequeueUdp(out message))
            {
                return true;
            }

            return _incoming.TryDequeue(out message);
        }

        public void SendUdp(Message message)
        {
            if (_udpChannel == null || !_udpChannel.HasRemoteEndpoint) return;
            if (message == null) return;
            var payload = BuildPayload(message);
            if (payload == null) return;
            _udpChannel.Send(payload);
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
                    _logger.LogInfo("Co-op connected to host");

                    StartUdp();

                    _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "WSS-CoopReceive" };
                    _sendThread = new Thread(SendLoop) { IsBackground = true, Name = "WSS-CoopSend" };
                    _receiveThread.Start();
                    _sendThread.Start();

                    _receiveThread.Join();
                }
                catch (Exception ex)
                {
                    Status = "Retrying";
                    _logger.LogWarning("Co-op connect failed: " + ex.Message);
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
                    if (!ReadExact(_stream, lengthBuffer, 4)) break;
                    var payloadLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (payloadLength <= 0 || payloadLength > Protocol.MaxPayloadBytes)
                    {
                        break;
                    }

                    var payload = new byte[payloadLength];
                    if (!ReadExact(_stream, payload, payloadLength)) break;

                    if (Protocol.TryParsePayload(payload, out var message, out var error))
                    {
                        if (message is PingMessage)
                        {
                            Enqueue(new PongMessage());
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
                        _logger.LogWarning("Co-op protocol error: " + error);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Co-op receive error: " + ex.Message);
            }
            finally
            {
                _connected = false;
                Status = "Disconnected";
                Cleanup();
            }
        }

        private void SendLoop()
        {
            try
            {
                while (_running && _client != null && _client.Connected)
                {
                    while (_outgoing.TryDequeue(out var frame))
                    {
                        SendFrame(frame);
                    }

                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Co-op send error: " + ex.Message);
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

        private static bool ReadExact(NetworkStream stream, byte[] buffer, int count)
        {
            var offset = 0;
            while (offset < count)
            {
                var read = stream.Read(buffer, offset, count - offset);
                if (read <= 0) return false;
                offset += read;
            }

            return true;
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

        private static byte[] BuildPayload(Message message)
        {
            switch (message.Type)
            {
                case MessageType.PlayerTransform:
                    return Protocol.BuildPlayerTransform(((PlayerTransformMessage)message).State);
                case MessageType.InteractRequest:
                {
                    var msg = (InteractRequestMessage)message;
                    return Protocol.BuildInteractRequest(msg.PlayerId, msg.TargetPath, msg.ActionType);
                }
                case MessageType.Pong:
                    return Protocol.BuildPong();
                case MessageType.PlayerInput:
                    return Protocol.BuildPlayerInput(((PlayerInputMessage)message).State);
                case MessageType.UdpInfo:
                    return Protocol.BuildUdpInfo(((UdpInfoMessage)message).Port);
                default:
                    return null;
            }
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
                _logger.LogWarning("Co-op UDP setup failed: " + ex.Message);
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
    }
}
