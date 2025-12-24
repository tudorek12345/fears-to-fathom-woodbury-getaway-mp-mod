using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BepInEx.Logging;
using WoodburySpectatorSync.Config;
using WoodburySpectatorSync.Net;

namespace WoodburySpectatorSync.Coop
{
    public sealed class CoopServer
    {
        private readonly ManualLogSource _logger;
        private readonly Settings _settings;
        private readonly ConcurrentQueue<byte[]> _outgoing = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<Message> _incoming = new ConcurrentQueue<Message>();
        private readonly object _clientLock = new object();

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _acceptThread;
        private Thread _sendThread;
        private Thread _receiveThread;
        private volatile bool _running;

        public CoopServer(ManualLogSource logger, Settings settings)
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

            _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "WSS-CoopAccept" };
            _sendThread = new Thread(SendLoop) { IsBackground = true, Name = "WSS-CoopSend" };
            _acceptThread.Start();
            _sendThread.Start();

            _logger.LogInfo("Co-op host server started");
        }

        public void Stop()
        {
            if (!_running) return;

            _running = false;
            try { _listener?.Stop(); } catch { }
            DisconnectClient();
            _logger.LogInfo("Co-op host server stopped");
        }

        public void Enqueue(Message message)
        {
            if (message == null) return;
            var payload = BuildPayload(message);
            if (payload == null) return;
            _outgoing.Enqueue(Protocol.BuildFrame(payload));
        }

        public bool TryDequeueIncoming(out Message message)
        {
            return _incoming.TryDequeue(out message);
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

                    _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "WSS-CoopReceive" };
                    _receiveThread.Start();

                    _logger.LogInfo("Co-op client connected");
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
                    _logger.LogWarning("Co-op accept loop error: " + ex.Message);
                    Thread.Sleep(200);
                }
            }
        }

        private void SendLoop()
        {
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

                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Co-op send loop error: " + ex.Message);
                    DisconnectClient();
                    Thread.Sleep(200);
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
                            _outgoing.Enqueue(Protocol.BuildFrame(Protocol.BuildPong()));
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
                _logger.LogWarning("Co-op receive loop error: " + ex.Message);
            }
            finally
            {
                DisconnectClient();
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

        private static IPAddress ParseBindIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return IPAddress.Any;
            if (IPAddress.TryParse(ip, out var address)) return address;
            return IPAddress.Any;
        }

        private static byte[] BuildPayload(Message message)
        {
            switch (message.Type)
            {
                case MessageType.SceneChange:
                    return Protocol.BuildSceneChange(((SceneChangeMessage)message).SceneName);
                case MessageType.PlayerTransform:
                    return Protocol.BuildPlayerTransform(((PlayerTransformMessage)message).State);
                case MessageType.InteractRequest:
                {
                    var msg = (InteractRequestMessage)message;
                    return Protocol.BuildInteractRequest(msg.PlayerId, msg.TargetPath, msg.ActionType);
                }
                case MessageType.DoorState:
                    return Protocol.BuildDoorState(((DoorStateMessage)message).State);
                case MessageType.HoldableState:
                    return Protocol.BuildHoldableState(((HoldableStateMessage)message).State);
                case MessageType.StoryFlag:
                {
                    var msg = (StoryFlagMessage)message;
                    return Protocol.BuildStoryFlag(msg.Key, msg.Value);
                }
                case MessageType.AiTransform:
                    return Protocol.BuildAiTransform(((AiTransformMessage)message).State);
                default:
                    return null;
            }
        }
    }
}
