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

        public void Draw(string modeLabel, string status, string sceneName)
        {
            if (!_visible) return;

            var sb = new StringBuilder();
            sb.AppendLine("Woodbury Spectator Sync (MVP)");
            sb.AppendLine("Mode: " + modeLabel);
            sb.AppendLine("Status: " + status);
            sb.AppendLine("Scene: " + sceneName);
            if (!string.IsNullOrEmpty(_progressMarker))
            {
                sb.AppendLine("Progress: " + _progressMarker);
            }
            sb.AppendLine("Hotkeys: F6 host on/off, F7 connect, F8 overlay, F9 progress");

            var rect = new Rect(10, 10, 480, 140);
            GUI.Box(rect, sb.ToString());
        }
    }
}
