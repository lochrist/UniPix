using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public static class GridSettingsWindow
    {
        public static void OnGUI(PixDropDownWindow window, PixEditor editor, object context)
        {
            using (new PixUI.FieldWidthScope(45, 45))
            using (new GUILayout.VerticalScope(Styles.pixBox))
            {
                GUILayout.Label("Grid");
                EditorGUI.BeginChangeCheck();
                GUILayout.Toggle(editor.Session.ShowGrid, "Show");
                if (EditorGUI.EndChangeCheck())
                {
                    PixCommands.ToggleGrid(editor.Session, editor);
                    editor.Repaint();
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Size {editor.Session.GridSize}", GUILayout.ExpandWidth(false));
                EditorGUI.BeginChangeCheck();
                var gridSize = (int)GUILayout.HorizontalSlider(editor.Session.GridSize, PixSession.k_MinGridSize, PixSession.k_MaxGridSize, GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    PixCommands.SetGridSize(editor.Session, editor, gridSize);
                    editor.Repaint();
                }
                GUILayout.EndHorizontal();

                EditorGUI.BeginChangeCheck();
                var color = EditorGUILayout.ColorField("Color", editor.Session.GridColor);
                if (EditorGUI.EndChangeCheck())
                {
                    PixCommands.SetGridColor(editor.Session, editor, color);
                    editor.Repaint();
                }
                GUILayout.Space(5);
                GUILayout.Label("Checkerboard");
                EditorGUI.BeginChangeCheck();
                editor.Session.ShowCheckerPattern = GUILayout.Toggle(editor.Session.ShowCheckerPattern, "Show");
                if (EditorGUI.EndChangeCheck())
                {
                    editor.Repaint();
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Size {editor.Session.CheckPatternSize}", GUILayout.ExpandWidth(false));
                EditorGUI.BeginChangeCheck();
                editor.Session.CheckPatternSize = (int)GUILayout.HorizontalSlider(editor.Session.CheckPatternSize, PixSession.k_MinCheckPatternSize, PixSession.k_MaxCheckPatternSize, GUILayout.ExpandWidth(true));
                if (EditorGUI.EndChangeCheck())
                {
                    editor.Repaint();
                }
                GUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
            }
        }

        public static Vector2 winSize = new Vector2(100, 140);
    }
}