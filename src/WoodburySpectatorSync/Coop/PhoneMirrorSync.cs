using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WoodburySpectatorSync.Coop
{
    internal static class PhoneMirrorSync
    {
        public const string UiKind = "Phone";

        private const string Header = "WSS_PHONE";
        private const int Version = 1;

        public struct Summary
        {
            public int CharacterCount;
            public int BatchCount;
            public int MessageCount;
            public int ActiveMessageCount;
            public int AppliedBatchCount;
            public int AppliedMessageCount;
            public int MissingCount;
            public string SceneName;

            public override string ToString()
            {
                return "scene=" + SceneName +
                       " chars=" + CharacterCount +
                       " batches=" + BatchCount +
                       " messages=" + MessageCount +
                       " active=" + ActiveMessageCount +
                       " appliedBatches=" + AppliedBatchCount +
                       " appliedMessages=" + AppliedMessageCount +
                       " missing=" + MissingCount;
            }
        }

        public static bool TryBuildPayload(string sceneName, out string payload, out int hash, out Summary summary)
        {
            payload = string.Empty;
            hash = 0;
            summary = new Summary { SceneName = sceneName ?? string.Empty };

            var phone = ResolvePhone();
            var notif = GetFieldValue(phone, "notifSystem");
            if (phone == null || notif == null)
            {
                return false;
            }

            var builder = new StringBuilder(4096);
            AppendHeader(builder, sceneName, phone);

            var characters = GetCharacters(notif);
            if (characters != null)
            {
                summary.CharacterCount = characters.Count;
                for (var c = 0; c < characters.Count; c++)
                {
                    var character = characters[c];
                    if (character == null || character.batches == null)
                    {
                        continue;
                    }

                    for (var b = 0; b < character.batches.Count; b++)
                    {
                        var batch = character.batches[b];
                        if (batch == null)
                        {
                            continue;
                        }

                        summary.BatchCount++;
                        var activeIndexes = BuildActiveIndexList(batch.message, ref summary);
                        builder.Append("B|c=").Append(c.ToString(CultureInfo.InvariantCulture))
                            .Append("|b=").Append(b.ToString(CultureInfo.InvariantCulture))
                            .Append("|name=").Append(Encode(character.characterNameString))
                            .Append("|pending=").Append(batch.pendingReply ? "1" : "0")
                            .Append("|reply=").Append(batch.replyBatch ? "1" : "0")
                            .Append("|one=").Append(batch.loadReplyOneByOne ? "1" : "0")
                            .Append("|notdel=").Append(batch.notDeliveredBatch ? "1" : "0")
                            .Append("|next=").Append(batch.nextInSomeTime ? "1" : "0")
                            .Append("|timeMs=").Append(Mathf.RoundToInt(batch.timeTillNext * 1000f).ToString(CultureInfo.InvariantCulture))
                            .Append("|count=").Append(batch.message != null ? batch.message.Count.ToString(CultureInfo.InvariantCulture) : "0")
                            .Append("|active=").Append(activeIndexes)
                            .AppendLine();
                    }
                }
            }

            payload = builder.ToString();
            hash = StableHash(payload);
            return true;
        }

        public static bool TryApplyPayload(string payload, string currentSceneName, out Summary summary)
        {
            summary = new Summary { SceneName = currentSceneName ?? string.Empty };
            if (string.IsNullOrEmpty(payload))
            {
                return true;
            }

            var lines = payload.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0 || !lines[0].StartsWith(Header + "|", StringComparison.Ordinal))
            {
                summary.MissingCount++;
                return true;
            }

            var headerValues = ParseLine(lines[0]);
            var version = GetInt(headerValues, "v", 0);
            if (version != Version)
            {
                summary.MissingCount++;
                return true;
            }

            var payloadScene = Decode(GetString(headerValues, "scene", string.Empty));
            if (!string.IsNullOrEmpty(payloadScene))
            {
                summary.SceneName = payloadScene;
            }

            if (!string.IsNullOrEmpty(payloadScene) &&
                !string.IsNullOrEmpty(currentSceneName) &&
                !string.Equals(payloadScene, currentSceneName, StringComparison.Ordinal))
            {
                return false;
            }

            var phone = ResolvePhone();
            var notif = GetFieldValue(phone, "notifSystem");
            if (phone == null || notif == null)
            {
                return false;
            }

            ApplyPhoneHeader(phone, headerValues);

            var characters = GetCharacters(notif);
            if (characters == null)
            {
                summary.MissingCount++;
                return false;
            }

            summary.CharacterCount = characters.Count;
            for (var i = 1; i < lines.Length; i++)
            {
                if (!lines[i].StartsWith("B|", StringComparison.Ordinal))
                {
                    continue;
                }

                var values = ParseLine(lines[i]);
                var c = GetInt(values, "c", -1);
                var b = GetInt(values, "b", -1);
                var hostCount = GetInt(values, "count", 0);
                summary.BatchCount++;
                summary.MessageCount += Math.Max(0, hostCount);

                if (c < 0 || b < 0 || c >= characters.Count || characters[c] == null || characters[c].batches == null || b >= characters[c].batches.Count)
                {
                    summary.MissingCount++;
                    continue;
                }

                var batch = characters[c].batches[b];
                if (batch == null)
                {
                    summary.MissingCount++;
                    continue;
                }

                batch.pendingReply = GetBool(values, "pending");
                batch.replyBatch = GetBool(values, "reply");
                batch.loadReplyOneByOne = GetBool(values, "one");
                batch.notDeliveredBatch = GetBool(values, "notdel");
                batch.nextInSomeTime = GetBool(values, "next");
                batch.timeTillNext = GetInt(values, "timeMs", Mathf.RoundToInt(batch.timeTillNext * 1000f)) / 1000f;

                if (batch.message == null)
                {
                    summary.MissingCount++;
                    continue;
                }

                summary.AppliedBatchCount++;
                var active = ParseIndexSet(GetString(values, "active", string.Empty));
                for (var m = 0; m < batch.message.Count; m++)
                {
                    var message = batch.message[m];
                    if (message == null)
                    {
                        summary.MissingCount++;
                        continue;
                    }

                    var shouldBeActive = active.Contains(m);
                    if (message.activeSelf != shouldBeActive)
                    {
                        message.SetActive(shouldBeActive);
                    }

                    summary.AppliedMessageCount++;
                    if (shouldBeActive)
                    {
                        summary.ActiveMessageCount++;
                    }
                }

                if (batch.message.Count != hostCount)
                {
                    summary.MissingCount += Math.Abs(batch.message.Count - hostCount);
                }

                TryBringCharacterPanelToFront(characters[c]);
            }

            return true;
        }

        private static void AppendHeader(StringBuilder builder, string sceneName, Phone phone)
        {
            var notif = GetFieldValue(phone, "notifSystem");
            builder.Append(Header)
                .Append("|v=").Append(Version.ToString(CultureInfo.InvariantCulture))
                .Append("|scene=").Append(Encode(sceneName))
                .Append("|allow=").Append(phone.allowPhone ? "1" : "0")
                .Append("|paused=").Append(phone.isPaused ? "1" : "0")
                .Append("|canvas=").Append(phone.phoneCanvas != null && phone.phoneCanvas.activeSelf ? "1" : "0")
                .Append("|net=").Append(((int)phone.networkStatus).ToString(CultureInfo.InvariantCulture))
                .Append("|notifS=").Append(Encode(GetTextValue(GetFieldValue(notif, "senderNameText"))))
                .Append("|notifT=").Append(Encode(GetTextValue(GetFieldValue(notif, "notificationText"))))
                .Append("|rentS=").Append(Encode(GetTextValue(GetFieldValue(notif, "senderNameTextRentACabin"))))
                .Append("|rentT=").Append(Encode(GetTextValue(GetFieldValue(notif, "notificationTextRentACabin"))))
                .AppendLine();
        }

        private static string BuildActiveIndexList(List<GameObject> messages, ref Summary summary)
        {
            if (messages == null || messages.Count == 0)
            {
                return string.Empty;
            }

            summary.MessageCount += messages.Count;
            var builder = new StringBuilder();
            for (var i = 0; i < messages.Count; i++)
            {
                var message = messages[i];
                if (message == null || !message.activeSelf)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(',');
                }

                builder.Append(i.ToString(CultureInfo.InvariantCulture));
                summary.ActiveMessageCount++;
            }

            return builder.ToString();
        }

        private static void ApplyPhoneHeader(Phone phone, Dictionary<string, string> values)
        {
            phone.allowPhone = GetBool(values, "allow");
            var networkStatus = Mathf.Clamp(GetInt(values, "net", (int)phone.networkStatus), 0, 4);
            phone.networkStatus = (Phone.NetworkStatus)networkStatus;

            if (phone.noServiceObj != null && phone.networkBarsObj != null)
            {
                var noService = phone.networkStatus == Phone.NetworkStatus.NoService;
                phone.noServiceObj.SetActive(noService);
                phone.networkBarsObj.SetActive(!noService);
            }

            SetBarColor(GetFieldValue(phone, "bar1"), networkStatus >= 0 && networkStatus <= 3);
            SetBarColor(GetFieldValue(phone, "bar2"), networkStatus >= 1 && networkStatus <= 3);
            SetBarColor(GetFieldValue(phone, "bar3"), networkStatus >= 2 && networkStatus <= 3);
            SetBarColor(GetFieldValue(phone, "bar4"), networkStatus >= 3 && networkStatus <= 3);

            var notif = GetFieldValue(phone, "notifSystem");
            if (notif == null)
            {
                return;
            }

            SetTextValue(GetFieldValue(notif, "senderNameText"), Decode(GetString(values, "notifS", string.Empty)));
            SetTextValue(GetFieldValue(notif, "notificationText"), Decode(GetString(values, "notifT", string.Empty)));
            SetTextValue(GetFieldValue(notif, "senderNameTextRentACabin"), Decode(GetString(values, "rentS", string.Empty)));
            SetTextValue(GetFieldValue(notif, "notificationTextRentACabin"), Decode(GetString(values, "rentT", string.Empty)));
        }

        private static void SetBarColor(object image, bool high)
        {
            if (image == null)
            {
                return;
            }

            var color = Color.white;
            color.a = high ? 1f : 0.1f;
            var property = image.GetType().GetProperty("color");
            if (property != null && property.CanWrite)
            {
                property.SetValue(image, color, null);
                return;
            }

            var field = image.GetType().GetField("color");
            if (field != null)
            {
                field.SetValue(image, color);
            }
        }

        private static Phone ResolvePhone()
        {
            var activeScene = SceneManager.GetActiveScene();
            var phones = Resources.FindObjectsOfTypeAll<Phone>();
            for (var i = 0; i < phones.Length; i++)
            {
                var phone = phones[i];
                if (phone == null || phone.gameObject == null)
                {
                    continue;
                }

                var scene = phone.gameObject.scene;
                if (scene.IsValid() && scene == activeScene)
                {
                    return phone;
                }
            }

            return null;
        }

        private static List<Character> GetCharacters(object notifSystem)
        {
            return GetFieldValue(notifSystem, "characters") as List<Character>;
        }

        private static object GetFieldValue(object target, string fieldName)
        {
            if (target == null || string.IsNullOrEmpty(fieldName))
            {
                return null;
            }

            var field = target.GetType().GetField(fieldName);
            return field != null ? field.GetValue(target) : null;
        }

        private static string GetTextValue(object textComponent)
        {
            if (textComponent == null)
            {
                return string.Empty;
            }

            var property = textComponent.GetType().GetProperty("text");
            var value = property != null ? property.GetValue(textComponent, null) as string : null;
            return value ?? string.Empty;
        }

        private static void SetTextValue(object textComponent, string value)
        {
            if (textComponent == null)
            {
                return;
            }

            var property = textComponent.GetType().GetProperty("text");
            if (property != null && property.CanWrite)
            {
                property.SetValue(textComponent, value ?? string.Empty, null);
            }
        }

        private static void TryBringCharacterPanelToFront(Character character)
        {
            if (character == null || character.characterPanel == null)
            {
                return;
            }

            var transform = character.characterPanel.transform;
            if (transform != null && transform.parent != null)
            {
                transform.SetAsLastSibling();
            }
        }

        private static Dictionary<string, string> ParseLine(string line)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            var parts = line.Split('|');
            for (var i = 1; i < parts.Length; i++)
            {
                var part = parts[i];
                var eq = part.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                values[part.Substring(0, eq)] = part.Substring(eq + 1);
            }

            return values;
        }

        private static HashSet<int> ParseIndexSet(string value)
        {
            var indexes = new HashSet<int>();
            if (string.IsNullOrEmpty(value))
            {
                return indexes;
            }

            var parts = value.Split(',');
            for (var i = 0; i < parts.Length; i++)
            {
                int parsed;
                if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) && parsed >= 0)
                {
                    indexes.Add(parsed);
                }
            }

            return indexes;
        }

        private static bool GetBool(Dictionary<string, string> values, string key)
        {
            return GetInt(values, key, 0) != 0;
        }

        private static int GetInt(Dictionary<string, string> values, string key, int fallback)
        {
            string value;
            int parsed;
            if (values.TryGetValue(key, out value) &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static string GetString(Dictionary<string, string> values, string key, string fallback)
        {
            string value;
            return values.TryGetValue(key, out value) ? value : fallback;
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

        private static int StableHash(string text)
        {
            unchecked
            {
                var hash = 17;
                for (var i = 0; i < text.Length; i++)
                {
                    hash = hash * 31 + text[i];
                }

                return hash;
            }
        }
    }
}
