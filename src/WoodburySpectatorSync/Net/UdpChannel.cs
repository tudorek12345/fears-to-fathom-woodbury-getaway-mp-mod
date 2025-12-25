using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BepInEx.Logging;

namespace WoodburySpectatorSync.Net
{
    public sealed class UdpChannel
    {
        private readonly ManualLogSource _logger;
        private readonly ConcurrentQueue<Message> _incoming = new ConcurrentQueue<Message>();
        private readonly Dictionary<MessageType, uint> _lastSeqByType = new Dictionary<MessageType, uint>();
        private UdpClient _client;
        private IPEndPoint _remoteEndpoint;
        private Thread _receiveThread;
        private volatile bool _running;
        private uint _sendSeq;
        private long _lastReceiveUnixMs;
        private long _lastSendUnixMs;

        public UdpChannel(ManualLogSource logger, int localPort, IPAddress bindAddress = null)
        {
            _logger = logger;
            _client = bindAddress != null
                ? new UdpClient(new IPEndPoint(bindAddress, localPort))
                : new UdpClient(localPort);
            _running = true;
            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "WSS-UdpReceive" };
            _receiveThread.Start();
        }

        public bool HasRemoteEndpoint => _remoteEndpoint != null;
        public long LastReceiveUnixMs => Interlocked.Read(ref _lastReceiveUnixMs);
        public long LastSendUnixMs => Interlocked.Read(ref _lastSendUnixMs);

        public void SetRemote(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host) || port <= 0) return;
            if (IPAddress.TryParse(host, out var address))
            {
                _remoteEndpoint = new IPEndPoint(address, port);
            }
            else
            {
                try
                {
                    var addresses = Dns.GetHostAddresses(host);
                    if (addresses.Length > 0)
                    {
                        _remoteEndpoint = new IPEndPoint(addresses[0], port);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("UDP resolve failed: " + ex.Message);
                }
            }
        }

        public void ClearRemote()
        {
            _remoteEndpoint = null;
        }

        public void Send(byte[] payload)
        {
            if (payload == null || payload.Length == 0) return;
            if (_remoteEndpoint == null) return;

            var seq = unchecked(++_sendSeq);
            var packet = new byte[4 + payload.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(seq), 0, packet, 0, 4);
            Buffer.BlockCopy(payload, 0, packet, 4, payload.Length);

            try
            {
                Interlocked.Exchange(ref _lastSendUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                _client.Send(packet, packet.Length, _remoteEndpoint);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("UDP send failed: " + ex.Message);
            }
        }

        public bool TryDequeue(out Message message)
        {
            return _incoming.TryDequeue(out message);
        }

        public void Stop()
        {
            _running = false;
            try { _client?.Close(); } catch { }
        }

        private void ReceiveLoop()
        {
            while (_running)
            {
                try
                {
                    IPEndPoint remote = null;
                    var data = _client.Receive(ref remote);
                    if (data == null || data.Length < 5) continue;

                    if (remote != null)
                    {
                        _remoteEndpoint = remote;
                    }

                    Interlocked.Exchange(ref _lastReceiveUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                    var seq = BitConverter.ToUInt32(data, 0);
                    var payload = new byte[data.Length - 4];
                    Buffer.BlockCopy(data, 4, payload, 0, payload.Length);

                    if (Protocol.TryParsePayload(payload, out var message, out var error))
                    {
                        if (ShouldAccept(seq, message.Type))
                        {
                            _incoming.Enqueue(message);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("UDP parse error: " + error);
                    }
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (SocketException)
                {
                    Thread.Sleep(5);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("UDP receive error: " + ex.Message);
                    Thread.Sleep(5);
                }
            }
        }

        private bool ShouldAccept(uint seq, MessageType type)
        {
            if (_lastSeqByType.TryGetValue(type, out var last))
            {
                if (seq <= last)
                {
                    return false;
                }
            }

            _lastSeqByType[type] = seq;
            return true;
        }
    }
}
