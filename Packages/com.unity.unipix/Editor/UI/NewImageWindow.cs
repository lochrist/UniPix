using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public static class NewImagesWindow
    {
        static int m_X = -1;
        static int m_Y = -1;
        public static void OnGUI(PixDropDownWindow window, PixEditor editor, object context)
        {
            using (new PixUI.FieldWidthScope(45, 45))
            using (new GUILayout.VerticalScope(Styles.pixBox))
            {
                if (m_X == -1)
                    m_X = PixCommands.k_DefaultNewImageSize;
                if (m_Y == -1)
                    m_Y = PixCommands.k_DefaultNewImageSize;

                GUILayout.Label("New Image Size");
                m_X = EditorGUILayout.IntField("Width", m_X);
                m_X = Mathf.Clamp(m_X, 2, 512);
                m_Y = EditorGUILayout.IntField("Height", m_Y);
                m_Y = Mathf.Clamp(m_Y, 2, 512);

                if (GUILayout.Button("Create"))
                {
                    PixCommands.NewImage(editor.Session, m_X, m_Y);
                    window.Close();
                }
            }
        }

        public static Vector2 winSize = new Vector2(140, 85);
    }
}