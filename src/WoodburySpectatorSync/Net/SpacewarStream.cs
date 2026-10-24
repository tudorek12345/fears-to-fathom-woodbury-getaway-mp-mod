using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace WoodburySpectatorSync.Net
{
    // A reliable, ordered byte stream over one Steam peer. The native helper owns
    // Steam, lobby validation and relay negotiation; no Steam DLL enters Unity.
    internal sealed class SpacewarStream : Stream
    {
        private readonly object _gate = new object();
        private readonly object _writeGate = new object();
        private readonly Queue<byte[]> _incoming = new Queue<byte[]>();
        private readonly SpacewarQueue _outgoing = new SpacewarQueue();
        private readonly Action<string> _status;
        private readonly bool _host;
        private readonly ulong _targetLobby;
        private readonly Process _process;
        private readonly long _started = Stopwatch.GetTimestamp();
        private byte[] _current;
        private int _offset, _queuedBytes;
        private int _request = 2;
        private ulong _self, _peer;
        private bool _ready, _peerReady;
        private long _routeStarted;
        private static readonly byte[] RouteProbe = Protocol.BuildFrame(Protocol.BuildPing());
        private volatile bool _closed;
        private Exception _failure;
        public ulong LobbyId { get; private set; }

        public SpacewarStream(string directory, bool host, ulong lobby, Action<string> status)
        {
            _host = host; _targetLobby = lobby; _status = status;
            if (!host && lobby == 0) throw new IOException("Enter the host's Spacewar lobby ID.");
            string executable = Path.Combine(directory, "Woodbury.Spacewar.exe");
            if (!File.Exists(executable) || !File.Exists(Path.Combine(directory, "steam_api64.dll")))
                throw new FileNotFoundException("Spacewar helper missing. Install the Spacewar folder beside WoodburySpectatorSync.dll.", executable);
            var start = new ProcessStartInfo(executable, "--parent " + Process.GetCurrentProcess().Id)
            {
                WorkingDirectory = directory, UseShellExecute = false, CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            start.EnvironmentVariables["SteamAppId"] = "480";
            start.EnvironmentVariables["SteamGameId"] = "480";
            _process = new Process { StartInfo = start };
            try
            {
                _process.ErrorDataReceived += (s, e) => { if (!string.IsNullOrEmpty(e.Data)) _status?.Invoke("Spacewar: " + e.Data); };
                if (!_process.Start()) throw new IOException("Could not launch Spacewar helper.");
                _process.BeginErrorReadLine();
                Queue(SpacewarOp.Init, 1, SpacewarWire.Data(w => { w.Write(Protocol.Version); SpacewarWire.Text(w, Protocol.PluginVersion); w.Write(0UL); }));
                new Thread(ReadHelper) { IsBackground = true, Name = "WSS-SpacewarRead" }.Start();
                new Thread(WriteHelper) { IsBackground = true, Name = "WSS-SpacewarWrite" }.Start();
            }
            catch { Dispose(); throw; }
        }

        public void WaitForPeer(Func<bool> keepWaiting)
        {
            lock (_gate)
            {
                CheckOpen();
                while (!_peerReady)
                {
                    CheckOpen();
                    if (!keepWaiting()) throw new OperationCanceledException();
                    if (LobbyId == 0 && (Stopwatch.GetTimestamp() - _started) / (double)Stopwatch.Frequency > 45)
                        throw new IOException("Spacewar initialization/lobby timed out. Start Steam and verify App ID 480.");
                    if (_routeStarted != 0 && (Stopwatch.GetTimestamp() - _routeStarted) / (double)Stopwatch.Frequency > 65)
                        throw new IOException("Spacewar lobby joined, but no packets arrived from the other player within 65 seconds. Steam could not establish a direct or relay route.");
                    System.Threading.Monitor.Wait(_gate, 100);
                }
            }
        }

        private void Queue(SpacewarOp op, uint request, byte[] data)
        {
            _outgoing.Push(new SpacewarFrame { Op = op, Request = request, Data = data, Reliable = true });
        }

        private void WriteHelper()
        {
            try
            {
                while (!_closed)
                {
                    if (_outgoing.Pop(out var frame)) SpacewarWire.Write(_process.StandardInput.BaseStream, frame);
                    else Thread.Sleep(2);
                }
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void ReadHelper()
        {
            try
            {
                while (!_closed)
                {
                    var frame = SpacewarWire.Read(_process.StandardOutput.BaseStream);
                    using (var reader = new BinaryReader(new MemoryStream(frame.Data)))
                    lock (_gate)
                    {
                        switch (frame.Op)
                        {
                            case SpacewarOp.Ready:
                                if (_ready || frame.Request != 1 || reader.ReadUInt32() != 480) throw new IOException("Invalid Steam initialization.");
                                _self = reader.ReadUInt64(); SpacewarWire.Text(reader);
                                if (_self == 0) throw new IOException("Missing Steam identity.");
                                _ready = true;
                                Queue(_host ? SpacewarOp.Host : SpacewarOp.Join, 2,
                                    _host ? new byte[0] : SpacewarWire.Data(w => w.Write(_targetLobby)));
                                break;
                            case SpacewarOp.Lobby:
                                if (!_ready || frame.Request != 2) break;
                                ulong lobby = reader.ReadUInt64(), owner = reader.ReadUInt64(), peer = reader.ReadUInt64();
                                byte count = reader.ReadByte();
                                if (lobby == 0 || owner == 0 || count > 1 ||
                                    (_host ? owner != _self : owner == _self || lobby != _targetLobby) ||
                                    (count == 0 ? peer != 0 : reader.ReadUInt64() != peer || peer == 0 || peer == _self || (!_host && peer != owner)) ||
                                    reader.BaseStream.Position != reader.BaseStream.Length) throw new IOException("Invalid two-player Spacewar lobby.");
                                if (_peer != 0 && _peer != peer) throw new IOException("Spacewar peer left. Start/connect again for a new session.");
                                LobbyId = lobby; _peer = peer;
                                if (peer != 0 && _routeStarted == 0)
                                {
                                    _routeStarted = Stopwatch.GetTimestamp();
                                    // Lobby membership is not packet connectivity. A normal game Ping
                                    // starts Steam route negotiation on both peers before returning
                                    // Connected to Unity. Older peers can answer it with Pong too.
                                    Queue(SpacewarOp.Send, unchecked((uint)Interlocked.Increment(ref _request)),
                                        SpacewarWire.Data(w => { w.Write(peer); w.Write(RouteProbe); }));
                                }
                                _status?.Invoke("Spacewar lobby " + lobby + (peer == 0 ? " (waiting for player 2)" : " (establishing packet connection)"));
                                System.Threading.Monitor.PulseAll(_gate);
                                break;
                            case SpacewarOp.Packet:
                                if (frame.Request != 2 || !frame.Reliable) break;
                                ulong sender = reader.ReadUInt64();
                                if (_peer == 0 || sender != _peer) break;
                                int size = frame.Data.Length - 8;
                                if (size <= 0 || size > SpacewarWire.MaxPacket || _queuedBytes + size > SpacewarWire.MaxQueue)
                                    throw new IOException("Spacewar receive queue overflow or invalid packet.");
                                if (!_peerReady)
                                {
                                    _peerReady = true;
                                    _status?.Invoke("Spacewar packet connection established.");
                                    System.Threading.Monitor.PulseAll(_gate);
                                    bool probe = size == RouteProbe.Length;
                                    for (int i = 0; probe && i < size; i++) probe = frame.Data[i + 8] == RouteProbe[i];
                                    if (probe) break; // Only consume the initial transport probe.
                                }
                                _incoming.Enqueue(reader.ReadBytes(size)); _queuedBytes += size;
                                System.Threading.Monitor.PulseAll(_gate);
                                break;
                            case SpacewarOp.PeerLeft:
                            case SpacewarOp.ConnectionLost:
                                if (frame.Request == 2 && reader.ReadUInt64() == _peer) throw new IOException("Spacewar peer disconnected.");
                                break;
                            case SpacewarOp.Error:
                                reader.ReadByte();
                                throw new IOException(SpacewarWire.Text(reader));
                            case SpacewarOp.Status:
                                if (!_peerReady && frame.Request == 2) _status?.Invoke(SpacewarWire.Text(reader));
                                break;
                        }
                    }
                }
            }
            catch (Exception ex) { Fail(ex); }
        }

        private void Fail(Exception ex)
        {
            lock (_gate)
            {
                if (_failure == null)
                {
                    _failure = ex;
                    if (!_closed) _status?.Invoke("Spacewar connection failed: " + ex.Message);
                }
                System.Threading.Monitor.PulseAll(_gate);
            }
        }
        private void CheckOpen()
        {
            if (_closed) throw new ObjectDisposedException(nameof(SpacewarStream));
            if (_failure != null) throw new IOException("Spacewar transport failed: " + _failure.Message, _failure);
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null || offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentException("Invalid read range.");
            if (count == 0) return 0;
            lock (_gate)
            {
                while (_current == null)
                {
                    CheckOpen();
                    if (_incoming.Count > 0) { _current = _incoming.Dequeue(); _offset = 0; break; }
                    System.Threading.Monitor.Wait(_gate, 100);
                }
                int length = Math.Min(count, _current.Length - _offset);
                Buffer.BlockCopy(_current, _offset, buffer, offset, length);
                _offset += length; _queuedBytes -= length;
                if (_offset == _current.Length) _current = null;
                return length;
            }
        }
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null || offset < 0 || count < 0 || offset > buffer.Length - count) throw new ArgumentException("Invalid write range.");
            lock (_writeGate)
            {
                CheckOpen();
                ulong peer; lock (_gate) peer = _peer;
                if (peer == 0) throw new IOException("Spacewar peer is not ready.");
                // Split the game's 1 MiB frames into bounded reliable Steam messages.
                while (count > 0)
                {
                    int length = Math.Min(count, 64 * 1024);
                    Queue(SpacewarOp.Send, unchecked((uint)Interlocked.Increment(ref _request)), SpacewarWire.Data(w => { w.Write(peer); w.Write(buffer, offset, length); }));
                    offset += length; count -= length;
                }
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (_closed) return;
            _closed = true;
            lock (_gate) System.Threading.Monitor.PulseAll(_gate);
            if (disposing && _process != null)
            {
                try { _process.StandardInput.Close(); } catch { }
                try { if (!_process.WaitForExit(500)) _process.Kill(); } catch { }
                try { _process.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }
        public override bool CanRead => !_closed;
        public override bool CanWrite => !_closed;
        public override bool CanSeek => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }
}
