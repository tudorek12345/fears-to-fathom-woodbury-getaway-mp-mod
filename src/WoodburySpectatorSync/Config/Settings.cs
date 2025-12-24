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
            return settings;
        }
    }
}
