using UnityEngine;

namespace WoodburySpectatorSync.UI
{
    public sealed class RemoteDialogueOverlay
    {
        private Texture2D _panelTexture;
        private Texture2D _accentTexture;
        private GUIStyle _speakerStyle;
        private GUIStyle _textStyle;
        private GUIStyle _choiceStyle;
        private GUIStyle _choiceSelectedStyle;

        public void Draw(string speaker, string text)
        {
            Draw(speaker, text, 0);
        }

        public void Draw(string speaker, string text, byte kind)
        {
            if (string.IsNullOrEmpty(text)) return;

            EnsureStyles();

            var isMenu = kind == 3;
            var lines = SplitLines(text);
            var width = Mathf.Min(isMenu ? 760f : 900f, Screen.width - 40f);
            var lineHeight = isMenu ? 24f : 22f;
            var titleHeight = string.IsNullOrEmpty(speaker) ? 0f : 22f;
            var height = Mathf.Clamp(26f + titleHeight + Mathf.Max(1, lines.Length) * lineHeight, isMenu ? 116f : 92f, Screen.height * 0.42f);
            var rect = new Rect((Screen.width - width) * 0.5f, Screen.height - height - 42f, width, height);

            var previousDepth = GUI.depth;
            var previousColor = GUI.color;
            GUI.depth = -1000;

            GUI.color = Color.white;
            GUI.DrawTexture(rect, _panelTexture);
            GUI.color = new Color(1f, 0.36f, 0.12f, 0.95f);
            GUI.DrawTexture(new Rect(rect.x + 14f, rect.y + 14f, 2f, rect.height - 28f), _accentTexture);

            var y = rect.y + 14f;
            var x = rect.x + 30f;
            var contentWidth = rect.width - 48f;
            if (!string.IsNullOrEmpty(speaker))
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(x, y, contentWidth, 22f), speaker, _speakerStyle);
                y += 24f;
            }

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i] ?? string.Empty;
                var selected = isMenu && line.TrimStart().StartsWith(">", System.StringComparison.Ordinal);
                var style = selected ? _choiceSelectedStyle : (isMenu ? _choiceStyle : _textStyle);
                var display = selected ? line.TrimStart().Substring(1).TrimStart() : line.TrimStart();
                GUI.color = selected ? new Color(1f, 0.86f, 0.62f, 1f) : Color.white;
                GUI.Label(new Rect(x, y, contentWidth, lineHeight), display, style);
                y += lineHeight;
                if (y > rect.yMax - 18f)
                {
                    break;
                }
            }

            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }

        private static string[] SplitLines(string text)
        {
            return (text ?? string.Empty).Replace("\r\n", "\n").Split('\n');
        }

        private void EnsureStyles()
        {
            if (_textStyle != null) return;

            _panelTexture = CreateTexture(new Color(0.015f, 0.014f, 0.013f, 0.82f));
            _accentTexture = CreateTexture(Color.white);

            _speakerStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = false,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(1f, 0.5f, 0.22f, 0.98f) }
            };

            _textStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = 16,
                clipping = TextClipping.Clip,
                normal = { textColor = new Color(0.96f, 0.96f, 0.92f, 0.98f) }
            };

            _choiceStyle = new GUIStyle(_textStyle)
            {
                fontSize = 15,
                normal = { textColor = new Color(0.72f, 0.72f, 0.68f, 0.94f) }
            };

            _choiceSelectedStyle = new GUIStyle(_choiceStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.86f, 0.62f, 1f) }
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
