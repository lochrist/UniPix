using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public class ExportWindow : EditorWindow
    {
        void OnEnable() { }

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
                GUILayout.Label("Export", PixEditor.Styles.layerHeader);

                if (GUILayout.Button("Export current frame"))
                {

                }

                if (GUILayout.Button("Export all frames"))
                {

                }

                if (GUILayout.Button("Export spritesheet"))
                {

                }
            }
        }

        public static bool ShowAtPosition(PixEditor editor, Rect rect)
        {
            var screenPos = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
            var screenRect = new Rect(screenPos, rect.size);
            var gridSettings = ScriptableObject.CreateInstance<ExportWindow>();
            s_Editor = editor;
            gridSettings.ShowAsDropDown(screenRect, new Vector2(200, 90));
            return true;
        }
    }
}