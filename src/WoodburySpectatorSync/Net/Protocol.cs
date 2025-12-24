using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace WoodburySpectatorSync.Net
{
    public enum MessageType : ushort
    {
        CameraState = 1,
        SceneChange = 2,
        ProgressMarker = 3,
        Ping = 4,
        Pong = 5,
        PlayerTransform = 6,
        InteractRequest = 7,
        DoorState = 8,
        HoldableState = 9,
        StoryFlag = 10,
        AiTransform = 11
    }

    public abstract class Message
    {
        public MessageType Type;
    }

    public sealed class CameraStateMessage : Message
    {
        public CameraState State;

        public CameraStateMessage(CameraState state)
        {
            Type = MessageType.CameraState;
            State = state;
        }
    }

    public sealed class SceneChangeMessage : Message
    {
        public string SceneName;

        public SceneChangeMessage(string sceneName)
        {
            Type = MessageType.SceneChange;
            SceneName = sceneName ?? string.Empty;
        }
    }

    public sealed class ProgressMarkerMessage : Message
    {
        public string Marker;

        public ProgressMarkerMessage(string marker)
        {
            Type = MessageType.ProgressMarker;
            Marker = marker ?? string.Empty;
        }
    }

    public sealed class PingMessage : Message
    {
        public PingMessage()
        {
            Type = MessageType.Ping;
        }
    }

    public sealed class PongMessage : Message
    {
        public PongMessage()
        {
            Type = MessageType.Pong;
        }
    }

    public sealed class PlayerTransformMessage : Message
    {
        public PlayerTransformState State;

        public PlayerTransformMessage(PlayerTransformState state)
        {
            Type = MessageType.PlayerTransform;
            State = state;
        }
    }

    public sealed class InteractRequestMessage : Message
    {
        public byte PlayerId;
        public string TargetPath;
        public byte ActionType;

        public InteractRequestMessage(byte playerId, string targetPath, byte actionType)
        {
            Type = MessageType.InteractRequest;
            PlayerId = playerId;
            TargetPath = targetPath ?? string.Empty;
            ActionType = actionType;
        }
    }

    public sealed class DoorStateMessage : Message
    {
        public DoorState State;

        public DoorStateMessage(DoorState state)
        {
            Type = MessageType.DoorState;
            State = state;
        }
    }

    public sealed class HoldableStateMessage : Message
    {
        public HoldableState State;

        public HoldableStateMessage(HoldableState state)
        {
            Type = MessageType.HoldableState;
            State = state;
        }
    }

    public sealed class StoryFlagMessage : Message
    {
        public string Key;
        public int Value;

        public StoryFlagMessage(string key, int value)
        {
            Type = MessageType.StoryFlag;
            Key = key ?? string.Empty;
            Value = value;
        }
    }

    public sealed class AiTransformMessage : Message
    {
        public AiTransformState State;

        public AiTransformMessage(AiTransformState state)
        {
            Type = MessageType.AiTransform;
            State = state;
        }
    }

    public struct CameraState
    {
        public long UnixTimeMs;
        public Vector3 Position;
        public Quaternion Rotation;
        public float Fov;
    }

    public struct PlayerTransformState
    {
        public byte PlayerId;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 CameraPosition;
        public Quaternion CameraRotation;
    }

    public struct DoorState
    {
        public string Path;
        public byte DoorType;
        public bool IsOpen;
        public bool IsLocked;
    }

    public struct HoldableState
    {
        public string Path;
        public byte Holder;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool Active;
    }

    public struct AiTransformState
    {
        public string Path;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool Active;
    }

    public static class Protocol
    {
        public const uint Magic = 0x57535331; // "WSS1"
        public const ushort Version = 1;
        public const int MaxPayloadBytes = 1024 * 1024;

        public static byte[] BuildFrame(byte[] payload)
        {
            if (payload == null) payload = new byte[0];
            var frame = new byte[4 + payload.Length];
            var lengthBytes = BitConverter.GetBytes(payload.Length);
            Buffer.BlockCopy(lengthBytes, 0, frame, 0, 4);
            Buffer.BlockCopy(payload, 0, frame, 4, payload.Length);
            return frame;
        }

        public static byte[] BuildCameraState(CameraState state)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.CameraState);
                writer.Write(state.UnixTimeMs);
                writer.Write(state.Position.x);
                writer.Write(state.Position.y);
                writer.Write(state.Position.z);
                writer.Write(state.Rotation.x);
                writer.Write(state.Rotation.y);
                writer.Write(state.Rotation.z);
                writer.Write(state.Rotation.w);
                writer.Write(state.Fov);
                return ms.ToArray();
            }
        }

        public static byte[] BuildSceneChange(string sceneName)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.SceneChange);
                WriteString(writer, sceneName);
                return ms.ToArray();
            }
        }

        public static byte[] BuildProgressMarker(string marker)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.ProgressMarker);
                WriteString(writer, marker);
                return ms.ToArray();
            }
        }

        public static byte[] BuildPing()
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.Ping);
                return ms.ToArray();
            }
        }

        public static byte[] BuildPong()
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.Pong);
                return ms.ToArray();
            }
        }

        public static byte[] BuildPlayerTransform(PlayerTransformState state)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.PlayerTransform);
                writer.Write(state.PlayerId);
                WriteVector3(writer, state.Position);
                WriteQuaternion(writer, state.Rotation);
                WriteVector3(writer, state.CameraPosition);
                WriteQuaternion(writer, state.CameraRotation);
                return ms.ToArray();
            }
        }

        public static byte[] BuildInteractRequest(byte playerId, string targetPath, byte actionType)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.InteractRequest);
                writer.Write(playerId);
                WriteString(writer, targetPath);
                writer.Write(actionType);
                return ms.ToArray();
            }
        }

        public static byte[] BuildDoorState(DoorState state)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.DoorState);
                WriteString(writer, state.Path);
                writer.Write(state.DoorType);
                writer.Write(state.IsOpen);
                writer.Write(state.IsLocked);
                return ms.ToArray();
            }
        }

        public static byte[] BuildHoldableState(HoldableState state)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.HoldableState);
                WriteString(writer, state.Path);
                writer.Write(state.Holder);
                WriteVector3(writer, state.Position);
                WriteQuaternion(writer, state.Rotation);
                writer.Write(state.Active);
                return ms.ToArray();
            }
        }

        public static byte[] BuildStoryFlag(string key, int value)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.StoryFlag);
                WriteString(writer, key);
                writer.Write(value);
                return ms.ToArray();
            }
        }

        public static byte[] BuildAiTransform(AiTransformState state)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.AiTransform);
                WriteString(writer, state.Path);
                WriteVector3(writer, state.Position);
                WriteQuaternion(writer, state.Rotation);
                writer.Write(state.Active);
                return ms.ToArray();
            }
        }

        public static bool TryParsePayload(byte[] payload, out Message message, out string error)
        {
            message = null;
            error = null;

            if (payload == null || payload.Length < 8)
            {
                error = "Payload too short";
                return false;
            }

            try
            {
                using (var ms = new MemoryStream(payload))
                using (var reader = new BinaryReader(ms, Encoding.UTF8))
                {
                    var magic = reader.ReadUInt32();
                    if (magic != Magic)
                    {
                        error = "Bad magic";
                        return false;
                    }

                    var version = reader.ReadUInt16();
                    if (version != Version)
                    {
                        error = "Unsupported version";
                        return false;
                    }

                    var type = (MessageType)reader.ReadUInt16();
                    switch (type)
                    {
                        case MessageType.CameraState:
                            var state = new CameraState
                            {
                                UnixTimeMs = reader.ReadInt64(),
                                Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                                Rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
                                Fov = reader.ReadSingle()
                            };
                            message = new CameraStateMessage(state);
                            return true;
                        case MessageType.SceneChange:
                            message = new SceneChangeMessage(ReadString(reader));
                            return true;
                        case MessageType.ProgressMarker:
                            message = new ProgressMarkerMessage(ReadString(reader));
                            return true;
                        case MessageType.Ping:
                            message = new PingMessage();
                            return true;
                        case MessageType.Pong:
                            message = new PongMessage();
                            return true;
                        case MessageType.PlayerTransform:
                            message = new PlayerTransformMessage(new PlayerTransformState
                            {
                                PlayerId = reader.ReadByte(),
                                Position = ReadVector3(reader),
                                Rotation = ReadQuaternion(reader),
                                CameraPosition = ReadVector3(reader),
                                CameraRotation = ReadQuaternion(reader)
                            });
                            return true;
                        case MessageType.InteractRequest:
                            message = new InteractRequestMessage(reader.ReadByte(), ReadString(reader), reader.ReadByte());
                            return true;
                        case MessageType.DoorState:
                            message = new DoorStateMessage(new DoorState
                            {
                                Path = ReadString(reader),
                                DoorType = reader.ReadByte(),
                                IsOpen = reader.ReadBoolean(),
                                IsLocked = reader.ReadBoolean()
                            });
                            return true;
                        case MessageType.HoldableState:
                            message = new HoldableStateMessage(new HoldableState
                            {
                                Path = ReadString(reader),
                                Holder = reader.ReadByte(),
                                Position = ReadVector3(reader),
                                Rotation = ReadQuaternion(reader),
                                Active = reader.ReadBoolean()
                            });
                            return true;
                        case MessageType.StoryFlag:
                            message = new StoryFlagMessage(ReadString(reader), reader.ReadInt32());
                            return true;
                        case MessageType.AiTransform:
                            message = new AiTransformMessage(new AiTransformState
                            {
                                Path = ReadString(reader),
                                Position = ReadVector3(reader),
                                Rotation = ReadQuaternion(reader),
                                Active = reader.ReadBoolean()
                            });
                            return true;
                        default:
                            error = "Unknown message type";
                            return false;
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static void WriteHeader(BinaryWriter writer, MessageType type)
        {
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write((ushort)type);
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            if (value == null) value = string.Empty;
            var bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(bytes.Length);
            writer.Write(bytes);
        }

        private static string ReadString(BinaryReader reader)
        {
            var length = reader.ReadInt32();
            if (length < 0 || length > MaxPayloadBytes)
            {
                throw new InvalidDataException("Invalid string length");
            }

            var bytes = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(bytes);
        }

        private static void WriteVector3(BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        private static Vector3 ReadVector3(BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        private static void WriteQuaternion(BinaryWriter writer, Quaternion value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
            writer.Write(value.w);
        }

        private static Quaternion ReadQuaternion(BinaryReader reader)
        {
            return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
