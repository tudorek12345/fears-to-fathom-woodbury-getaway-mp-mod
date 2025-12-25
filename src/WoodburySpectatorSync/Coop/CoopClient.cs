using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
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
        private readonly ConcurrentQueue<Message> _incomingPriority = new ConcurrentQueue<Message>();
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
        private long _lastTcpReceiveMs;
        private long _lastPingSentMs;
        private long _lastPingRttMs;
        private long _lastTcpTransformMs;
        private long _lastUdpTransformMs;
        private long _lastHostTransformReceivedMs;
        private long _lastHostTransformReceivedTick;
        private int _tcpTransformCount;
        private int _udpTransformCount;
        private string _lastTransformSource = "n/a";
        private long _hostStateReadCount;
        private long _hostStateEnqueuedCount;
        private int _pendingTransformMessages;
        private int _udpDrainBudget;
        private int _udpDrainBudgetTick;
        private readonly object _latestHostTransformLock = new object();
        private PlayerTransformState _latestHostTransform;
        private int _latestHostTransformSeq;
        private int _lastConsumedHostTransformSeq;
        private const int ConnectTimeoutMs = 3000;
        private const int RetryDelayMs = 2000;
        private const int MaxUdpDrainPerCall = 64;
        private const int MaxPendingTransforms = 120;
        private const int MaxUdpDrainPerFrame = 128;
        private const int UdpDrainWindowMs = 16;

        public CoopClient(ManualLogSource logger, Settings settings)
        {
            _logger = logger;
            _settings = settings;
            Status = "Idle";
        }

        public bool IsConnected => _connected;
        public string Status { get; private set; }
        public bool HasUdp => _udpChannel != null && _udpChannel.HasRemoteEndpoint;
        public long LastTcpReceiveMs => Interlocked.Read(ref _lastTcpReceiveMs);
        public long LastPingRttMs => Interlocked.Read(ref _lastPingRttMs);
        public long UdpLastReceiveMs => _udpChannel != null ? _udpChannel.LastReceiveUnixMs : 0;
        public long LastTcpTransformMs => Interlocked.Read(ref _lastTcpTransformMs);
        public long LastUdpTransformMs => Interlocked.Read(ref _lastUdpTransformMs);
        public long LastHostTransformReceivedMs => Interlocked.Read(ref _lastHostTransformReceivedMs);
        public long LastHostTransformReceivedTick => Interlocked.Read(ref _lastHostTransformReceivedTick);
        public int TcpTransformCount => Interlocked.CompareExchange(ref _tcpTransformCount, 0, 0);
        public int UdpTransformCount => Interlocked.CompareExchange(ref _udpTransformCount, 0, 0);
        public string LastTransformSource => _lastTransformSource;
        public long HostStateReadCount => Interlocked.Read(ref _hostStateReadCount);
        public long HostStateEnqueuedCount => Interlocked.Read(ref _hostStateEnqueuedCount);
        public int LatestHostTransformSeq
        {
            get
            {
                lock (_latestHostTransformLock)
                {
                    return _latestHostTransformSeq;
                }
            }
        }
        public int LastConsumedHostTransformSeq
        {
            get
            {
                lock (_latestHostTransformLock)
                {
                    return _lastConsumedHostTransformSeq;
                }
            }
        }

        public bool TryGetLatestHostTransform(out PlayerTransformState state)
        {
            lock (_latestHostTransformLock)
            {
                if (_latestHostTransformSeq == 0)
                {
                    state = default;
                    return false;
                }

                state = _latestHostTransform;
                return true;
            }
        }

        public bool TryConsumeLatestHostTransform(out PlayerTransformState state)
        {
            lock (_latestHostTransformLock)
            {
                if (_latestHostTransformSeq == _lastConsumedHostTransformSeq)
                {
                    state = default;
                    return false;
                }

                state = _latestHostTransform;
                _lastConsumedHostTransformSeq = _latestHostTransformSeq;
                return true;
            }
        }

        public void MarkLatestHostTransformConsumed()
        {
            lock (_latestHostTransformLock)
            {
                _lastConsumedHostTransformSeq = _latestHostTransformSeq;
            }
        }

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
            if (_incomingPriority.TryDequeue(out message))
            {
                return true;
            }

            if (TryDequeueUdp(out message))
            {
                return true;
            }

            if (_incoming.TryDequeue(out message))
            {
                if (message is PlayerTransformMessage)
                {
                    var pending = Interlocked.Decrement(ref _pendingTransformMessages);
                    if (pending < 0)
                    {
                        Interlocked.Exchange(ref _pendingTransformMessages, 0);
                    }
                }
                return true;
            }

            return false;
        }

        public void SendUdp(Message message)
        {
            if (_udpChannel == null || !_udpChannel.HasRemoteEndpoint) return;
            if (message == null) return;
            var payload = BuildPayload(message);
            if (payload == null) return;
            _udpChannel.Send(payload);
        }

        public void SendPing()
        {
            if (!_connected) return;
            Interlocked.Exchange(ref _lastPingSentMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            Enqueue(new PingMessage());
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
                while (_running && _connected)
                {
                    var stream = _stream;
                    if (stream == null) break;

                    var readResult = TcpFraming.TryReadFrame(stream, lengthBuffer, out var payload);
                    if (readResult == FrameReadResult.Disconnected) break;
                    if (readResult == FrameReadResult.BadFrame)
                    {
                        Status = "Bad frame";
                        break;
                    }

                    if (Protocol.TryParsePayload(payload, out var message, out var error))
                    {
                        Interlocked.Exchange(ref _lastTcpReceiveMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                        if (IsHostStateType(message.Type))
                        {
                            Interlocked.Increment(ref _hostStateReadCount);
                        }
                        if (message is PingMessage)
                        {
                            Enqueue(new PongMessage());
                        }
                        else if (message is PongMessage pong)
                        {
                            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            var sent = Interlocked.Read(ref _lastPingSentMs);
                            if (sent > 0)
                            {
                                Interlocked.Exchange(ref _lastPingRttMs, now - sent);
                            }
                            if (pong.HasTransform)
                            {
                                StoreLatestHostTransform(pong.Transform, "TCP/Pong", isTcp: true);
                                TryEnqueueTransform(pong.Transform);
                            }
                        }
                        else if (message is PlayerTransformMessage)
                        {
                            var state = ((PlayerTransformMessage)message).State;
                            StoreLatestHostTransform(state, "TCP", isTcp: true);
                            TryEnqueueTransform(state);
                        }
                        else if (message is UdpInfoMessage udpInfo)
                        {
                            ConfigureUdp(udpInfo.Port);
                            SendUdpPing();
                        }
                        else
                        {
                            if (IsHostStateType(message.Type))
                            {
                                Interlocked.Increment(ref _hostStateEnqueuedCount);
                                _incomingPriority.Enqueue(message);
                                continue;
                            }
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
                while (_running && _connected)
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

        private void Cleanup()
        {
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null;
            _client = null;
            _udpChannel?.Stop();
            _udpChannel = null;
            DrainQueue(_incomingPriority);
            DrainQueue(_incoming);
            Interlocked.Exchange(ref _lastTcpReceiveMs, 0);
            Interlocked.Exchange(ref _lastPingSentMs, 0);
            Interlocked.Exchange(ref _lastPingRttMs, 0);
            Interlocked.Exchange(ref _lastTcpTransformMs, 0);
            Interlocked.Exchange(ref _lastUdpTransformMs, 0);
            Interlocked.Exchange(ref _lastHostTransformReceivedMs, 0);
            Interlocked.Exchange(ref _lastHostTransformReceivedTick, 0);
            Interlocked.Exchange(ref _tcpTransformCount, 0);
            Interlocked.Exchange(ref _udpTransformCount, 0);
            Interlocked.Exchange(ref _lastTransformSource, "n/a");
            Interlocked.Exchange(ref _hostStateReadCount, 0);
            Interlocked.Exchange(ref _hostStateEnqueuedCount, 0);
            Interlocked.Exchange(ref _pendingTransformMessages, 0);
            _udpDrainBudget = 0;
            _udpDrainBudgetTick = 0;
            lock (_latestHostTransformLock)
            {
                _latestHostTransform = default;
                _latestHostTransformSeq = 0;
                _lastConsumedHostTransformSeq = 0;
            }
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
                case MessageType.PlayerInput:
                    return Protocol.BuildPlayerInput(((PlayerInputMessage)message).State);
                case MessageType.UdpInfo:
                    return Protocol.BuildUdpInfo(((UdpInfoMessage)message).Port);
                case MessageType.SceneReady:
                    return Protocol.BuildSceneReady(((SceneReadyMessage)message).SceneName);
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

            var now = Environment.TickCount;
            if (unchecked(now - _udpDrainBudgetTick) >= UdpDrainWindowMs)
            {
                _udpDrainBudgetTick = now;
                _udpDrainBudget = MaxUdpDrainPerFrame;
            }

            if (_udpDrainBudget <= 0)
            {
                return false;
            }

            var drained = 0;
            while (drained < MaxUdpDrainPerCall && _udpDrainBudget > 0 && _udpChannel.TryDequeue(out message))
            {
                drained++;
                _udpDrainBudget--;
                if (message is PingMessage || message is PongMessage || message is UdpInfoMessage)
                {
                    continue;
                }

                if (message is PlayerTransformMessage)
                {
                    var state = ((PlayerTransformMessage)message).State;
                    StoreLatestHostTransform(state, "UDP", isTcp: false);
                    TryEnqueueTransform(state);
                    continue;
                }

                return true;
            }

            message = null;
            return false;
        }

        private void MarkTcpTransform(string source)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Interlocked.Exchange(ref _lastTcpTransformMs, now);
            Interlocked.Increment(ref _tcpTransformCount);
            Interlocked.Exchange(ref _lastTransformSource, source);
        }

        private void MarkUdpTransform(string source)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Interlocked.Exchange(ref _lastUdpTransformMs, now);
            Interlocked.Increment(ref _udpTransformCount);
            Interlocked.Exchange(ref _lastTransformSource, source);
        }

        private void StoreLatestHostTransform(PlayerTransformState state, string source, bool isTcp)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var nowTick = Stopwatch.GetTimestamp();
            if (isTcp)
            {
                MarkTcpTransform(source);
            }
            else
            {
                MarkUdpTransform(source);
            }
            Interlocked.Exchange(ref _lastHostTransformReceivedMs, nowMs);
            Interlocked.Exchange(ref _lastHostTransformReceivedTick, nowTick);

            lock (_latestHostTransformLock)
            {
                _latestHostTransform = state;
                _latestHostTransformSeq++;
            }
        }

        private bool TryEnqueueTransform(PlayerTransformState state)
        {
            var pending = Interlocked.Increment(ref _pendingTransformMessages);
            if (pending > MaxPendingTransforms)
            {
                DropOldTransforms(pending - MaxPendingTransforms);
                pending = Interlocked.CompareExchange(ref _pendingTransformMessages, 0, 0);
                if (pending > MaxPendingTransforms)
                {
                    Interlocked.Decrement(ref _pendingTransformMessages);
                    return false;
                }
            }

            _incoming.Enqueue(new PlayerTransformMessage(state));
            return true;
        }

        private void DropOldTransforms(int desiredDrops)
        {
            if (desiredDrops <= 0) return;
            var dropped = 0;
            var attempts = 0;
            var maxAttempts = Math.Max(desiredDrops * 4, 16);
            while (dropped < desiredDrops && attempts < maxAttempts && _incoming.TryDequeue(out var message))
            {
                attempts++;
                if (message is PlayerTransformMessage)
                {
                    var pending = Interlocked.Decrement(ref _pendingTransformMessages);
                    if (pending < 0)
                    {
                        Interlocked.Exchange(ref _pendingTransformMessages, 0);
                    }
                    dropped++;
                    continue;
                }

                _incoming.Enqueue(message);
            }
        }

        private static void DrainQueue(ConcurrentQueue<Message> queue)
        {
            if (queue == null) return;
            while (queue.TryDequeue(out _))
            {
            }
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
    }
}
