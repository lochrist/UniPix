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

            OnNewFrame(session);

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

        #region UI
        public static void SetCurrentFrame(SessionData session, int frameIndex)
        {
            session.CurrentFrameIndex = frameIndex;
            OnNewFrame(session);
        }

        public static void SetCurrentLayer(SessionData session, int layerIndex)
        {
            session.CurrentLayerIndex = layerIndex;
        }
        #endregion

        // TODO: should it be part of the model?
        public static void AddPaletteColor(SessionData session, Color newColor)
        {
            session.Palette.Colors.Add(newColor);
        }

        #region ModelChanged
        public static void NewFrame(SessionData session)
        {
            var newFrame = session.Image.AddFrame();
            newFrame.AddLayer();
            session.CurrentFrameIndex = session.Image.Frames.Count - 1;
            DirtyImage(session);
        }

        public static void DeleteFrame(SessionData session, int frameIndex)
        {
            session.Image.Frames.RemoveAt(frameIndex);
            if (session.Image.Frames.Count == 0)
            {
                var newFrame = session.Image.AddFrame();
                newFrame.AddLayer();
            }
            if (session.CurrentFrameIndex > session.Image.Frames.Count - 1)
            {
                session.CurrentFrameIndex = session.Image.Frames.Count - 1;
            }
            
            DirtyImage(session);
        }

        public static void CloneFrame(SessionData session, int frameIndex)
        {
            var toClone = session.Image.Frames[frameIndex];
            var clone = session.Image.AddFrame(frameIndex + 1);
            for (int i = 0; i < toClone.Layers.Count; i++)
            {
                if (i >= clone.Layers.Count)
                {
                    clone.AddLayer();
                }

                var srcLayer = toClone.Layers[i];
                var dstLayer = clone.Layers[i];
                CopyLayer(srcLayer, dstLayer);
            }

            session.CurrentFrameIndex = frameIndex + 1;
            DirtyImage(session);
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
            var clonedLayerName = clonedLayer.Name;
            var currentLayer = session.CurrentLayer;
            CopyLayer(currentLayer, clonedLayer);
            clonedLayer.Name = clonedLayerName;

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

            if (session.CurrentLayerIndex >= session.CurrentFrame.Layers.Count)
                session.CurrentLayerIndex = session.CurrentFrame.Layers.Count - 1;
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

        public static void MergeLayers(SessionData session, int src1, int dst2)
        {
            var srcLayer1 = session.CurrentFrame.Layers[src1];
            var dstLayer2 = session.CurrentFrame.Layers[dst2];
            
            UniPixUtils.Blend(srcLayer1, dstLayer2, dstLayer2);

            session.CurrentFrame.Layers.Remove(srcLayer1);

            if (session.CurrentLayerIndex == src1)
                session.CurrentLayerIndex = dst2;

            DirtyImage(session);
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
        #endregion

        #region Impl

        private static void CopyLayer(Layer srcLayer, Layer dstLayer)
        {
            dstLayer.Locked = srcLayer.Locked;
            dstLayer.Visible = srcLayer.Visible;
            dstLayer.Name = srcLayer.Name;
            dstLayer.Opacity = srcLayer.Opacity;
            for (int i = 0; i < srcLayer.Pixels.Length; i++)
            {
                dstLayer.Pixels[i] = srcLayer.Pixels[i];
            }
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

        private static void OnNewFrame(SessionData session)
        {
            session.CurrentLayerIndex = 0;
            session.Palette = new Palette();
            UniPixUtils.ExtractPaletteFrom(session.CurrentFrame, session.Palette.Colors);

            session.CurrentColor = session.Palette.Colors.Count > 0 ? session.Palette.Colors[0] : Color.black;
            session.SecondaryColor = session.Palette.Colors.Count > 1 ? session.Palette.Colors[1] : Color.white;
        }
        #endregion
    }
}