using System;
using System.Collections.Generic;
using System.IO;
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

            name = Sanitize(TryGetSteamLoginUsersPersonaName());
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

        private static string TryGetSteamLoginUsersPersonaName()
        {
            try
            {
                foreach (var steamRoot in GetSteamRootCandidates())
                {
                    var path = Path.Combine(steamRoot, "config", "loginusers.vdf");
                    if (!File.Exists(path))
                    {
                        continue;
                    }

                    var name = ParseLoginUsersPersonaName(path);
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
            }
            catch
            {
                return string.Empty;
            }

            return string.Empty;
        }

        private static IEnumerable<string> GetSteamRootCandidates()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddCandidate(seen, Environment.GetEnvironmentVariable("SteamPath"));
            AddCandidate(seen, Environment.GetEnvironmentVariable("STEAM_PATH"));
            AddCandidate(seen, Environment.GetEnvironmentVariable("SteamDir"));

            var programFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (!string.IsNullOrEmpty(programFilesX86))
            {
                AddCandidate(seen, Path.Combine(programFilesX86, "Steam"));
            }

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (!string.IsNullOrEmpty(programFiles))
            {
                AddCandidate(seen, Path.Combine(programFiles, "Steam"));
            }

            AddCandidate(seen, @"C:\Program Files (x86)\Steam");
            AddCandidate(seen, @"C:\Program Files\Steam");

            return seen;
        }

        private static void AddCandidate(HashSet<string> seen, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                seen.Add(Path.GetFullPath(path.Trim().Trim('"')));
            }
            catch
            {
                // Ignore malformed environment paths.
            }
        }

        private static string ParseLoginUsersPersonaName(string path)
        {
            var bestName = string.Empty;
            var fallbackName = string.Empty;
            var currentName = string.Empty;
            var currentMostRecent = false;
            var depth = 0;
            var inUserBlock = false;

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (line == "{")
                {
                    depth++;
                    if (depth == 2)
                    {
                        inUserBlock = true;
                        currentName = string.Empty;
                        currentMostRecent = false;
                    }
                    continue;
                }

                if (line == "}")
                {
                    if (inUserBlock && depth == 2)
                    {
                        if (!string.IsNullOrEmpty(currentName))
                        {
                            if (currentMostRecent)
                            {
                                bestName = currentName;
                            }
                            else if (string.IsNullOrEmpty(fallbackName))
                            {
                                fallbackName = currentName;
                            }
                        }

                        inUserBlock = false;
                    }

                    depth = Math.Max(0, depth - 1);
                    continue;
                }

                if (!inUserBlock)
                {
                    continue;
                }

                var keyValue = ParseVdfKeyValue(line);
                if (keyValue == null)
                {
                    continue;
                }

                if (string.Equals(keyValue.Value.Key, "PersonaName", StringComparison.OrdinalIgnoreCase))
                {
                    currentName = keyValue.Value.Value;
                }
                else if (string.Equals(keyValue.Value.Key, "MostRecent", StringComparison.OrdinalIgnoreCase))
                {
                    currentMostRecent = keyValue.Value.Value == "1";
                }
            }

            return !string.IsNullOrEmpty(bestName) ? bestName : fallbackName;
        }

        private static KeyValuePair<string, string>? ParseVdfKeyValue(string line)
        {
            var firstKeyQuote = line.IndexOf('"');
            if (firstKeyQuote < 0)
            {
                return null;
            }

            var secondKeyQuote = line.IndexOf('"', firstKeyQuote + 1);
            if (secondKeyQuote <= firstKeyQuote)
            {
                return null;
            }

            var firstValueQuote = line.IndexOf('"', secondKeyQuote + 1);
            if (firstValueQuote < 0)
            {
                return null;
            }

            var secondValueQuote = line.IndexOf('"', firstValueQuote + 1);
            if (secondValueQuote <= firstValueQuote)
            {
                return null;
            }

            return new KeyValuePair<string, string>(
                line.Substring(firstKeyQuote + 1, secondKeyQuote - firstKeyQuote - 1),
                line.Substring(firstValueQuote + 1, secondValueQuote - firstValueQuote - 1));
        }
    }
}
