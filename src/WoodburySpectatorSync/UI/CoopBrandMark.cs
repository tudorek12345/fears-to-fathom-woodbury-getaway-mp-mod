using UnityEngine;

namespace WoodburySpectatorSync.UI
{
    public sealed class CoopBrandMark
    {
        private GUIStyle _f2fStyle;
        private GUIStyle _mainStyle;
        private GUIStyle _accentStyle;
        private GUIStyle _shadowStyle;
        private Texture2D _panelTexture;
        private Texture2D _lineTexture;
        private Texture2D _redTexture;

        public void Draw()
        {
            EnsureStyles();

            var width = Mathf.Min(330f, Mathf.Max(250f, Screen.width - 24f));
            var height = 34f;
            var x = (Screen.width - width) * 0.5f;
            var y = Mathf.Max(8f, Screen.height - height - 12f);
            var rect = new Rect(x, y, width, height);

            GUI.depth = -900;
            GUI.DrawTexture(rect, _panelTexture);
            GUI.DrawTexture(new Rect(rect.x + 10f, rect.y + 5f, 2f, rect.height - 10f), _lineTexture);
            GUI.DrawTexture(new Rect(rect.x + rect.width - 28f, rect.y + 12f, 10f, 10f), _redTexture);

            DrawLabel(new Rect(rect.x + 22f, rect.y + 7f, 44f, 20f), "F2F", _f2fStyle);
            DrawLabel(new Rect(rect.x + 66f, rect.y + 7f, 126f, 20f), "WOODBURY", _mainStyle);
            DrawLabel(new Rect(rect.x + 186f, rect.y + 7f, 84f, 20f), "CO:OP", _accentStyle);
        }

        private void DrawLabel(Rect rect, string text, GUIStyle style)
        {
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, _shadowStyle);
            GUI.Label(rect, text, style);
        }

        private void EnsureStyles()
        {
            if (_panelTexture != null) return;

            _panelTexture = CreateTexture(new Color(0.025f, 0.022f, 0.018f, 0.56f));
            _lineTexture = CreateTexture(new Color(1f, 0.61f, 0.12f, 0.92f));
            _redTexture = CreateTexture(new Color(0.85f, 0.08f, 0.04f, 0.9f));

            _shadowStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0f, 0f, 0f, 0.72f) },
                clipping = TextClipping.Clip
            };

            _f2fStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.72f, 0.18f, 0.96f) },
                clipping = TextClipping.Clip
            };

            _mainStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.86f, 0.76f, 0.94f) },
                clipping = TextClipping.Clip
            };

            _accentStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.24f, 0.12f, 0.96f) },
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
