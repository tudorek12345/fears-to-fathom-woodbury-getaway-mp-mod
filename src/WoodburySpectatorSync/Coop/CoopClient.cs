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

        public CoopClient(ManualLogSource logger, Settings settings)
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
            return _incoming.TryDequeue(out message);
        }

        private void ConnectLoop()
        {
            try
            {
                _client = new TcpClient();
                _client.NoDelay = true;
                _client.Connect(_settings.SpectatorHostIP.Value, _settings.HostPort.Value);
                _stream = _client.GetStream();
                _connected = true;
                Status = "Connected";
                _logger.LogInfo("Co-op connected to host");

                _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "WSS-CoopReceive" };
                _sendThread = new Thread(SendLoop) { IsBackground = true, Name = "WSS-CoopSend" };
                _receiveThread.Start();
                _sendThread.Start();
            }
            catch (Exception ex)
            {
                Status = "Connection failed";
                _logger.LogWarning("Co-op connect failed: " + ex.Message);
                _running = false;
                Cleanup();
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
                default:
                    return null;
            }
        }
    }
}
