using System;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public static class PixUI
    {
        public static float Slider(string title, float value, float left, float right, GUIStyle sliderStyle = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, GUILayout.ExpandWidth(false));
            var result = GUILayout.HorizontalSlider(value, left, right, sliderStyle ?? GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            return result;
        }

        public static Rect LayoutFrameTile(Frame frame, bool currentFrame = false)
        {
            var tex = frame.Texture;
            var frameRect = GUILayoutUtility.GetRect(Styles.kFramePreviewSize, Styles.kFramePreviewWidth, Styles.pixBox, GUILayout.Width(Styles.kFramePreviewSize), GUILayout.Height(Styles.kFramePreviewSize));
            DrawFrame(frameRect, tex, currentFrame);
            return frameRect;
        }

        public static void DrawFrame(Rect frameRect, Texture2D tex, bool currentFrame)
        {
            GUI.Box(frameRect, "", currentFrame ? Styles.selectedPixBox : Styles.pixBox);
            var texRect = new Rect(frameRect.xMin + Styles.kMargin, frameRect.yMin + Styles.kMargin, frameRect.width - 2 * Styles.kMargin, frameRect.height - 2 * Styles.kMargin);
            GUI.DrawTexture(texRect, tex, ScaleMode.ScaleToFit);
        }

        public static Texture2D GetTransparentCheckerTexture()
        {
            if (EditorGUIUtility.isProSkin)
            {
                return EditorGUIUtility.LoadRequired("Previews/Textures/textureCheckerDark.png") as Texture2D;
            }

            return EditorGUIUtility.LoadRequired("Previews/Textures/textureChecker.png") as Texture2D;
        }

        public struct FieldWidthScope : IDisposable
        {
            float m_LabelWidth;
            float m_FieldWidth;

            public FieldWidthScope(float labelWidth, float fieldWidth = 0)
            {
                m_LabelWidth = EditorGUIUtility.labelWidth;
                m_FieldWidth = EditorGUIUtility.fieldWidth;

                EditorGUIUtility.labelWidth = labelWidth;
                if (fieldWidth != 0)
                    EditorGUIUtility.fieldWidth = fieldWidth;
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = m_LabelWidth;
                EditorGUIUtility.fieldWidth = m_FieldWidth;
            }
        }
    }
}