using UnityEngine;
using UnityEditor;
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

            session.IsImageDirty = false;

            UpdateImageTitle(session);

            session.CurrentFrameIndex = 0;
            session.CurrentLayerIndex = 0;
            
            session.PreviewFrameIndex = 0;

            session.Palette = new Palette();
            UniPixUtils.ExtractPaletteFrom(session.CurrentFrame, session.Palette.Colors);

            session.CurrentColor = session.Palette.Colors.Count > 0 ? session.Palette.Colors[0] : Color.black;
            session.SecondaryColor = session.Palette.Colors.Count > 1 ? session.Palette.Colors[1] : Color.white;

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
            session.IsImageDirty = false;
            UpdateImageTitle(session);
        }

        public static void SetCurrentFrame(SessionData session, int frameIndex)
        {
            session.CurrentFrameIndex = frameIndex;
        }

        public static void SetCurrentLayer(SessionData session, int layerIndex)
        {
            session.CurrentLayerIndex = layerIndex;
        }

        public static void CreateLayer(SessionData session)
        {
            session.CurrentFrame.AddLayer(session.CurrentLayerIndex + 1);
            session.CurrentLayerIndex = session.CurrentLayerIndex + 1;
            DirtyImage(session);
        }

        public static void CloneLayer(SessionData session)
        {
            var clonedLayer = session.CurrentFrame.AddLayer(session.CurrentLayerIndex + 1);
            var currentLayer = session.CurrentLayer;
            for (int i = 0; i < currentLayer.Pixels.Length; i++)
            {
                clonedLayer.Pixels[i] = currentLayer.Pixels[i];
            }
            session.CurrentLayerIndex = session.CurrentLayerIndex + 1;
            DirtyImage(session);
        }

        public static void DeleteLayer(SessionData session)
        {
            session.CurrentFrame.Layers.Remove(session.CurrentLayer);
            if (session.CurrentFrame.Layers.Count == 0)
            {
                session.CurrentFrame.AddLayer();
            }
            DirtyImage(session);
        }

        public static void SetLayerOpacity(SessionData session, int layerIndex, float opacity)
        {
            session.CurrentFrame.Layers[layerIndex].Opacity = opacity;
            DirtyImage(session);
        }

        public static void SetLayerVisibility(SessionData session, int layerIndex, bool isVisible)
        {
            session.CurrentFrame.Layers[layerIndex].Visible = isVisible;
            DirtyImage(session);
        }

        public static void SwapLayers(SessionData session, int layerIndex1, int layerIndex2)
        {
            var layer1 = session.CurrentFrame.Layers[layerIndex1];
            var layer2 = session.CurrentFrame.Layers[layerIndex2];
            session.CurrentFrame.Layers[layerIndex2] = layer1;
            session.CurrentFrame.Layers[layerIndex1] = layer2;

            if (session.CurrentLayerIndex == layerIndex1)
                session.CurrentLayerIndex = layerIndex2;
            else if (session.CurrentLayerIndex == layerIndex2)
                session.CurrentLayerIndex = layerIndex1;

            DirtyImage(session);
        }

        public static void AddPaletteColor(SessionData session, Color newColor)
        {
            session.Palette.Colors.Add(newColor);
        }

        public static void SetPixelsUnderBrush(SessionData session, Color color)
        {
            var brushRect = session.BrushRect;
            for (var y = brushRect.y; y < brushRect.yMax; ++y)
            {
                for (var x = brushRect.x; x < brushRect.xMax; ++x)
                {
                    var pixelIndex = session.ImgCoordToPixelIndex(x, y);
                    session.CurrentLayer.Pixels[pixelIndex] = color;
                }
            }
            DirtyImage(session);
        }

        private static void DirtyImage(SessionData session)
        {
            session.CurrentFrame.UpdateFrame();
            if (!session.IsImageDirty)
            {
                session.IsImageDirty = true;
                UpdateImageTitle(session);
            }
        }

        private static void UpdateImageTitle(SessionData session)
        {
            session.ImagePath = AssetDatabase.GetAssetPath(session.Image);
            if (string.IsNullOrEmpty(session.ImagePath))
            {
                session.ImageTitle = "*Untitled*";
            }
            else
            {
                session.ImageTitle = session.ImagePath;
                if (session.IsImageDirty)
                {
                    session.ImageTitle += "*";
                }
            }
        }
    }
}