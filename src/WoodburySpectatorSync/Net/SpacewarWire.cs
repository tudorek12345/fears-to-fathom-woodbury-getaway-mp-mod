using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WoodburySpectatorSync.Net
{
    internal enum SpacewarOp : ushort
    {
        Init = 1, Host = 2, Join = 3, Leave = 4, Friends = 5, Invite = 6, Send = 7, Shutdown = 8, ResetPeer = 9, BlockPeer = 10,
        Ready = 100, Lobby = 101, Packet = 102, Friend = 103, FriendsEnd = 104,
        Invitation = 105, PeerLeft = 106, Error = 107, Status = 108, Ack = 109, ConnectionLost = 110, PeerMetrics = 111, RelayStatus = 112
    }

    internal sealed class SpacewarFrame
    {
        internal SpacewarOp Op;
        internal uint Request;
        internal bool Reliable = true;
        internal byte[] Data = new byte[0];
        internal int Size { get { return 13 + Data.Length; } }
    }

    internal static class SpacewarWire
    {
        internal const ushort Version = 6;
        internal const int MaxPacket = 256 * 1024;
        internal const int MaxFrame = MaxPacket + 1024;
        internal const int MaxQueue = 8 * 1024 * 1024;
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        internal static byte[] Data(Action<BinaryWriter> write)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream)) { write(writer); return stream.ToArray(); }
            }
        }

        internal static void Text(BinaryWriter writer, string text)
        {
            byte[] bytes = Utf8.GetBytes(text ?? string.Empty);
            if (bytes.Length > 4096) throw new IOException("IPC string exceeds 4096 bytes.");
            writer.Write((uint)bytes.Length); writer.Write(bytes);
        }

        internal static string Text(BinaryReader reader)
        {
            uint count = reader.ReadUInt32();
            if (count > 4096) throw new IOException("IPC string exceeds 4096 bytes.");
            return Utf8.GetString(Exact(reader.BaseStream, (int)count));
        }

        internal static byte[] Exact(Stream stream, int count)
        {
            byte[] data = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(data, offset, count - offset);
                if (read == 0) throw new EndOfStreamException("Spacewar IPC closed or truncated.");
                offset += read;
            }
            return data;
        }

        internal static SpacewarFrame Read(Stream stream)
        {
            byte[] prefix = Exact(stream, 4);
            uint size = (uint)prefix[0] | ((uint)prefix[1] << 8) | ((uint)prefix[2] << 16) | ((uint)prefix[3] << 24);
            if (size < 9 || size > MaxFrame) throw new IOException("Invalid Spacewar frame size: " + size + ".");
            using (BinaryReader reader = new BinaryReader(new MemoryStream(Exact(stream, (int)size))))
            {
                if (reader.ReadUInt16() != Version) throw new IOException("Incompatible Spacewar helper version.");
                SpacewarFrame frame = new SpacewarFrame();
                frame.Op = (SpacewarOp)reader.ReadUInt16(); frame.Request = reader.ReadUInt32();
                byte flags = reader.ReadByte();
                if (flags > 1) throw new IOException("Invalid Spacewar flags.");
                frame.Reliable = flags == 1; frame.Data = reader.ReadBytes((int)size - 9);
                return frame;
            }
        }

        internal static void Write(Stream stream, SpacewarFrame frame)
        {
            if (frame.Data == null || frame.Data.Length > MaxFrame - 9) throw new IOException("Invalid Spacewar frame size.");
            byte[] bytes = Data(delegate(BinaryWriter writer)
            {
                writer.Write((uint)(9 + frame.Data.Length)); writer.Write(Version); writer.Write((ushort)frame.Op);
                writer.Write(frame.Request); writer.Write((byte)(frame.Reliable ? 1 : 0)); writer.Write(frame.Data);
            });
            stream.Write(bytes, 0, bytes.Length); stream.Flush();
        }
    }

    // Both pipe workers are bounded. Reliable overflow is a connection failure, never silent data loss.
    internal sealed class SpacewarQueue
    {
        private readonly LinkedList<SpacewarFrame> _frames = new LinkedList<SpacewarFrame>();
        private readonly object _gate = new object();
        private int _bytes;
        internal bool Push(SpacewarFrame frame)
        {
            lock (_gate)
            {
                if (frame.Size > SpacewarWire.MaxQueue) throw new IOException("Spacewar frame exceeds queue capacity.");
                if (_bytes + frame.Size > SpacewarWire.MaxQueue && !frame.Reliable) return false;
                LinkedListNode<SpacewarFrame> node = _frames.First;
                while (_bytes + frame.Size > SpacewarWire.MaxQueue && node != null)
                {
                    LinkedListNode<SpacewarFrame> next = node.Next;
                    if (!node.Value.Reliable) { _bytes -= node.Value.Size; _frames.Remove(node); }
                    node = next;
                }
                if (_bytes + frame.Size > SpacewarWire.MaxQueue) throw new IOException("Spacewar reliable queue exceeded 8 MiB.");
                _frames.AddLast(frame); _bytes += frame.Size; return true;
            }
        }
        internal bool Pop(out SpacewarFrame frame)
        {
            lock (_gate)
            {
                if (_frames.Count == 0) { frame = null; return false; }
                frame = _frames.First.Value; _frames.RemoveFirst(); _bytes -= frame.Size; return true;
            }
        }
        internal void Clear() { lock (_gate) { _frames.Clear(); _bytes = 0; } }
        internal void DropSends()
        {
            lock (_gate)
            {
                LinkedListNode<SpacewarFrame> node = _frames.First;
                while (node != null)
                {
                    LinkedListNode<SpacewarFrame> next = node.Next;
                    if (node.Value.Op == SpacewarOp.Send) { _bytes -= node.Value.Size; _frames.Remove(node); }
                    node = next;
                }
            }
        }
    }
}
