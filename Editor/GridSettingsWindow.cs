using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public class GridSettingsWindow : EditorWindow
    {
        void OnEnable()
        {
        }

        [UsedImplicitly]
        internal void OnDestroy()
        {
            s_CloseTime = EditorApplication.timeSinceStartup;
        }

        static PixEditor s_Editor;
        static double s_CloseTime;
        internal static bool canShow
        {
            get
            {
                if (EditorApplication.timeSinceStartup - s_CloseTime < 0.250)
                    return false;
                return true;
            }
        }

        void OnGUI()
        {
            using (new GUILayout.VerticalScope(PixEditor.Styles.pixBox))
            {
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                var oldFieldWidth = EditorGUIUtility.fieldWidth;
                EditorGUIUtility.labelWidth = 45;
                EditorGUIUtility.fieldWidth = 45;
                EditorGUI.BeginChangeCheck();
                s_Editor.Session.ShowGrid = GUILayout.Toggle(s_Editor.Session.ShowGrid, "Show");
                if (EditorGUI.EndChangeCheck())
                {
                    s_Editor.Repaint();
                }

                EditorGUI.BeginChangeCheck();
                s_Editor.Session.GridSize = Mathf.Clamp(EditorGUILayout.IntField("Size", s_Editor.Session.GridSize), 1, 5);
                if (EditorGUI.EndChangeCheck())
                {
                    s_Editor.Repaint();
                }

                EditorGUI.BeginChangeCheck();
                s_Editor.Session.GridColor = EditorGUILayout.ColorField("Color", s_Editor.Session.GridColor);
                if (EditorGUI.EndChangeCheck())
                {
                    s_Editor.Repaint();
                }
                EditorGUIUtility.labelWidth = oldLabelWidth;
                EditorGUIUtility.fieldWidth = oldFieldWidth;
                GUILayout.FlexibleSpace();
            }
        }

        public static bool ShowAtPosition(PixEditor editor, Rect rect)
        {
            var screenPos = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
            var screenRect = new Rect(screenPos, rect.size);
            var gridSettings = ScriptableObject.CreateInstance<GridSettingsWindow>();
            s_Editor = editor;
            gridSettings.ShowAsDropDown(screenRect, new Vector2(100, 75));
            return true;
        }
    }
}