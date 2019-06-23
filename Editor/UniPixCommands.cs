using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;

namespace UniPix
{
    public static class UniPixCommands
    {
        public static bool LoadPix(SessionData session)
        {
            var path = EditorUtility.OpenFilePanel(
                    "Find Pix (.asset | .png | .jpg)",
                    "Assets/",
                    "Image Files;*.unipix.asset;*.jpg;*.png");
            return LoadPix(session, path);
        }

        public static bool LoadPix(SessionData session, string path)
        {
            return LoadPix(session, new [] {path});
        }

        public static bool LoadPix(SessionData session, string[] paths)
        {
            if (paths.Length > 0)
            {
                var processedPaths = paths.Select(p =>
                {
                    if (Path.IsPathRooted(p))
                    {
                        return FileUtil.GetProjectRelativePath(p);
                    }

                    return p;
                }).Where(p => File.Exists(p)).ToArray();

                if (processedPaths.Length == 1 && processedPaths[0].EndsWith(".asset"))
                {
                    session.Image = LoadPix(processedPaths[0]);
                }
                else
                {
                    session.Image = UniPixUtils.CreateImageFromTexture(processedPaths);
                }
            }

            if (session.Image == null)
            {
                session.Image = UniPixUtils.CreateImage(32, 32, Color.clear);
            }

            session.CurrentFrameIndex = 0;
            session.CurrentLayerIndex = 0;

            session.palette = new Palette();
            UniPixUtils.ExtractPaletteFrom(session.CurrentFrame, session.palette.Colors);

            session.CurrentColor = session.palette.Colors.Count > 0 ? session.palette.Colors[0] : Color.black;
            session.SecondaryColor = session.palette.Colors.Count > 1 ? session.palette.Colors[1] : Color.white;

            return true;
        }

        internal static Image LoadPix(string path)
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

        public static void CreatePix(SessionData session, int w, int h)
        {
            string path = EditorUtility.SaveFilePanel(
                "Create UniPix",
                "Assets/", "Pix.unipix", "asset");
            if (path == "")
            {
                return;
            }

            path = FileUtil.GetProjectRelativePath(path);
            Image img = UniPixUtils.CreateImage(w, h, Color.clear);
            AssetDatabase.CreateAsset(img, path);
            EditorUtility.SetDirty(img);
            AssetDatabase.SaveAssets();

            LoadPix(session, AssetDatabase.GetAssetPath(img));
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
                    "Assets/", "Pix.unipix", "asset");
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
            session.CurrentFrame.AddLayer();
        }

        public static void SetPixel(SessionData session, int pixelIndex, Color color)
        {
            session.CurrentLayer.Pixels[pixelIndex] = color;
        }

        public static void SetCurrentFrame(SessionData session, int frameIndex)
        {
            session.CurrentFrameIndex = frameIndex;
        }

        public static void SetCurrentLayer(SessionData session, int layerIndex)
        {
            session.CurrentLayerIndex = layerIndex;
        }

        public static void SetPixelsUnderBrush(SessionData session, Color color)
        {
            var brushRect = session.BrushRect;
            for (var y = brushRect.y; y < brushRect.yMax; ++y)
            {
                for (var x = brushRect.x; x < brushRect.xMax; ++x)
                {
                    var pixelIndex = session.ImgCoordToPixelIndex(x, y);
                    SetPixel(session, pixelIndex, color);
                }
            }
        }
    }
}