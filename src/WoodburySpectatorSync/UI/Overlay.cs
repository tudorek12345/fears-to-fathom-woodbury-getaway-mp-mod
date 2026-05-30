using System.Text;
using UnityEngine;
using WoodburySpectatorSync.Config;

namespace WoodburySpectatorSync.UI
{
    public sealed class Overlay
    {
        private readonly Settings _settings;
        private bool _visible;
        private string _progressMarker = "";
        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private Texture2D _panelTexture;

        public Overlay(Settings settings)
        {
            _settings = settings;
            _visible = settings.OverlayEnabled.Value;
        }

        public bool IsVisible => _visible;

        public void Toggle()
        {
            _visible = !_visible;
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
        }

        public void SetProgressMarker(string marker)
        {
            _progressMarker = marker ?? "";
        }

        public string Draw(string modeLabel, string status, string sceneName, string[] extraLines)
        {
            if (!_visible) return null;

            var text = BuildText(modeLabel, status, sceneName, extraLines, out var lineCount);
            return DrawText(text, lineCount);
        }

        public string DrawText(string text, int lineCount)
        {
            if (!_visible || string.IsNullOrEmpty(text)) return null;

            EnsureStyles();

            var width = Mathf.Min(460f, Mathf.Max(320f, Screen.width - 20f));
            var height = Mathf.Clamp(24f + lineCount * 15f, 80f, Mathf.Max(80f, Screen.height - 20f));
            var rect = new Rect(10f, 10f, width, height);
            GUI.Box(rect, GUIContent.none, _panelStyle);

            var newline = text.IndexOf('\n');
            var title = newline >= 0 ? text.Substring(0, newline).TrimEnd() : text.TrimEnd();
            var body = newline >= 0 ? text.Substring(newline + 1).TrimEnd() : string.Empty;

            GUI.Label(new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, 18f), title, _titleStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 29f, rect.width - 24f, rect.height - 36f), body, _bodyStyle);
            return text;
        }

        public string BuildText(string modeLabel, string status, string sceneName, string[] extraLines, out int lineCount)
        {
            var sb = new StringBuilder();
            lineCount = 0;
            sb.AppendLine("Woodbury Co-op Sync");
            lineCount++;
            sb.AppendLine("Mode: " + modeLabel);
            lineCount++;
            sb.AppendLine("Status: " + status);
            lineCount++;
            sb.AppendLine("Scene: " + sceneName);
            lineCount++;
            if (!string.IsNullOrEmpty(_progressMarker))
            {
                sb.AppendLine("Progress: " + _progressMarker);
                lineCount++;
            }
            if (extraLines != null)
            {
                foreach (var line in extraLines)
                {
                    if (string.IsNullOrEmpty(line)) continue;
                    sb.AppendLine(line);
                    lineCount++;
                }
            }
            sb.AppendLine("Hotkeys: F6 host on/off, F7 connect, F8 overlay, F9 progress, F10 dump, F11 menu");
            lineCount++;
            return sb.ToString();
        }

        private void EnsureStyles()
        {
            if (_panelStyle != null) return;

            _panelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _panelTexture.SetPixel(0, 0, new Color(0.02f, 0.025f, 0.03f, 0.72f));
            _panelTexture.Apply();

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _panelTexture },
                border = new RectOffset(8, 8, 8, 8),
                padding = new RectOffset(0, 0, 0, 0)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.92f, 0.96f, 1f, 1f) },
                clipping = TextClipping.Clip
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 12,
                normal = { textColor = new Color(0.84f, 0.88f, 0.92f, 1f) },
                wordWrap = false,
                clipping = TextClipping.Clip
            };
        }
    }
}
