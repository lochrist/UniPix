using UnityEditor;
using UnityEngine;

namespace UniPix
{
    [CustomEditor(typeof(PixImage))]
    [CanEditMultipleObjects]
    public class ImageInspector : Editor
    {

        void OnEnable()
        {

        }

        public override void OnInspectorGUI()
        {
            PixImage img = (PixImage)target;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Edit"))
            {
                PixCommands.EditInPix(new Object[] { img });
            }
            if (GUILayout.Button("Edit (isolated)"))
            {
                PixRoleProvider.StartUniPixIsolated(img.Path);
            }
            GUILayout.EndHorizontal();
            
            EditorGUILayout.IntField("Width", img.Width);
            EditorGUILayout.IntField("Height", img.Height);

            using (new PixUI.FieldWidthScope(15, 45))
            {
                int frameIndex = 0;
                foreach (var frame in img.Frames)
                {
                    if (frame.SourceSprite != null)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(new GUIContent($"{frameIndex}"), frame.SourceSprite, typeof(Sprite), false);
                        PixUI.LayoutFrameTile(frame);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{frameIndex}");
                        PixUI.LayoutFrameTile(frame);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    ++frameIndex;
                }
            }

        }
    }
}
