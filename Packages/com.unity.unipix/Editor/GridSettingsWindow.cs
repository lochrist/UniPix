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
            using (new PixUI.FieldWidthScope(45, 45))
            using (new GUILayout.VerticalScope(Styles.pixBox))
            {
                GUILayout.Label("Grid");
                EditorGUI.BeginChangeCheck();
                GUILayout.Toggle(s_Editor.Session.ShowGrid, "Show");
                if (EditorGUI.EndChangeCheck())
                {
                    PixCommands.ToggleGrid(s_Editor.Session, s_Editor);
                    s_Editor.Repaint();
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Size {s_Editor.Session.GridSize}", GUILayout.ExpandWidth(false));
                EditorGUI.BeginChangeCheck();
                var gridSize = (int)GUILayout.HorizontalSlider(s_Editor.Session.GridSize, PixSession.k_MinGridSize, PixSession.k_MaxGridSize, GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    PixCommands.SetGridSize(s_Editor.Session, s_Editor, gridSize);
                    s_Editor.Repaint();
                }
                GUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                var color = EditorGUILayout.ColorField("Color", s_Editor.Session.GridColor);
                if (EditorGUI.EndChangeCheck())
                {
                    PixCommands.SetGridColor(s_Editor.Session, s_Editor, color);
                    s_Editor.Repaint();
                }
                GUILayout.Space(5);
                GUILayout.Label("Checkerboard");
                EditorGUI.BeginChangeCheck();
                s_Editor.Session.ShowCheckerPattern = GUILayout.Toggle(s_Editor.Session.ShowCheckerPattern, "Show");
                if (EditorGUI.EndChangeCheck())
                {
                    s_Editor.Repaint();
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Size {s_Editor.Session.CheckPatternSize}", GUILayout.ExpandWidth(false));
                EditorGUI.BeginChangeCheck();
                s_Editor.Session.CheckPatternSize = (int)GUILayout.HorizontalSlider(s_Editor.Session.CheckPatternSize, PixSession.k_MinCheckPatternSize, PixSession.k_MaxCheckPatternSize, GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    s_Editor.Repaint();
                }
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
            }
        }

        public static bool ShowAtPosition(PixEditor editor, Rect rect)
        {
            var screenPos = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
            var screenRect = new Rect(screenPos, rect.size);
            var gridSettings = ScriptableObject.CreateInstance<GridSettingsWindow>();
            s_Editor = editor;
            gridSettings.ShowAsDropDown(screenRect, new Vector2(100, 140));
            return true;
        }
    }
}