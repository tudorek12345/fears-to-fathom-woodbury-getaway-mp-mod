using System;
using System.Collections.Concurrent;
using System.IO;
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
                _logger.LogInfo("Connected to host");

                _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "WSS-Receive" };
                _receiveThread.Start();
            }
            catch (Exception ex)
            {
                Status = "Connection failed";
                _logger.LogWarning("Connect failed: " + ex.Message);
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
                        Status = "Bad frame";
                        break;
                    }

                    var payload = new byte[payloadLength];
                    if (!ReadExact(_stream, payload, payloadLength)) break;

                    if (Protocol.TryParsePayload(payload, out var message, out var error))
                    {
                        if (message is PingMessage)
                        {
                            SendFrame(Protocol.BuildFrame(Protocol.BuildPong()));
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
    }
}
