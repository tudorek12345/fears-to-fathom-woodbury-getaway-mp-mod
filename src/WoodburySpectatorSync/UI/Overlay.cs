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

        public void SetProgressMarker(string marker)
        {
            _progressMarker = marker ?? "";
        }

        public string Draw(string modeLabel, string status, string sceneName, string[] extraLines)
        {
            if (!_visible) return null;

            var text = BuildText(modeLabel, status, sceneName, extraLines, out var lineCount);

            var height = 20 + lineCount * 18;
            var rect = new Rect(10, 10, 560, height);
            GUI.Box(rect, text);
            return text;
        }

        public string BuildText(string modeLabel, string status, string sceneName, string[] extraLines, out int lineCount)
        {
            var sb = new StringBuilder();
            lineCount = 0;
            sb.AppendLine("Woodbury Spectator Sync (MVP)");
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
            sb.AppendLine("Hotkeys: F6 host on/off, F7 connect, F8 overlay, F9 progress");
            lineCount++;
            return sb.ToString();
        }
    }
}
