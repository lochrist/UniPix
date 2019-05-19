using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace UniPix
{
    public static class UnixPixCommands
    {
        public static Image LoadPix(string path)
        {
            var img = AssetDatabase.LoadAssetAtPath<Image>(path);
            if (img == null)
            {
                EditorPrefs.SetString(PixEditor.Prefs.kCurrentImg, "");
                return null;
            }

            EditorPrefs.SetString(PixEditor.Prefs.kCurrentImg, path);
            return img;
        }

        public static Image CreatePix(int w, int h)
        {
            string path = EditorUtility.SaveFilePanel(
                "Create UniPix",
                "Assets/", "Pix.unipix", "unipix");
            if (path == "")
            {
                return null;
            }

            path = FileUtil.GetProjectRelativePath(path);

            Image img = UniPixUtils.CreateImage(w, h, Color.clear);
            AssetDatabase.CreateAsset(img, path);
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(img);
            EditorPrefs.SetString(PixEditor.Prefs.kCurrentImg, AssetDatabase.GetAssetPath(img));
            return img;
        }

        public static void CreateLayer(SessionData session)
        {
            session.CurrentFrame.AddLayer(session.Image.Width, session.Image.Height);
        }
    }
}