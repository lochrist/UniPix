using System;
using System.Collections;
using System.Collections.Generic;
using UniPix;
using UnityEditor;
using UnityEngine;

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
        var frameRect = GUILayoutUtility.GetRect(PixEditor.Styles.kFramePreviewSize, PixEditor.Styles.kFramePreviewWidth, PixEditor.Styles.pixBox, GUILayout.Width(PixEditor.Styles.kFramePreviewSize), GUILayout.Height(PixEditor.Styles.kFramePreviewSize));
        DrawFrame(frameRect, tex, currentFrame);
        return frameRect;
    }

    public static void DrawFrame(Rect frameRect, Texture2D tex, bool currentFrame)
    {
        GUI.Box(frameRect, "", currentFrame ? PixEditor.Styles.selectedPixBox : PixEditor.Styles.pixBox);
        var texRect = new Rect(frameRect.xMin + PixEditor.Styles.kMargin, frameRect.yMin + PixEditor.Styles.kMargin, frameRect.width - 2 * PixEditor.Styles.kMargin, frameRect.height - 2 * PixEditor.Styles.kMargin);
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
