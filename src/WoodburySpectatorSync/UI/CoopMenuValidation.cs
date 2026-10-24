using System;

namespace WoodburySpectatorSync.UI
{
    internal static class CoopMenuValidation
    {
        internal static bool TryValidate(bool spacewar, bool joining, string address, string portText,
            string lobbyText, string displayName, out int port, out ulong lobby, out string error)
        {
            port = 0; lobby = 0; error = null;
            if (string.IsNullOrWhiteSpace(displayName) || displayName.Trim().Length > 32)
                error = "Enter a player name (1–32 characters).";
            else if (!spacewar && (!int.TryParse(portText, out port) || port < 1 || port > 65535))
                error = "Enter a port between 1 and 65535.";
            else if (!spacewar && joining && (string.IsNullOrWhiteSpace(address) || address.Trim().Length > 253 ||
                address.IndexOfAny(new[] { '/', '\\', ' ', '\r', '\n', '\t' }) >= 0))
                error = "Enter the host's IP address or hostname.";
            else if (spacewar && joining && (!ulong.TryParse(lobbyText, out lobby) || lobby == 0))
                error = "Enter the lobby ID from your host.";
            return error == null;
        }
    }
}
