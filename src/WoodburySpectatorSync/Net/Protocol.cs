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
        AiTransform = 11,
        PlayerInput = 12,
        UdpInfo = 13,
        SceneReady = 14,
        DialogueLine = 15,
        DialogueStart = 16,
        DialogueAdvance = 17,
        DialogueChoice = 18,
        DialogueEnd = 19,
        Hello = 20,
        HelloAck = 21,
        SnapshotBegin = 22,
        SnapshotEnd = 23,
        SnapshotAck = 24
    }

    public enum ProtocolParseErrorCode
    {
        None = 0,
        BadMagic = 1,
        UnsupportedVersion = 2,
        UnknownMessageType = 3,
        MalformedPayload = 4,
        PayloadTooLarge = 5
    }

    public enum SceneReadyStatus : byte
    {
        Unknown = 0,
        ReadyFull = 1,
        ReadyPartial = 2
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
        public int SessionId;
        public int Generation;
        public string SceneName;
        public int BuildIndex;
        public int StartSequence;
        public int FromMenu;

        public SceneChangeMessage(string sceneName, int buildIndex = -1, int startSequence = -1, int fromMenu = -1, int sessionId = 0, int generation = 0)
        {
            Type = MessageType.SceneChange;
            SessionId = sessionId;
            Generation = generation;
            SceneName = sceneName ?? string.Empty;
            BuildIndex = buildIndex;
            StartSequence = startSequence;
            FromMenu = fromMenu;
        }
    }

    public sealed class SceneReadyMessage : Message
    {
        public int SessionId;
        public int Generation;
        public SceneReadyStatus ReadyStatus;
        public string Reason;
        public string SceneName;

        public SceneReadyMessage(string sceneName, int sessionId = 0, int generation = 0, SceneReadyStatus readyStatus = SceneReadyStatus.ReadyFull, string reason = "")
        {
            Type = MessageType.SceneReady;
            SessionId = sessionId;
            Generation = generation;
            ReadyStatus = readyStatus;
            Reason = reason ?? string.Empty;
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
        public bool HasTransform;
        public PlayerTransformState Transform;

        public PongMessage()
        {
            Type = MessageType.Pong;
        }

        public PongMessage(PlayerTransformState transform)
        {
            Type = MessageType.Pong;
            HasTransform = true;
            Transform = transform;
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

    public sealed class PlayerInputMessage : Message
    {
        public PlayerInputState State;

        public PlayerInputMessage(PlayerInputState state)
        {
            Type = MessageType.PlayerInput;
            State = state;
        }
    }

    public sealed class UdpInfoMessage : Message
    {
        public int Port;

        public UdpInfoMessage(int port)
        {
            Type = MessageType.UdpInfo;
            Port = port;
        }
    }

    public sealed class DialogueLineMessage : Message
    {
        public string Speaker;
        public string Text;
        public float Duration;
        public byte Kind;

        public DialogueLineMessage(string speaker, string text, float duration, byte kind)
        {
            Type = MessageType.DialogueLine;
            Speaker = speaker ?? string.Empty;
            Text = text ?? string.Empty;
            Duration = duration;
            Kind = kind;
        }
    }

    public sealed class DialogueStartMessage : Message
    {
        public int ConversationId;
        public int EntryId;

        public DialogueStartMessage(int conversationId, int entryId)
        {
            Type = MessageType.DialogueStart;
            ConversationId = conversationId;
            EntryId = entryId;
        }
    }

    public sealed class DialogueAdvanceMessage : Message
    {
        public int ConversationId;
        public int EntryId;

        public DialogueAdvanceMessage(int conversationId, int entryId)
        {
            Type = MessageType.DialogueAdvance;
            ConversationId = conversationId;
            EntryId = entryId;
        }
    }

    public sealed class DialogueChoiceMessage : Message
    {
        public int ConversationId;
        public int EntryId;
        public int ChoiceIndex;

        public DialogueChoiceMessage(int conversationId, int entryId, int choiceIndex)
        {
            Type = MessageType.DialogueChoice;
            ConversationId = conversationId;
            EntryId = entryId;
            ChoiceIndex = choiceIndex;
        }
    }

    public sealed class DialogueEndMessage : Message
    {
        public int ConversationId;

        public DialogueEndMessage(int conversationId)
        {
            Type = MessageType.DialogueEnd;
            ConversationId = conversationId;
        }
    }

    public sealed class HelloMessage : Message
    {
        public ushort ProtocolVersion;
        public string PluginVersion;
        public long ClientNonce;

        public HelloMessage(ushort protocolVersion, string pluginVersion, long clientNonce)
        {
            Type = MessageType.Hello;
            ProtocolVersion = protocolVersion;
            PluginVersion = pluginVersion ?? string.Empty;
            ClientNonce = clientNonce;
        }
    }

    public sealed class HelloAckMessage : Message
    {
        public ushort ProtocolVersion;
        public string PluginVersion;
        public int SessionId;
        public bool Accepted;
        public string Reason;

        public bool Accept { get { return Accepted; } }

        public HelloAckMessage(ushort protocolVersion, string pluginVersion, int sessionId, bool accepted, string reason)
        {
            Type = MessageType.HelloAck;
            ProtocolVersion = protocolVersion;
            PluginVersion = pluginVersion ?? string.Empty;
            SessionId = sessionId;
            Accepted = accepted;
            Reason = reason ?? string.Empty;
        }
    }

    public sealed class SnapshotBeginMessage : Message
    {
        public int SessionId;
        public int Generation;
        public string SceneName;
        public int StoryCount;
        public int DoorCount;
        public int HoldableCount;
        public int AiCount;
        public int DialogueCount;
        public int PlayerCount;
        public int CustomCount;
        public int ExpectedItemCount
        {
            get { return StoryCount + DoorCount + HoldableCount + AiCount + DialogueCount + PlayerCount + CustomCount; }
        }

        public SnapshotBeginMessage(int sessionId, int generation, string sceneName, int storyCount, int doorCount, int holdableCount, int aiCount, int dialogueCount, int playerCount, int customCount)
        {
            Type = MessageType.SnapshotBegin;
            SessionId = sessionId;
            Generation = generation;
            SceneName = sceneName ?? string.Empty;
            StoryCount = storyCount;
            DoorCount = doorCount;
            HoldableCount = holdableCount;
            AiCount = aiCount;
            DialogueCount = dialogueCount;
            PlayerCount = playerCount;
            CustomCount = customCount;
        }
    }

    public sealed class SnapshotEndMessage : Message
    {
        public int SessionId;
        public int Generation;
        public string SceneName;
        public int StoryCount;
        public int DoorCount;
        public int HoldableCount;
        public int AiCount;
        public int DialogueCount;
        public int PlayerCount;
        public int CustomCount;
        public int SentItemCount
        {
            get { return StoryCount + DoorCount + HoldableCount + AiCount + DialogueCount + PlayerCount + CustomCount; }
        }

        public SnapshotEndMessage(int sessionId, int generation, string sceneName, int storyCount, int doorCount, int holdableCount, int aiCount, int dialogueCount, int playerCount, int customCount)
        {
            Type = MessageType.SnapshotEnd;
            SessionId = sessionId;
            Generation = generation;
            SceneName = sceneName ?? string.Empty;
            StoryCount = storyCount;
            DoorCount = doorCount;
            HoldableCount = holdableCount;
            AiCount = aiCount;
            DialogueCount = dialogueCount;
            PlayerCount = playerCount;
            CustomCount = customCount;
        }
    }

    public sealed class SnapshotAckMessage : Message
    {
        public int SessionId;
        public int Generation;
        public string SceneName;
        public int AppliedStoryCount;
        public int AppliedDoorCount;
        public int AppliedHoldableCount;
        public int AppliedAiCount;
        public int AppliedDialogueCount;
        public int AppliedPlayerCount;
        public int PendingObjectCount;
        public int MissingObjectCount;
        public bool Ok;
        public string Reason;
        public int AppliedItemCount
        {
            get
            {
                return AppliedStoryCount + AppliedDoorCount + AppliedHoldableCount +
                       AppliedAiCount + AppliedDialogueCount + AppliedPlayerCount;
            }
        }

        public SnapshotAckMessage(int sessionId, int generation, string sceneName, int appliedStoryCount, int appliedDoorCount, int appliedHoldableCount, int appliedAiCount, int appliedDialogueCount, int appliedPlayerCount, int pendingObjectCount, int missingObjectCount, bool ok, string reason)
        {
            Type = MessageType.SnapshotAck;
            SessionId = sessionId;
            Generation = generation;
            SceneName = sceneName ?? string.Empty;
            AppliedStoryCount = appliedStoryCount;
            AppliedDoorCount = appliedDoorCount;
            AppliedHoldableCount = appliedHoldableCount;
            AppliedAiCount = appliedAiCount;
            AppliedDialogueCount = appliedDialogueCount;
            AppliedPlayerCount = appliedPlayerCount;
            PendingObjectCount = pendingObjectCount;
            MissingObjectCount = missingObjectCount;
            Ok = ok;
            Reason = reason ?? string.Empty;
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

    public struct PlayerInputState
    {
        public byte PlayerId;
        public float MoveX;
        public float MoveY;
        public float LookYaw;
        public float LookPitch;
        public bool Jump;
        public bool Crouch;
        public bool Sprint;
    }

    public static class Protocol
    {
        public const uint Magic = 0x57535331; // "WSS1"
        public const ushort Version = 2;
        public const string PluginVersion = "0.3.0";
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

        public static byte[] BuildSceneChange(string sceneName, int buildIndex = -1, int startSequence = -1, int fromMenu = -1, int sessionId = 0, int generation = 0)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.SceneChange);
                writer.Write(sessionId);
                writer.Write(generation);
                WriteString(writer, sceneName);
                writer.Write(buildIndex);
                writer.Write(startSequence);
                writer.Write(fromMenu);
                return ms.ToArray();
            }
        }

        public static byte[] BuildSceneReady(string sceneName, int sessionId = 0, int generation = 0, SceneReadyStatus readyStatus = SceneReadyStatus.ReadyFull, string reason = "")
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.SceneReady);
                writer.Write(sessionId);
                writer.Write(generation);
                WriteString(writer, sceneName);
                writer.Write((byte)readyStatus);
                WriteString(writer, reason);
                return ms.ToArray();
            }
        }

        public static byte[] BuildHello(ushort protocolVersion, string pluginVersion, long clientNonce)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.Hello);
                writer.Write(protocolVersion);
                WriteString(writer, pluginVersion);
                writer.Write(clientNonce);
                return ms.ToArray();
            }
        }

        public static byte[] BuildHelloAck(ushort protocolVersion, string pluginVersion, int sessionId, bool accepted, string reason)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.HelloAck);
                writer.Write(protocolVersion);
                WriteString(writer, pluginVersion);
                writer.Write(sessionId);
                writer.Write(accepted);
                WriteString(writer, reason);
                return ms.ToArray();
            }
        }

        public static byte[] BuildSnapshotBegin(int sessionId, int generation, string sceneName, int storyCount, int doorCount, int holdableCount, int aiCount, int dialogueCount, int playerCount, int customCount)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.SnapshotBegin);
                writer.Write(sessionId);
                writer.Write(generation);
                WriteString(writer, sceneName);
                WriteSnapshotCounts(writer, storyCount, doorCount, holdableCount, aiCount, dialogueCount, playerCount, customCount);
                return ms.ToArray();
            }
        }

        public static byte[] BuildSnapshotEnd(int sessionId, int generation, string sceneName, int storyCount, int doorCount, int holdableCount, int aiCount, int dialogueCount, int playerCount, int customCount)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.SnapshotEnd);
                writer.Write(sessionId);
                writer.Write(generation);
                WriteString(writer, sceneName);
                WriteSnapshotCounts(writer, storyCount, doorCount, holdableCount, aiCount, dialogueCount, playerCount, customCount);
                return ms.ToArray();
            }
        }

        public static byte[] BuildSnapshotAck(
            int sessionId,
            int generation,
            string sceneName,
            int appliedStoryCount,
            int appliedDoorCount,
            int appliedHoldableCount,
            int appliedAiCount,
            int appliedDialogueCount,
            int appliedPlayerCount,
            int pendingObjectCount,
            int missingObjectCount,
            bool ok,
            string reason)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.SnapshotAck);
                writer.Write(sessionId);
                writer.Write(generation);
                WriteString(writer, sceneName);
                writer.Write(appliedStoryCount);
                writer.Write(appliedDoorCount);
                writer.Write(appliedHoldableCount);
                writer.Write(appliedAiCount);
                writer.Write(appliedDialogueCount);
                writer.Write(appliedPlayerCount);
                writer.Write(pendingObjectCount);
                writer.Write(missingObjectCount);
                writer.Write(ok);
                WriteString(writer, reason);
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

        public static byte[] BuildPong(PlayerTransformState? hostTransform = null)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.Pong);
                if (hostTransform.HasValue)
                {
                    writer.Write((byte)1);
                    WritePlayerTransform(writer, hostTransform.Value);
                }
                return ms.ToArray();
            }
        }

        public static byte[] BuildPlayerTransform(PlayerTransformState state)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.PlayerTransform);
                WritePlayerTransform(writer, state);
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

        public static byte[] BuildPlayerInput(PlayerInputState state)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.PlayerInput);
                writer.Write(state.PlayerId);
                writer.Write(state.MoveX);
                writer.Write(state.MoveY);
                writer.Write(state.LookYaw);
                writer.Write(state.LookPitch);
                writer.Write(state.Jump);
                writer.Write(state.Crouch);
                writer.Write(state.Sprint);
                return ms.ToArray();
            }
        }

        public static byte[] BuildUdpInfo(int port)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.UdpInfo);
                writer.Write(port);
                return ms.ToArray();
            }
        }

        public static byte[] BuildDialogueLine(string speaker, string text, float duration, byte kind)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.DialogueLine);
                WriteString(writer, speaker);
                WriteString(writer, text);
                writer.Write(duration);
                writer.Write(kind);
                return ms.ToArray();
            }
        }

        public static byte[] BuildDialogueStart(int conversationId, int entryId)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.DialogueStart);
                writer.Write(conversationId);
                writer.Write(entryId);
                return ms.ToArray();
            }
        }

        public static byte[] BuildDialogueAdvance(int conversationId, int entryId)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.DialogueAdvance);
                writer.Write(conversationId);
                writer.Write(entryId);
                return ms.ToArray();
            }
        }

        public static byte[] BuildDialogueChoice(int conversationId, int entryId, int choiceIndex)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.DialogueChoice);
                writer.Write(conversationId);
                writer.Write(entryId);
                writer.Write(choiceIndex);
                return ms.ToArray();
            }
        }

        public static byte[] BuildDialogueEnd(int conversationId)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                WriteHeader(writer, MessageType.DialogueEnd);
                writer.Write(conversationId);
                return ms.ToArray();
            }
        }

        public static bool TryParsePayload(byte[] payload, out Message message, out string error)
        {
            ProtocolParseErrorCode errorCode;
            return TryParsePayload(payload, out message, out errorCode, out error);
        }

        public static bool TryParsePayload(byte[] payload, out Message message, out ProtocolParseErrorCode errorCode, out string error)
        {
            message = null;
            errorCode = ProtocolParseErrorCode.None;
            error = null;

            if (payload == null || payload.Length < 8)
            {
                errorCode = ProtocolParseErrorCode.MalformedPayload;
                error = "MalformedPayload: payload too short";
                return false;
            }

            if (payload.Length > MaxPayloadBytes)
            {
                errorCode = ProtocolParseErrorCode.PayloadTooLarge;
                error = "PayloadTooLarge: " + payload.Length + " > " + MaxPayloadBytes;
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
                        errorCode = ProtocolParseErrorCode.BadMagic;
                        error = "BadMagic: 0x" + magic.ToString("X8");
                        return false;
                    }

                    var version = reader.ReadUInt16();
                    if (version != Version)
                    {
                        errorCode = ProtocolParseErrorCode.UnsupportedVersion;
                        error = "UnsupportedVersion: " + version + " expected " + Version;
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
                        {
                            var sessionId = reader.ReadInt32();
                            var generation = reader.ReadInt32();
                            var sceneName = ReadString(reader);
                            var buildIndex = -1;
                            var startSequence = -1;
                            var fromMenu = -1;
                            if (ms.Position <= ms.Length - 4)
                            {
                                buildIndex = reader.ReadInt32();
                            }
                            if (ms.Position <= ms.Length - 4)
                            {
                                startSequence = reader.ReadInt32();
                            }
                            if (ms.Position <= ms.Length - 4)
                            {
                                fromMenu = reader.ReadInt32();
                            }
                            message = new SceneChangeMessage(sceneName, buildIndex, startSequence, fromMenu, sessionId, generation);
                            return true;
                        }
                        case MessageType.SceneReady:
                        {
                            var sessionId = reader.ReadInt32();
                            var generation = reader.ReadInt32();
                            var sceneName = ReadString(reader);
                            var readyStatus = SceneReadyStatus.ReadyFull;
                            var reason = string.Empty;
                            if (ms.Position < ms.Length)
                            {
                                readyStatus = (SceneReadyStatus)reader.ReadByte();
                            }
                            if (ms.Position < ms.Length)
                            {
                                reason = ReadString(reader);
                            }
                            message = new SceneReadyMessage(sceneName, sessionId, generation, readyStatus, reason);
                            return true;
                        }
                        case MessageType.ProgressMarker:
                            message = new ProgressMarkerMessage(ReadString(reader));
                            return true;
                        case MessageType.Ping:
                            message = new PingMessage();
                            return true;
                        case MessageType.Pong:
                        {
                            var pong = new PongMessage();
                            if (ms.Position < ms.Length)
                            {
                                var hasTransform = reader.ReadByte();
                                if (hasTransform != 0)
                                {
                                    pong.HasTransform = true;
                                    pong.Transform = new PlayerTransformState
                                    {
                                        PlayerId = reader.ReadByte(),
                                        Position = ReadVector3(reader),
                                        Rotation = ReadQuaternion(reader),
                                        CameraPosition = ReadVector3(reader),
                                        CameraRotation = ReadQuaternion(reader)
                                    };
                                }
                            }
                            message = pong;
                            return true;
                        }
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
                        case MessageType.PlayerInput:
                            message = new PlayerInputMessage(new PlayerInputState
                            {
                                PlayerId = reader.ReadByte(),
                                MoveX = reader.ReadSingle(),
                                MoveY = reader.ReadSingle(),
                                LookYaw = reader.ReadSingle(),
                                LookPitch = reader.ReadSingle(),
                                Jump = reader.ReadBoolean(),
                                Crouch = reader.ReadBoolean(),
                                Sprint = reader.ReadBoolean()
                            });
                            return true;
                        case MessageType.UdpInfo:
                            message = new UdpInfoMessage(reader.ReadInt32());
                            return true;
                        case MessageType.DialogueLine:
                        {
                            var speaker = ReadString(reader);
                            var text = ReadString(reader);
                            var duration = reader.ReadSingle();
                            var kind = reader.ReadByte();
                            message = new DialogueLineMessage(speaker, text, duration, kind);
                            return true;
                        }
                        case MessageType.DialogueStart:
                            message = new DialogueStartMessage(reader.ReadInt32(), reader.ReadInt32());
                            return true;
                        case MessageType.DialogueAdvance:
                            message = new DialogueAdvanceMessage(reader.ReadInt32(), reader.ReadInt32());
                            return true;
                        case MessageType.DialogueChoice:
                            message = new DialogueChoiceMessage(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
                            return true;
                        case MessageType.DialogueEnd:
                            message = new DialogueEndMessage(reader.ReadInt32());
                            return true;
                        case MessageType.Hello:
                        {
                            var protocolVersion = reader.ReadUInt16();
                            var pluginVersion = ReadString(reader);
                            var clientNonce = reader.ReadInt64();
                            message = new HelloMessage(protocolVersion, pluginVersion, clientNonce);
                            return true;
                        }
                        case MessageType.HelloAck:
                        {
                            var protocolVersion = reader.ReadUInt16();
                            var pluginVersion = ReadString(reader);
                            var sessionId = reader.ReadInt32();
                            var accept = reader.ReadBoolean();
                            var reason = ReadString(reader);
                            message = new HelloAckMessage(protocolVersion, pluginVersion, sessionId, accept, reason);
                            return true;
                        }
                        case MessageType.SnapshotBegin:
                        {
                            var sessionId = reader.ReadInt32();
                            var generation = reader.ReadInt32();
                            var sceneName = ReadString(reader);
                            ReadSnapshotCounts(reader, out var storyCount, out var doorCount, out var holdableCount, out var aiCount, out var dialogueCount, out var playerCount, out var customCount);
                            message = new SnapshotBeginMessage(sessionId, generation, sceneName, storyCount, doorCount, holdableCount, aiCount, dialogueCount, playerCount, customCount);
                            return true;
                        }
                        case MessageType.SnapshotEnd:
                        {
                            var sessionId = reader.ReadInt32();
                            var generation = reader.ReadInt32();
                            var sceneName = ReadString(reader);
                            ReadSnapshotCounts(reader, out var storyCount, out var doorCount, out var holdableCount, out var aiCount, out var dialogueCount, out var playerCount, out var customCount);
                            message = new SnapshotEndMessage(sessionId, generation, sceneName, storyCount, doorCount, holdableCount, aiCount, dialogueCount, playerCount, customCount);
                            return true;
                        }
                        case MessageType.SnapshotAck:
                        {
                            var sessionId = reader.ReadInt32();
                            var generation = reader.ReadInt32();
                            var sceneName = ReadString(reader);
                            var appliedStoryCount = reader.ReadInt32();
                            var appliedDoorCount = reader.ReadInt32();
                            var appliedHoldableCount = reader.ReadInt32();
                            var appliedAiCount = reader.ReadInt32();
                            var appliedDialogueCount = reader.ReadInt32();
                            var appliedPlayerCount = reader.ReadInt32();
                            var pendingObjectCount = reader.ReadInt32();
                            var missingObjectCount = reader.ReadInt32();
                            var ok = reader.ReadBoolean();
                            var reason = ReadString(reader);
                            message = new SnapshotAckMessage(
                                sessionId,
                                generation,
                                sceneName,
                                appliedStoryCount,
                                appliedDoorCount,
                                appliedHoldableCount,
                                appliedAiCount,
                                appliedDialogueCount,
                                appliedPlayerCount,
                                pendingObjectCount,
                                missingObjectCount,
                                ok,
                                reason);
                            return true;
                        }
                        default:
                            errorCode = ProtocolParseErrorCode.UnknownMessageType;
                            error = "UnknownMessageType: " + ((ushort)type);
                            return false;
                    }
                }
            }
            catch (EndOfStreamException ex)
            {
                errorCode = ProtocolParseErrorCode.MalformedPayload;
                error = "MalformedPayload: " + ex.Message;
                return false;
            }
            catch (InvalidDataException ex)
            {
                errorCode = ProtocolParseErrorCode.MalformedPayload;
                error = "MalformedPayload: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                errorCode = ProtocolParseErrorCode.MalformedPayload;
                error = "MalformedPayload: " + ex.Message;
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

        private static void WriteSnapshotCounts(BinaryWriter writer, int storyCount, int doorCount, int holdableCount, int aiCount, int dialogueCount, int playerCount, int customCount)
        {
            writer.Write(storyCount);
            writer.Write(doorCount);
            writer.Write(holdableCount);
            writer.Write(aiCount);
            writer.Write(dialogueCount);
            writer.Write(playerCount);
            writer.Write(customCount);
        }

        private static void ReadSnapshotCounts(
            BinaryReader reader,
            out int storyCount,
            out int doorCount,
            out int holdableCount,
            out int aiCount,
            out int dialogueCount,
            out int playerCount,
            out int customCount)
        {
            storyCount = reader.ReadInt32();
            doorCount = reader.ReadInt32();
            holdableCount = reader.ReadInt32();
            aiCount = reader.ReadInt32();
            dialogueCount = reader.ReadInt32();
            playerCount = reader.ReadInt32();
            customCount = reader.ReadInt32();
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

        private static void WritePlayerTransform(BinaryWriter writer, PlayerTransformState state)
        {
            writer.Write(state.PlayerId);
            WriteVector3(writer, state.Position);
            WriteQuaternion(writer, state.Rotation);
            WriteVector3(writer, state.CameraPosition);
            WriteQuaternion(writer, state.CameraRotation);
        }

        private static Quaternion ReadQuaternion(BinaryReader reader)
        {
            return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
