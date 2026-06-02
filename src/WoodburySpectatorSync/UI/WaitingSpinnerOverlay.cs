using UnityEngine;

namespace WoodburySpectatorSync.UI
{
    public sealed class WaitingSpinnerOverlay
    {
        private Texture2D _panelTexture;
        private Texture2D _whiteTexture;
        private GUIStyle _titleStyle;
        private GUIStyle _detailStyle;

        public void Draw(string title, string detail)
        {
            if (string.IsNullOrEmpty(title)) return;

            EnsureStyles();

            var width = Mathf.Min(360f, Mathf.Max(280f, Screen.width - 40f));
            var height = string.IsNullOrEmpty(detail) ? 84f : 104f;
            var rect = new Rect((Screen.width - width) * 0.5f, Mathf.Max(72f, Screen.height * 0.18f), width, height);

            var previousDepth = GUI.depth;
            var previousColor = GUI.color;
            GUI.depth = -1100;

            GUI.color = Color.white;
            GUI.DrawTexture(rect, _panelTexture);
            GUI.color = new Color(1f, 0.56f, 0.1f, 0.96f);
            GUI.DrawTexture(new Rect(rect.x + 14f, rect.y + 14f, 2f, rect.height - 28f), _whiteTexture);
            GUI.color = Color.white;

            DrawSpinner(new Vector2(rect.x + 52f, rect.y + rect.height * 0.5f), 17f);
            GUI.Label(new Rect(rect.x + 86f, rect.y + 22f, rect.width - 104f, 24f), title, _titleStyle);
            if (!string.IsNullOrEmpty(detail))
            {
                GUI.Label(new Rect(rect.x + 86f, rect.y + 49f, rect.width - 104f, 34f), detail, _detailStyle);
            }

            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        private void DrawSpinner(Vector2 center, float radius)
        {
            var active = Mathf.FloorToInt(Time.realtimeSinceStartup * 10f) % 12;
            for (var i = 0; i < 12; i++)
            {
                var angle = ((i / 12f) * Mathf.PI * 2f) - (Mathf.PI * 0.5f);
                var x = center.x + Mathf.Cos(angle) * radius;
                var y = center.y + Mathf.Sin(angle) * radius;
                var distance = (i - active + 12) % 12;
                var alpha = Mathf.Clamp01(1f - distance * 0.075f);
                var red = i == active ? 1f : 0.95f;
                var green = i == active ? 0.18f : 0.52f;
                var blue = i == active ? 0.08f : 0.12f;
                GUI.color = new Color(red, green, blue, 0.2f + alpha * 0.72f);
                GUI.DrawTexture(new Rect(x - 3f, y - 3f, 6f, 6f), _whiteTexture);
            }
            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (_panelTexture != null) return;

            _panelTexture = CreateTexture(new Color(0.02f, 0.018f, 0.015f, 0.78f));
            _whiteTexture = CreateTexture(Color.white);

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.86f, 0.62f, 0.98f) },
                clipping = TextClipping.Clip
            };

            _detailStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 11,
                normal = { textColor = new Color(0.86f, 0.88f, 0.9f, 0.94f) },
                wordWrap = true,
                clipping = TextClipping.Clip
            };
        }

        private static Texture2D CreateTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
