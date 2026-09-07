using UnityEngine;

namespace Interaction.Editor
{
    internal static class SerumOverlayDraw
    {
        internal static readonly Color IdleBg = new Color(0.18f, 0.18f, 0.18f, 1f);
        internal static readonly Color ActiveBg = new Color(0.15f, 0.85f, 0.28f, 1f);
        internal static readonly Color IdleText = new Color(0.75f, 0.75f, 0.75f, 1f);
        internal static readonly Color MutedText = new Color(0.55f, 0.55f, 0.55f, 1f);

        static GUIStyle _keyStyle;

        static GUIStyle KeyStyle
        {
            get
            {
                if (_keyStyle != null)
                    return _keyStyle;

                _keyStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 11
                };
                _keyStyle.normal.background = Texture2D.whiteTexture;
                _keyStyle.hover.background = Texture2D.whiteTexture;
                _keyStyle.active.background = Texture2D.whiteTexture;
                return _keyStyle;
            }
        }

        internal static void DrawRow(string label, bool active, float width, float height = 24f)
        {
            Color previousBg = GUI.backgroundColor;
            GUI.backgroundColor = active ? ActiveBg : IdleBg;
            KeyStyle.normal.textColor = active ? Color.black : IdleText;
            KeyStyle.hover.textColor = KeyStyle.normal.textColor;
            KeyStyle.active.textColor = KeyStyle.normal.textColor;
            GUILayout.Box(label, KeyStyle, GUILayout.Width(width), GUILayout.Height(height));
            GUI.backgroundColor = previousBg;
        }

        internal static void DrawSource(string source)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                wordWrap = true,
                normal = { textColor = MutedText }
            };
            GUILayout.Label(source, style);
        }
    }
}
