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
            settings.CoopSnapToHostOnSceneLoad = config.Bind("Coop", "SnapToHostOnSceneLoad", true, "Snap client camera to host after scene load");
            settings.CoopUseLocalPlayer = config.Bind("Coop", "UseLocalPlayerController", true, "Use the local player controller instead of freecam in co-op client");
            settings.CoopRouteInteractions = config.Bind("Coop", "RouteInteractionsToHost", true, "Route client interaction clicks to the host (prevents local story triggers)");
            settings.CoopAutoStartHost = config.Bind("Coop", "AutoStartHost", false, "Auto-start co-op host server on launch");
            settings.CoopAutoConnectClient = config.Bind("Coop", "AutoConnectClient", false, "Auto-connect co-op client on launch");
            settings.CoopForceCabinStart = config.Bind("Coop", "ForceCabinStartSequence", true, "Force the client to start at a cabin testing sequence (skips driving intro)");
            settings.CoopCabinStartSequence = config.Bind("Coop", "CabinStartSequence", "StartAfterShower", "CabinSceneSequences enum name (StartAfterShower, StartInKitchenAfterOvenStart, StartHiding)");
            return settings;
        }
    }
}
