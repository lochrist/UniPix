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
            using (new GUILayout.VerticalScope(Styles.pixBox))
            {
                GUILayout.Label("Export", Styles.layerHeader);

                var exportMode = -1;
                if (GUILayout.Button("Export current frame"))
                {
                    exportMode = 0;
                }

                if (GUILayout.Button("Export all frames"))
                {
                    exportMode = 1;
                }

                if (GUILayout.Button("Export to sprite sheet"))
                {
                    exportMode = 2;
                }

                if (exportMode != -1)
                {
                    EditorApplication.delayCall += () => DelayedExport(exportMode);
                    Close();
                }
            }
        }

        private static void DelayedExport(int mode)
        {
            var exportedFile = "";
            if (mode == 0)
            {
                var frames = PixIO.ExportFrames(s_Editor.Session, new[] { s_Editor.Session.CurrentFrame });
                exportedFile = frames != null && frames.Length > 0 ? frames[0] : null;
            }
            else if (mode == 1)
            {
                var frames = PixIO.ExportFrames(s_Editor.Session);
                exportedFile = frames != null && frames.Length > 0 ? frames[0] : null;
            }
            else
            {
                exportedFile = PixIO.ExportFramesToSpriteSheet(s_Editor.Session);
            }

            if (!string.IsNullOrEmpty(exportedFile))
            {
                EditorUtility.RevealInFinder(exportedFile);
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