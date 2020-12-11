using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public static class ExportWindow
    {
        public static void OnGUI(PixDropDownWindow window, PixEditor editor, object context)
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
                    EditorApplication.delayCall += () => DelayedExport(editor, exportMode);
                    window.Close();
                }
            }
        }

        private static void DelayedExport(PixEditor editor, int mode)
        {
            var exportedFile = "";
            if (mode == 0)
            {
                var frames = PixIO.ExportFrames(editor.Session, new[] { editor.Session.CurrentFrame });
                exportedFile = frames != null && frames.Length > 0 ? frames[0] : null;
            }
            else if (mode == 1)
            {
                var frames = PixIO.ExportFrames(editor.Session);
                exportedFile = frames != null && frames.Length > 0 ? frames[0] : null;
            }
            else
            {
                exportedFile = PixIO.ExportFramesToSpriteSheet(editor.Session);
            }

            if (!string.IsNullOrEmpty(exportedFile))
            {
                EditorUtility.RevealInFinder(exportedFile);
            }
        }

        public static Vector2 winSize = new Vector2(200, 90);
    }
}