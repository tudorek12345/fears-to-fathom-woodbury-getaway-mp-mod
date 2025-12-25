using System;
using System.Net.Sockets;

namespace WoodburySpectatorSync.Net
{
    internal enum FrameReadResult
    {
        Success,
        Disconnected,
        BadFrame
    }

    internal static class TcpFraming
    {
        public static FrameReadResult TryReadFrame(NetworkStream stream, byte[] lengthBuffer, out byte[] payload)
        {
            payload = null;
            if (!ReadExact(stream, lengthBuffer, 4)) return FrameReadResult.Disconnected;

            var payloadLength = BitConverter.ToInt32(lengthBuffer, 0);
            if (payloadLength <= 0 || payloadLength > Protocol.MaxPayloadBytes)
            {
                return FrameReadResult.BadFrame;
            }

            payload = new byte[payloadLength];
            return ReadExact(stream, payload, payloadLength) ? FrameReadResult.Success : FrameReadResult.Disconnected;
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
    }
}
