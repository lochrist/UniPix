using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public class SyncWindow : EditorWindow
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
                GUILayout.Label("Sync", PixEditor.Styles.layerHeader);

                GUILayout.Label("Some frames do not have a source assets assigned. How do you want all those frames?", EditorStyles.helpBox);

                if (GUILayout.Button("Save separate images"))
                {

                }

                if (GUILayout.Button("Save as spritesheet"))
                {

                }
            }
        }

        public static bool ShowAtPosition(PixEditor editor, Rect rect)
        {
            var screenPos = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
            var screenRect = new Rect(screenPos, rect.size);
            var win = ScriptableObject.CreateInstance<SyncWindow>();
            s_Editor = editor;
            win.ShowAsDropDown(screenRect, new Vector2(200, 150));
            return true;
        }
    }
}