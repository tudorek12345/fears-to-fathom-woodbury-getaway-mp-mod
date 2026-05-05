using BepInEx.Configuration;

namespace WoodburySpectatorSync.Config
{
    public enum Mode
    {
        Host,
        Spectator,
        CoopHost,
        CoopClient
    }

    public enum RemotePlayerAvatarSource
    {
        Auto,
        GameModel,
        AssetBundle,
        Capsule
    }

    public sealed class Settings
    {
        public ConfigEntry<Mode> ModeSetting;
        public ConfigEntry<string> HostBindIP;
        public ConfigEntry<int> HostPort;
        public ConfigEntry<string> SpectatorHostIP;
        public ConfigEntry<int> SendHz;
        public ConfigEntry<float> SmoothingPosition;
        public ConfigEntry<float> SmoothingRotation;
        public ConfigEntry<bool> OverlayEnabled;
        public ConfigEntry<bool> VerboseLogging;
        public ConfigEntry<bool> UdpEnabled;
        public ConfigEntry<int> UdpPort;
        public ConfigEntry<float> CoopTeleportDistance;
        public ConfigEntry<float> CoopTeleportCooldownSeconds;
        public ConfigEntry<float> CoopTeleportStaleSeconds;
        public ConfigEntry<bool> CoopSnapToHostOnSceneLoad;
        public ConfigEntry<bool> CoopUseLocalPlayer;
        public ConfigEntry<bool> CoopRouteInteractions;
        public ConfigEntry<bool> CoopAutoStartHost;
        public ConfigEntry<bool> CoopAutoConnectClient;
        public ConfigEntry<bool> CoopForceCabinStart;
        public ConfigEntry<string> CoopCabinStartSequence;
        public ConfigEntry<string> CoopRemotePlayerPrefabPath;
        public ConfigEntry<string> CoopRemotePlayerRig;
        public ConfigEntry<RemotePlayerAvatarSource> CoopRemotePlayerAvatarSource;
        public ConfigEntry<string> CoopRemotePlayerAvatarBundlePath;
        public ConfigEntry<string> CoopRemotePlayerAvatarId;
        public ConfigEntry<float> CoopRemotePlayerAvatarScale;
        public ConfigEntry<float> CoopRemotePlayerAvatarYOffset;

        public static Settings Bind(ConfigFile config)
        {
            var settings = new Settings();
            settings.ModeSetting = config.Bind("General", "Mode", Mode.Host, "Host, Spectator, CoopHost, or CoopClient");
            settings.HostBindIP = config.Bind("Host", "HostBindIP", "0.0.0.0", "Host listen bind IP");
            settings.HostPort = config.Bind("Host", "HostPort", 27055, "Host listen port");
            settings.SpectatorHostIP = config.Bind("Spectator", "SpectatorHostIP", "127.0.0.1", "Host IP to connect to");
            settings.SendHz = config.Bind("Sync", "SendHz", 20, "Camera send rate (Hz)");
            settings.SmoothingPosition = config.Bind("Sync", "SmoothingPosition", 0.15f, "Camera position smoothing 0..1");
            settings.SmoothingRotation = config.Bind("Sync", "SmoothingRotation", 0.15f, "Camera rotation smoothing 0..1");
            settings.OverlayEnabled = config.Bind("UI", "OverlayEnabled", true, "Show overlay by default");
            settings.VerboseLogging = config.Bind("Debug", "VerboseLogging", false, "Verbose logging");
            settings.UdpEnabled = config.Bind("Network", "UdpEnabled", true, "Enable UDP for high-frequency state (camera/transform)");
            settings.UdpPort = config.Bind("Network", "UdpPort", 27056, "UDP port for high-frequency state");
            settings.CoopTeleportDistance = config.Bind("Coop", "TeleportDistance", 25f, "Auto-teleport client to host if farther than this (meters)");
            settings.CoopTeleportCooldownSeconds = config.Bind("Coop", "TeleportCooldownSeconds", 3f, "Minimum seconds between auto-teleports");
            settings.CoopTeleportStaleSeconds = config.Bind("Coop", "TeleportOnStaleSeconds", 6f, "Auto-teleport if host updates are stale for this long");
            settings.CoopSnapToHostOnSceneLoad = config.Bind("Coop", "SnapToHostOnSceneLoad", false, "Snap freecam to host after scene load. In local-player co-op, scene-start placement uses a side offset instead of exact overlap.");
            settings.CoopUseLocalPlayer = config.Bind("Coop", "UseLocalPlayerController", true, "Use the local player controller instead of freecam in co-op client");
            settings.CoopRouteInteractions = config.Bind("Coop", "RouteInteractionsToHost", true, "Route client interaction clicks to the host (prevents local story triggers)");
            settings.CoopAutoStartHost = config.Bind("Coop", "AutoStartHost", false, "Auto-start co-op host server on launch");
            settings.CoopAutoConnectClient = config.Bind("Coop", "AutoConnectClient", false, "Auto-connect co-op client on launch");
            settings.CoopForceCabinStart = config.Bind("Coop", "ForceCabinStartSequence", true, "Force the client to start at a cabin testing sequence (skips driving intro)");
            settings.CoopCabinStartSequence = config.Bind("Coop", "CabinStartSequence", "StartAfterShower", "CabinSceneSequences enum name (StartAfterShower, StartInKitchenAfterOvenStart, StartHiding)");
            settings.CoopRemotePlayerPrefabPath = config.Bind("Coop", "RemotePlayerPrefabPath", string.Empty, "Optional dedicated remote player source path (NetPath or Resources path). If empty, fallback uses local FPC clone.");
            settings.CoopRemotePlayerRig = config.Bind("Coop", "RemotePlayerRig", "Auto", "Animator rig profile for remote player proxies (Auto, WoodburyFpc, ThirdPersonBasic, LegacyHumanoid).");
            settings.CoopRemotePlayerAvatarSource = config.Bind("Coop", "RemotePlayerAvatarSource", RemotePlayerAvatarSource.Auto, "Remote player avatar source (Auto, GameModel, AssetBundle, Capsule). Auto prefers safe in-scene models and keeps AssetBundle optional.");
            settings.CoopRemotePlayerAvatarBundlePath = config.Bind("Coop", "RemotePlayerAvatarBundlePath", "BepInEx/plugins/WoodburySpectatorSync/avatars/woodbury_avatars.bundle", "Optional Unity AssetBundle for remote player avatars. Used only when RemotePlayerPrefabPath is blank.");
            settings.CoopRemotePlayerAvatarId = config.Bind("Coop", "RemotePlayerAvatarId", "woodbury_scene_auto", "Avatar id. Use woodbury_scene_auto for in-scene avatars or a bundle manifest id such as quaternius_regular_male for explicit AssetBundle testing.");
            settings.CoopRemotePlayerAvatarScale = config.Bind("Coop", "RemotePlayerAvatarScale", 1f, "Multiplier applied on top of the avatar manifest scale.");
            settings.CoopRemotePlayerAvatarYOffset = config.Bind("Coop", "RemotePlayerAvatarYOffset", 0f, "Extra vertical offset added on top of the avatar manifest offset.");
            return settings;
        }
    }
}
