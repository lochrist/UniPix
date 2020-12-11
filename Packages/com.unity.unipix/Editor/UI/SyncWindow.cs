using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public static class SyncWindow
    {
        public static void OnGUI(PixDropDownWindow window, PixEditor editor, object context)
        {
            using (new GUILayout.VerticalScope(Styles.pixBox))
            {
                GUILayout.Label("Sync", Styles.layerHeader);

                GUILayout.Label("Some frames do not have a Source Sprite assigned. How do you want to save all those frames?", EditorStyles.helpBox);

                if (GUILayout.Button("Save separate images"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        PixIO.UpdateImageSourceSprites(editor.Session);
                        window.Close();
                    };
                }

                if (GUILayout.Button("Save as spritesheet"))
                {
                    EditorApplication.delayCall += () =>
                    {
                        PixIO.UpdateImageSourceSprites(editor.Session, true);
                        window.Close();
                    };
                }
            }
        }

        public static Vector2 winSize = new Vector2(200, 110);
    }
}