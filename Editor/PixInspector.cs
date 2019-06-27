using UnityEditor;
using UnityEngine;

namespace UniPix
{
    [CustomEditor(typeof(Image))]
    [CanEditMultipleObjects]
    public class ImageInspector : Editor
    {

        void OnEnable()
        {

        }

        public override void OnInspectorGUI()
        {
            Image img = (Image)target;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Edit"))
            {
                UniPixCommands.EditInPix(new Object[] { img });
            }
            if (GUILayout.Button("Edit (isolated)"))
            {
                PixRoleProvider.StartUniPixIsolated(AssetDatabase.GetAssetPath(img));
            }
            GUILayout.EndHorizontal();
            

            EditorGUILayout.IntField("Width", img.Width);
            EditorGUILayout.IntField("Height", img.Height);

            using (new UniPixUtils.FieldWidthScope(15, 45))
            {
                int frameIndex = 0;
                foreach (var frame in img.Frames)
                {
                    if (frame.SourceSprite != null)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(new GUIContent($"{frameIndex}"), frame.SourceSprite, typeof(Sprite), false);
                        UniPixUtils.LayoutFrameTile(frame);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label($"{frameIndex}");
                        UniPixUtils.LayoutFrameTile(frame);
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();
                    }
                    ++frameIndex;
                }
            }

        }
    }
}
