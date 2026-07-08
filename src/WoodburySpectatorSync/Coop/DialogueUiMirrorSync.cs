using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.SceneManagement;

namespace WoodburySpectatorSync.Coop
{
    internal static class DialogueUiMirrorSync
    {
        public const string UiKind = "DialogueMenu";

        private const string PayloadHeader = "WSS_DIALOGUE_MENU";
        private const int PayloadVersion = 1;

        public struct Choice
        {
            public int Index;
            public int EntryId;
            public string Text;
        }

        public struct MenuState
        {
            public string SceneName;
            public int ConversationId;
            public int EntryId;
            public int ChoiceIndex;
            public string Speaker;
            public string Text;
            public Choice[] Choices;
        }

        public static string BuildPayload(
            string sceneName,
            int conversationId,
            int entryId,
            int choiceIndex,
            string speaker,
            string text,
            IList<Choice> choices)
        {
            var builder = new StringBuilder(256);
            builder.Append(PayloadHeader).Append('|').Append(PayloadVersion).AppendLine();
            builder.Append("scene=").Append(Encode(sceneName)).AppendLine();
            builder.Append("conv=").Append(conversationId).AppendLine();
            builder.Append("entry=").Append(entryId).AppendLine();
            builder.Append("choice=").Append(choiceIndex).AppendLine();
            builder.Append("speaker=").Append(Encode(speaker)).AppendLine();
            builder.Append("text=").Append(Encode(text)).AppendLine();

            var count = choices != null ? choices.Count : 0;
            builder.Append("count=").Append(count).AppendLine();
            for (var i = 0; i < count; i++)
            {
                var choice = choices[i];
                builder.Append("c|")
                    .Append(choice.Index).Append('|')
                    .Append(choice.EntryId).Append('|')
                    .Append(Encode(choice.Text))
                    .AppendLine();
            }

            return builder.ToString();
        }

        public static bool TryParsePayload(string payload, out MenuState state)
        {
            state = new MenuState
            {
                SceneName = string.Empty,
                ConversationId = -1,
                EntryId = -1,
                ChoiceIndex = -1,
                Speaker = string.Empty,
                Text = string.Empty,
                Choices = new Choice[0]
            };

            if (string.IsNullOrEmpty(payload))
            {
                return false;
            }

            var lines = payload.Replace("\r\n", "\n").Split('\n');
            if (lines.Length == 0 ||
                !lines[0].StartsWith(PayloadHeader + "|" + PayloadVersion, StringComparison.Ordinal))
            {
                return false;
            }

            var choices = new List<Choice>();
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                if (line.StartsWith("c|", StringComparison.Ordinal))
                {
                    var parts = line.Split(new[] { '|' }, 4);
                    if (parts.Length < 4)
                    {
                        continue;
                    }

                    choices.Add(new Choice
                    {
                        Index = ParseInt(parts[1], -1),
                        EntryId = ParseInt(parts[2], -1),
                        Text = Decode(parts[3])
                    });
                    continue;
                }

                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line.Substring(0, separator);
                var value = line.Substring(separator + 1);
                switch (key)
                {
                    case "scene":
                        state.SceneName = Decode(value);
                        break;
                    case "conv":
                        state.ConversationId = ParseInt(value, -1);
                        break;
                    case "entry":
                        state.EntryId = ParseInt(value, -1);
                        break;
                    case "choice":
                        state.ChoiceIndex = ParseInt(value, -1);
                        break;
                    case "speaker":
                        state.Speaker = Decode(value);
                        break;
                    case "text":
                        state.Text = Decode(value);
                        break;
                }
            }

            state.Choices = choices.ToArray();
            return true;
        }

        public static bool IsForCurrentScene(MenuState state)
        {
            if (string.IsNullOrEmpty(state.SceneName))
            {
                return true;
            }

            return string.Equals(
                state.SceneName,
                SceneManager.GetActiveScene().name,
                StringComparison.Ordinal);
        }

        public static string BuildDisplayText(MenuState state)
        {
            var builder = new StringBuilder(256);
            if (!string.IsNullOrEmpty(state.Text))
            {
                builder.Append(state.Text.Trim());
            }

            var choices = state.Choices ?? new Choice[0];
            for (var i = 0; i < choices.Length; i++)
            {
                if (string.IsNullOrEmpty(choices[i].Text))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(choices[i].Index == state.ChoiceIndex ? "> " : "  ");
                builder.Append(choices[i].Text.Trim());
            }

            return builder.ToString();
        }

        private static string Encode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string Decode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int ParseInt(string value, int fallback)
        {
            int result;
            return int.TryParse(value, out result) ? result : fallback;
        }
    }
}
