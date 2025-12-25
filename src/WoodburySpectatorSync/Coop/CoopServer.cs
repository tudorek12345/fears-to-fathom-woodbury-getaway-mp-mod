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
        private readonly ConcurrentQueue<OutboundFrame> _outgoing = new ConcurrentQueue<OutboundFrame>();
        private readonly ConcurrentQueue<Message> _incoming = new ConcurrentQueue<Message>();
        private readonly object _clientLock = new object();

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _acceptThread;
        private Thread _sendThread;
        private Thread _receiveThread;
        private volatile bool _running;
        private volatile bool _clientConnected;
        private UdpChannel _udpChannel;
        private long _lastTcpReceiveMs;
        private readonly object _hostTransformLock = new object();
        private PlayerTransformState _lastHostTransform;
        private bool _hasHostTransform;
        private long _lastTransformSentTcpMs;
        private long _lastTransformSentUdpMs;
        private int _transformSendCount;
        private long _hostStateEnqueued;
        private long _hostStateSent;
        private long _lastHostStateSentMs;
        private int _lastHostStateType;

        private struct OutboundFrame
        {
            public byte[] Frame;
            public bool IsHostState;
            public MessageType Type;
        }

        public CoopServer(ManualLogSource logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public bool IsRunning => _running;
        public bool IsClientConnected => _clientConnected;
        public bool HasUdp => _udpChannel != null && _udpChannel.HasRemoteEndpoint;
        public long LastTcpReceiveMs => Interlocked.Read(ref _lastTcpReceiveMs);
        public long UdpLastReceiveMs => _udpChannel != null ? _udpChannel.LastReceiveUnixMs : 0;
        public long LastTransformSentTcpMs => Interlocked.Read(ref _lastTransformSentTcpMs);
        public long LastTransformSentUdpMs => Interlocked.Read(ref _lastTransformSentUdpMs);
        public int TransformSendCount => Interlocked.CompareExchange(ref _transformSendCount, 0, 0);
        public long HostStateEnqueued => Interlocked.Read(ref _hostStateEnqueued);
        public long HostStateSent => Interlocked.Read(ref _hostStateSent);
        public long LastHostStateSentMs => Interlocked.Read(ref _lastHostStateSentMs);
        public MessageType LastHostStateType => (MessageType)Interlocked.CompareExchange(ref _lastHostStateType, 0, 0);

        public void UpdateHostTransform(PlayerTransformState state)
        {
            lock (_hostTransformLock)
            {
                _lastHostTransform = state;
                _hasHostTransform = true;
            }
        }

        public void PublishHostTransform(PlayerTransformState state)
        {
            UpdateHostTransform(state);
        }

        public void Start()
        {
            if (_running) return;

            _running = true;
            _clientConnected = false;
            var bindIp = ParseBindIp(_settings.HostBindIP.Value);
            _listener = new TcpListener(bindIp, _settings.HostPort.Value);
            _listener.Start();

            if (_settings.UdpEnabled.Value)
            {
                try
                {
                    _udpChannel = new UdpChannel(_logger, _settings.UdpPort.Value, bindIp);
                }
                catch (Exception ex)
                {
                    _udpChannel = null;
                    _logger.LogWarning("Co-op UDP disabled: " + ex.Message);
                }
            }

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
            _udpChannel?.Stop();
            _udpChannel = null;
            DisconnectClient();
            _logger.LogInfo("Co-op host server stopped");
        }

        public void Enqueue(Message message)
        {
            if (message == null) return;
            if (message.Type == MessageType.PlayerTransform)
            {
                PublishHostTransform(((PlayerTransformMessage)message).State);
                return;
            }
            var payload = BuildPayload(message);
            if (payload == null) return;
            var isHostState = IsHostStateType(message.Type);
            if (isHostState)
            {
                Interlocked.Increment(ref _hostStateEnqueued);
            }
            _outgoing.Enqueue(new OutboundFrame
            {
                Frame = Protocol.BuildFrame(payload),
                IsHostState = isHostState,
                Type = message.Type
            });
        }

        public bool TryDequeueIncoming(out Message message)
        {
            if (_incoming.TryDequeue(out message))
            {
                return true;
            }

            return TryDequeueUdp(out message);
        }

        public void SendUdp(Message message)
        {
            if (_udpChannel == null || !_udpChannel.HasRemoteEndpoint) return;
            if (message == null) return;
            var payload = BuildPayload(message);
            if (payload == null) return;
            _udpChannel.Send(payload);
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    if (_clientConnected)
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
                        _clientConnected = true;
                    }

                    if (_udpChannel != null)
                    {
                        _udpChannel.ClearRemote();
                        _outgoing.Enqueue(new OutboundFrame
                        {
                            Frame = Protocol.BuildFrame(Protocol.BuildUdpInfo(_settings.UdpPort.Value)),
                            IsHostState = false,
                            Type = MessageType.UdpInfo
                        });
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
                    if (!_clientConnected)
                    {
                        Thread.Sleep(50);
                        continue;
                    }

                    var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    MaybeSendHostTransform(nowMs);

                    while (_outgoing.TryDequeue(out var outbound))
                    {
                        SendFrame(outbound.Frame);
                        if (outbound.IsHostState)
                        {
                            Interlocked.Increment(ref _hostStateSent);
                            Interlocked.Exchange(ref _lastHostStateSentMs, nowMs);
                            Interlocked.Exchange(ref _lastHostStateType, (int)outbound.Type);
                        }
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

        private void MaybeSendHostTransform(long nowMs)
        {
            if (!_hasHostTransform) return;

            var intervalMs = (int)Math.Max(1, 1000.0 / Math.Max(1, _settings.SendHz.Value));
            if (nowMs - Interlocked.Read(ref _lastTransformSentTcpMs) < intervalMs) return;

            PlayerTransformState state;
            lock (_hostTransformLock)
            {
                if (!_hasHostTransform) return;
                state = _lastHostTransform;
            }

            var payload = Protocol.BuildPlayerTransform(state);
            if (payload == null) return;

            if (_settings.UdpEnabled.Value && _udpChannel != null && _udpChannel.HasRemoteEndpoint)
            {
                _udpChannel.Send(payload);
                Interlocked.Exchange(ref _lastTransformSentUdpMs, nowMs);
            }

            var frame = Protocol.BuildFrame(payload);
            SendFrame(frame);
            Interlocked.Exchange(ref _lastTransformSentTcpMs, nowMs);
            Interlocked.Increment(ref _transformSendCount);
        }

        private void ReceiveLoop()
        {
            try
            {
                var lengthBuffer = new byte[4];
                while (_running && _clientConnected)
                {
                    NetworkStream stream;
                    lock (_clientLock)
                    {
                        stream = _stream;
                    }
                    if (stream == null) break;

                    var readResult = TcpFraming.TryReadFrame(stream, lengthBuffer, out var payload);
                    if (readResult == FrameReadResult.Disconnected) break;
                    if (readResult == FrameReadResult.BadFrame)
                    {
                        _logger.LogWarning("Co-op receive bad frame length");
                        break;
                    }

                    if (Protocol.TryParsePayload(payload, out var message, out var error))
                    {
                        Interlocked.Exchange(ref _lastTcpReceiveMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                        _incoming.Enqueue(message);
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
                _clientConnected = false;
            }
            Interlocked.Exchange(ref _lastTcpReceiveMs, 0);
            Interlocked.Exchange(ref _lastTransformSentTcpMs, 0);
            Interlocked.Exchange(ref _lastTransformSentUdpMs, 0);
            Interlocked.Exchange(ref _transformSendCount, 0);
            Interlocked.Exchange(ref _hostStateEnqueued, 0);
            Interlocked.Exchange(ref _hostStateSent, 0);
            Interlocked.Exchange(ref _lastHostStateSentMs, 0);
            Interlocked.Exchange(ref _lastHostStateType, 0);
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

        public bool TryGetHostTransform(out PlayerTransformState state)
        {
            lock (_hostTransformLock)
            {
                state = _lastHostTransform;
                return _hasHostTransform;
            }
        }

        private static IPAddress ParseBindIp(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip)) return IPAddress.Any;
            if (IPAddress.TryParse(ip, out var address)) return address;
            return IPAddress.Any;
        }

        private static bool IsHostStateType(MessageType type)
        {
            switch (type)
            {
                case MessageType.SceneChange:
                case MessageType.DoorState:
                case MessageType.HoldableState:
                case MessageType.StoryFlag:
                case MessageType.AiTransform:
                case MessageType.DialogueLine:
                case MessageType.DialogueStart:
                case MessageType.DialogueAdvance:
                case MessageType.DialogueChoice:
                case MessageType.DialogueEnd:
                    return true;
                default:
                    return false;
            }
        }

        private static byte[] BuildPayload(Message message)
        {
            switch (message.Type)
            {
                case MessageType.SceneChange:
                {
                    var msg = (SceneChangeMessage)message;
                    return Protocol.BuildSceneChange(msg.SceneName, msg.BuildIndex, msg.StartSequence, msg.FromMenu);
                }
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
                case MessageType.UdpInfo:
                    return Protocol.BuildUdpInfo(((UdpInfoMessage)message).Port);
                case MessageType.SceneReady:
                    return Protocol.BuildSceneReady(((SceneReadyMessage)message).SceneName);
                case MessageType.DialogueLine:
                {
                    var msg = (DialogueLineMessage)message;
                    return Protocol.BuildDialogueLine(msg.Speaker, msg.Text, msg.Duration, msg.Kind);
                }
                case MessageType.DialogueStart:
                {
                    var msg = (DialogueStartMessage)message;
                    return Protocol.BuildDialogueStart(msg.ConversationId, msg.EntryId);
                }
                case MessageType.DialogueAdvance:
                {
                    var msg = (DialogueAdvanceMessage)message;
                    return Protocol.BuildDialogueAdvance(msg.ConversationId, msg.EntryId);
                }
                case MessageType.DialogueChoice:
                {
                    var msg = (DialogueChoiceMessage)message;
                    return Protocol.BuildDialogueChoice(msg.ConversationId, msg.EntryId, msg.ChoiceIndex);
                }
                case MessageType.DialogueEnd:
                {
                    var msg = (DialogueEndMessage)message;
                    return Protocol.BuildDialogueEnd(msg.ConversationId);
                }
                case MessageType.Ping:
                    return Protocol.BuildPing();
                case MessageType.Pong:
                {
                    var pong = (PongMessage)message;
                    return pong.HasTransform ? Protocol.BuildPong(pong.Transform) : Protocol.BuildPong();
                }
                default:
                    return null;
            }
        }
    }
}
