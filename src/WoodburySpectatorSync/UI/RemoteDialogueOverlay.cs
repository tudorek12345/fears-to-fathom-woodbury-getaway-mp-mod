using UnityEngine;

namespace WoodburySpectatorSync.UI
{
    public sealed class RemoteDialogueOverlay
    {
        private GUIStyle _textStyle;

        public void Draw(string speaker, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (_textStyle == null)
            {
                _textStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = true,
                    fontSize = 16
                };
            }

            var label = string.IsNullOrEmpty(speaker) ? text : (speaker + ": " + text);
            var width = Mathf.Min(900f, Screen.width - 40f);
            var height = 110f;
            var rect = new Rect((Screen.width - width) / 2f, Screen.height - height - 40f, width, height);
            GUI.Box(rect, string.Empty);
            var textRect = new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, rect.height - 24f);
            GUI.Label(textRect, label, _textStyle);
        }
    }
}
