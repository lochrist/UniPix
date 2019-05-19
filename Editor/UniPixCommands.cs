using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace UniPix
{
    public static class UniPixCommands
    {
        public static Image LoadPix()
        {
            string path = EditorUtility.OpenFilePanel(
                "Find Pix (.asset | .png | .jpg)",
                "Assets/",
                "Image Files;*.asset;*.jpg;*.png");

            if (path == null)
                return null;
            path = FileUtil.GetProjectRelativePath(path);
            if (path.EndsWith(".asset"))
            {
                return LoadPix(path);
            }

            return UniPixUtils.CreateImageFromTexture(path);
        }

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
                "Assets/", "Pix.asset", "asset");
            if (path == "")
            {
                return null;
            }

            path = FileUtil.GetProjectRelativePath(path);

            Image img = UniPixUtils.CreateImage(w, h, Color.clear);
            AssetDatabase.CreateAsset(img, path);
            EditorUtility.SetDirty(img);
            AssetDatabase.SaveAssets();
            EditorPrefs.SetString(PixEditor.Prefs.kCurrentImg, AssetDatabase.GetAssetPath(img));
            return img;
        }

        public static void SavePix(SessionData session)
        {
            if (session.Image == null)
                return;

            var assetPath = AssetDatabase.GetAssetPath(session.Image);
            if (string.IsNullOrEmpty(assetPath))
            {
                string path = EditorUtility.SaveFilePanel(
                    "Create UniPix",
                    "Assets/", "Pix.asset", "asset");
                if (path == "")
                {
                    return;
                }

                AssetDatabase.CreateAsset(session.Image, FileUtil.GetProjectRelativePath(path));
                EditorUtility.SetDirty(session.Image);
                assetPath = AssetDatabase.GetAssetPath(session.Image);
            }

            AssetDatabase.SaveAssets();
            EditorPrefs.SetString(PixEditor.Prefs.kCurrentImg, AssetDatabase.GetAssetPath(session.Image));
        }

        public static void CreateLayer(SessionData session)
        {
            session.CurrentFrame.AddLayer(session.Image.Width, session.Image.Height);
        }
    }
}