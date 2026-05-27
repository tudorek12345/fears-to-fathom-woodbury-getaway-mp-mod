using System;
using System.Reflection;
using WoodburySpectatorSync.Config;

namespace WoodburySpectatorSync.Coop
{
    internal static class PlayerDisplayName
    {
        private const int MaxLength = 24;

        public static string Resolve(Settings settings, string fallback)
        {
            var configured = settings != null && settings.CoopDisplayName != null
                ? settings.CoopDisplayName.Value
                : string.Empty;

            var name = Sanitize(configured);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            name = Sanitize(TryGetSteamName());
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return Sanitize(fallback);
        }

        public static string Normalize(string value, string fallback)
        {
            var name = Sanitize(value);
            return !string.IsNullOrEmpty(name) ? name : Sanitize(fallback);
        }

        public static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ').Replace('|', '/').Trim();
            while (normalized.IndexOf("  ", StringComparison.Ordinal) >= 0)
            {
                normalized = normalized.Replace("  ", " ");
            }

            if (normalized.Length > MaxLength)
            {
                normalized = normalized.Substring(0, MaxLength).Trim();
            }

            return normalized;
        }

        private static string TryGetSteamName()
        {
            try
            {
                var steamClientType = Type.GetType("Steamworks.SteamClient, Facepunch.Steamworks.Win64", false);
                if (steamClientType == null)
                {
                    steamClientType = Type.GetType("Steamworks.SteamClient, Facepunch.Steamworks", false);
                }

                if (steamClientType == null)
                {
                    return string.Empty;
                }

                var flags = BindingFlags.Public | BindingFlags.Static;
                var isValidProperty = steamClientType.GetProperty("IsValid", flags);
                if (isValidProperty != null &&
                    isValidProperty.PropertyType == typeof(bool) &&
                    !(bool)isValidProperty.GetValue(null, null))
                {
                    return string.Empty;
                }

                var nameProperty = steamClientType.GetProperty("Name", flags);
                if (nameProperty != null && nameProperty.PropertyType == typeof(string))
                {
                    return nameProperty.GetValue(null, null) as string;
                }
            }
            catch
            {
                return string.Empty;
            }

            return string.Empty;
        }
    }
}
