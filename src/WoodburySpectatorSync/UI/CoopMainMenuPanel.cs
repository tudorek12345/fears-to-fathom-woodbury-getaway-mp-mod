using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using WoodburySpectatorSync.Coop;
using WoodburySpectatorSync.Config;

namespace WoodburySpectatorSync.UI
{
    public sealed class CoopMainMenuPanel
    {
        private readonly Settings _settings;
        private bool _manualVisible;
        private Rect _rect = new Rect(24f, 120f, 360f, 430f);
        private string _hostIpText;
        private string _hostPortText;
        private string _udpPortText;
        private string _displayNameText;
        private string _avatarIdText;
        private string _avatarScaleText;
        private string _avatarYOffsetText;
        private string _steamworksAppIdText;
        private GUIStyle _windowStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _smallButtonStyle;
        private GUIStyle _fieldStyle;
        private Texture2D _panelTexture;

        public CoopMainMenuPanel(Settings settings)
        {
            _settings = settings;
            RefreshTextFromSettings();
        }

        public bool IsVisible
        {
            get
            {
                if (_settings == null || _settings.CoopMenuEnabled == null || !_settings.CoopMenuEnabled.Value)
                {
                    return _manualVisible;
                }

                return _manualVisible || IsMainMenu();
            }
        }

        public void Toggle()
        {
            _manualVisible = !_manualVisible;
            RefreshTextFromSettings();
        }

        public void Draw(CoopServer server, CoopClient client, Overlay overlay, Action<string> sessionWrite)
        {
            if (!IsVisible) return;

            EnsureStyles();
            UnlockCursorForMenu();
            _rect.width = Mathf.Min(380f, Mathf.Max(320f, Screen.width - 48f));
            _rect.height = Mathf.Min(510f, Mathf.Max(390f, Screen.height - 70f));
            _rect = GUI.Window(44270, _rect, id => DrawWindow(id, server, client, overlay, sessionWrite), "Woodbury Co-op", _windowStyle);
        }

        private void DrawWindow(int id, CoopServer server, CoopClient client, Overlay overlay, Action<string> sessionWrite)
        {
            GUILayout.BeginVertical();
            GUILayout.Space(4f);
            GUILayout.Label("Session", _headerStyle);
            DrawModeButtons(sessionWrite);
            GUILayout.Space(6f);

            GUILayout.Label("Connection", _headerStyle);
            DrawTextRow("Host IP", ref _hostIpText, 140f);
            DrawTextRow("TCP Port", ref _hostPortText, 80f);
            DrawTextRow("UDP Port", ref _udpPortText, 80f);
            DrawBoolRow("UDP", _settings.UdpEnabled, sessionWrite);
            DrawBoolRow("Voice", _settings.CoopVoiceChatEnabled, sessionWrite);
            DrawBoolRow("Footsteps", _settings.CoopFootstepSyncEnabled, sessionWrite);
            DrawSteamworksAppIdRow(sessionWrite);
            if (_settings.SteamworksAppIdMode != null &&
                _settings.SteamworksAppIdMode.Value == SteamworksAppIdMode.Custom)
            {
                DrawTextRow("App ID", ref _steamworksAppIdText, 96f);
            }
            DrawBoolRow("Auto host", _settings.CoopAutoStartHost, sessionWrite);
            DrawBoolRow("Auto connect", _settings.CoopAutoConnectClient, sessionWrite);
            DrawActionButtons(server, client, sessionWrite);
            GUILayout.Space(6f);

            GUILayout.Label("Player", _headerStyle);
            DrawTextRow("Name", ref _displayNameText, 190f);
            DrawBoolRow("Local player", _settings.CoopUseLocalPlayer, sessionWrite);
            DrawBoolRow("Route clicks", _settings.CoopRouteInteractions, sessionWrite);
            DrawAvatarSourceButtons(sessionWrite);
            DrawTextRow("Avatar ID", ref _avatarIdText, 190f);
            DrawTextRow("Scale", ref _avatarScaleText, 80f);
            DrawTextRow("Y offset", ref _avatarYOffsetText, 80f);
            GUILayout.Space(6f);

            GUILayout.Label("Debug", _headerStyle);
            DrawOverlayRow(overlay, sessionWrite);
            DrawBoolRow("Scene dump", _settings.SceneDiscoveryDump, sessionWrite);
            DrawBoolRow("Dump crawler", _settings.SceneDiscoveryDumpCrawler, sessionWrite);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply", _buttonStyle, GUILayout.Height(28f)))
            {
                ApplyTextSettings(sessionWrite);
            }
            if (GUILayout.Button("Reload", _buttonStyle, GUILayout.Height(28f)))
            {
                RefreshTextFromSettings();
                sessionWrite?.Invoke("Co-op menu: reloaded values from config");
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("F11 toggles this panel", _labelStyle);
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, _rect.width, 24f));
        }

        private void DrawModeButtons(Action<string> sessionWrite)
        {
            GUILayout.BeginHorizontal();
            DrawModeButton("Host", Mode.CoopHost, sessionWrite);
            DrawModeButton("Client", Mode.CoopClient, sessionWrite);
            DrawModeButton("Spectator", Mode.Spectator, sessionWrite);
            GUILayout.EndHorizontal();
        }

        private void DrawModeButton(string label, Mode mode, Action<string> sessionWrite)
        {
            var previous = GUI.backgroundColor;
            if (_settings.ModeSetting.Value == mode) GUI.backgroundColor = new Color(1f, 0.35f, 0.16f, 1f);
            if (GUILayout.Button(label, _smallButtonStyle, GUILayout.Height(26f)))
            {
                if (_settings.ModeSetting.Value != mode)
                {
                    _settings.ModeSetting.Value = mode;
                    sessionWrite?.Invoke("Co-op menu: Mode=" + mode);
                }
            }
            GUI.backgroundColor = previous;
        }

        private void DrawAvatarSourceButtons(Action<string> sessionWrite)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Avatar", _labelStyle, GUILayout.Width(86f));
            DrawAvatarSourceButton("Auto", RemotePlayerAvatarSource.Auto, sessionWrite);
            DrawAvatarSourceButton("Model", RemotePlayerAvatarSource.GameModel, sessionWrite);
            DrawAvatarSourceButton("Bundle", RemotePlayerAvatarSource.AssetBundle, sessionWrite);
            DrawAvatarSourceButton("Capsule", RemotePlayerAvatarSource.Capsule, sessionWrite);
            GUILayout.EndHorizontal();
        }

        private void DrawAvatarSourceButton(string label, RemotePlayerAvatarSource source, Action<string> sessionWrite)
        {
            var previous = GUI.backgroundColor;
            if (_settings.CoopRemotePlayerAvatarSource.Value == source) GUI.backgroundColor = new Color(1f, 0.35f, 0.16f, 1f);
            if (GUILayout.Button(label, _smallButtonStyle, GUILayout.Height(24f)))
            {
                if (_settings.CoopRemotePlayerAvatarSource.Value != source)
                {
                    _settings.CoopRemotePlayerAvatarSource.Value = source;
                    sessionWrite?.Invoke("Co-op menu: RemotePlayerAvatarSource=" + source);
                }
            }
            GUI.backgroundColor = previous;
        }

        private void DrawSteamworksAppIdRow(Action<string> sessionWrite)
        {
            if (_settings.SteamworksAppIdMode == null) return;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Steam", _labelStyle, GUILayout.Width(86f));
            DrawSteamworksAppIdButton("LAN/Local", SteamworksAppIdMode.Disabled, sessionWrite);
            DrawSteamworksAppIdButton("Spacewar", SteamworksAppIdMode.SteamworksTestApp, sessionWrite);
            DrawSteamworksAppIdButton("Custom", SteamworksAppIdMode.Custom, sessionWrite);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Space(90f);
            GUILayout.Label("direct/Steam default: Spacewar; local pair uses LAN/Local", _labelStyle, GUILayout.Width(270f));
            GUILayout.EndHorizontal();
        }

        private void DrawSteamworksAppIdButton(string label, SteamworksAppIdMode mode, Action<string> sessionWrite)
        {
            var previous = GUI.backgroundColor;
            if (_settings.SteamworksAppIdMode.Value == mode) GUI.backgroundColor = new Color(1f, 0.35f, 0.16f, 1f);
            if (GUILayout.Button(label, _smallButtonStyle, GUILayout.Height(24f)))
            {
                if (_settings.SteamworksAppIdMode.Value != mode)
                {
                    _settings.SteamworksAppIdMode.Value = mode;
                    sessionWrite?.Invoke("Co-op menu: Steamworks AppIdMode=" + mode + " (restart required)");
                }
            }
            GUI.backgroundColor = previous;
        }

        private void DrawActionButtons(CoopServer server, CoopClient client, Action<string> sessionWrite)
        {
            GUILayout.BeginHorizontal();
            var hostRunning = server != null && server.IsRunning;
            var clientConnected = client != null && client.IsConnected;
            if (GUILayout.Button(hostRunning ? "Stop Host" : "Start Host", _buttonStyle, GUILayout.Height(28f)))
            {
                ApplyTextSettings(sessionWrite);
                if (server != null)
                {
                    if (server.IsRunning) server.Stop(); else server.Start();
                    sessionWrite?.Invoke("Co-op menu: host " + (server.IsRunning ? "started" : "stopped"));
                }
            }
            if (GUILayout.Button(clientConnected ? "Disconnect" : "Connect", _buttonStyle, GUILayout.Height(28f)))
            {
                ApplyTextSettings(sessionWrite);
                if (client != null)
                {
                    if (client.IsConnected) client.Disconnect(); else client.Connect();
                    sessionWrite?.Invoke("Co-op menu: client " + (client.IsConnected ? "connected" : "connecting"));
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawTextRow(string label, ref string value, float width)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(86f));
            value = GUILayout.TextField(value ?? string.Empty, _fieldStyle, GUILayout.Width(width));
            GUILayout.EndHorizontal();
        }

        private void DrawBoolRow(string label, BepInEx.Configuration.ConfigEntry<bool> entry, Action<string> sessionWrite)
        {
            if (entry == null) return;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, _labelStyle, GUILayout.Width(86f));
            var next = GUILayout.Toggle(entry.Value, entry.Value ? "on" : "off", _buttonStyle, GUILayout.Width(74f), GUILayout.Height(24f));
            if (next != entry.Value)
            {
                entry.Value = next;
                sessionWrite?.Invoke("Co-op menu: " + entry.Definition.Key + "=" + next);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawOverlayRow(Overlay overlay, Action<string> sessionWrite)
        {
            if (_settings.OverlayEnabled == null) return;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Overlay", _labelStyle, GUILayout.Width(86f));
            var current = overlay != null ? overlay.IsVisible : _settings.OverlayEnabled.Value;
            var next = GUILayout.Toggle(current, current ? "on" : "off", _buttonStyle, GUILayout.Width(74f), GUILayout.Height(24f));
            if (next != current)
            {
                _settings.OverlayEnabled.Value = next;
                overlay?.SetVisible(next);
                sessionWrite?.Invoke("Co-op menu: OverlayEnabled=" + next);
            }
            GUILayout.EndHorizontal();
        }

        private void ApplyTextSettings(Action<string> sessionWrite)
        {
            _settings.SpectatorHostIP.Value = (_hostIpText ?? string.Empty).Trim();
            _settings.CoopDisplayName.Value = (_displayNameText ?? string.Empty).Trim();
            _settings.CoopRemotePlayerAvatarId.Value = (_avatarIdText ?? string.Empty).Trim();

            if (int.TryParse(_hostPortText, out var hostPort))
            {
                _settings.HostPort.Value = Mathf.Clamp(hostPort, 1, 65535);
            }

            if (int.TryParse(_udpPortText, out var udpPort))
            {
                _settings.UdpPort.Value = Mathf.Clamp(udpPort, 1, 65535);
            }

            if (float.TryParse(_avatarScaleText, out var avatarScale))
            {
                _settings.CoopRemotePlayerAvatarScale.Value = Mathf.Clamp(avatarScale, 0.05f, 5f);
            }

            if (float.TryParse(_avatarYOffsetText, out var avatarYOffset))
            {
                _settings.CoopRemotePlayerAvatarYOffset.Value = Mathf.Clamp(avatarYOffset, -3f, 3f);
            }

            if (_settings.SteamworksCustomAppId != null &&
                uint.TryParse(_steamworksAppIdText, out var steamworksAppId))
            {
                _settings.SteamworksCustomAppId.Value = steamworksAppId;
            }

            RefreshTextFromSettings();
            sessionWrite?.Invoke("Co-op menu: applied settings");
        }

        private void RefreshTextFromSettings()
        {
            if (_settings == null) return;
            _hostIpText = _settings.SpectatorHostIP != null ? _settings.SpectatorHostIP.Value : "127.0.0.1";
            _hostPortText = _settings.HostPort != null ? _settings.HostPort.Value.ToString() : "27055";
            _udpPortText = _settings.UdpPort != null ? _settings.UdpPort.Value.ToString() : "27056";
            _displayNameText = _settings.CoopDisplayName != null ? _settings.CoopDisplayName.Value : string.Empty;
            _avatarIdText = _settings.CoopRemotePlayerAvatarId != null ? _settings.CoopRemotePlayerAvatarId.Value : "woodbury_scene_auto";
            _avatarScaleText = _settings.CoopRemotePlayerAvatarScale != null ? _settings.CoopRemotePlayerAvatarScale.Value.ToString("0.###") : "1";
            _avatarYOffsetText = _settings.CoopRemotePlayerAvatarYOffset != null ? _settings.CoopRemotePlayerAvatarYOffset.Value.ToString("0.###") : "0";
            _steamworksAppIdText = _settings.SteamworksCustomAppId != null ? _settings.SteamworksCustomAppId.Value.ToString() : "0";
        }

        private static bool IsMainMenu()
        {
            return string.Equals(SceneManager.GetActiveScene().name, "MainMenu", StringComparison.OrdinalIgnoreCase);
        }

        private static void UnlockCursorForMenu()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null) return;

            _panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _panelTexture.SetPixel(0, 0, new Color(0.015f, 0.017f, 0.02f, 0.88f));
            _panelTexture.Apply();

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                normal = { background = _panelTexture, textColor = new Color(0.95f, 0.95f, 0.9f, 1f) },
                padding = new RectOffset(12, 12, 24, 12),
                border = new RectOffset(8, 8, 20, 8),
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.32f, 0.12f, 1f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.88f, 0.88f, 0.82f, 1f) },
                clipping = TextClipping.Clip
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                hover = { textColor = Color.white },
                active = { textColor = Color.white }
            };

            _smallButtonStyle = new GUIStyle(_buttonStyle)
            {
                fontSize = 11
            };

            _fieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 12,
                normal = { textColor = Color.white },
                focused = { textColor = Color.white }
            };
        }
    }
}
