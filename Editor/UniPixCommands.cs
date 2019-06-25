using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEngine.PlayerLoop;

namespace UniPix
{
    public static class UniPixCommands
    {
        #region SaveAndLoad
        public static bool LoadPix(SessionData session)
        {
            var path = EditorUtility.OpenFilePanel(
                    "Find Pix (.asset | .png | .jpg)",
                    "Assets/",
                    "Image Files;*.unipix.asset;*.jpg;*.png");
            return LoadPix(session, path);
        }

        public static bool LoadPix(SessionData session, UnityEngine.Object[] pixSources)
        {
            session.Image = null;

            if (pixSources.Length == 1 && pixSources[0] is Image image)
            {
                session.Image = image;
            }
            else
            {
                foreach (var pixSource in pixSources)
                {
                    if (pixSource is Texture2D tex)
                    {
                        UniPixUtils.MakeReadable(tex);
                        var texPath = AssetDatabase.GetAssetPath(tex);
                        var sprites = AssetDatabase.LoadAllAssetsAtPath(texPath).Select(a => a as Sprite).Where(s => s != null).ToArray();                        
                        if (sprites.Length > 0)
                        {
                            UniPixUtils.ImportFrames(ref session.Image, sprites);
                        }
                        else
                        {
                            UniPixUtils.ImportFrame(ref session.Image, tex);
                        }
                    }
                    else if (pixSource is Sprite sprite)
                    {
                        UniPixUtils.ImportFrame(ref session.Image, sprite);
                    }
                }
            }

            InitImageSession(session);
            return true;
        }

        public static bool LoadPix(SessionData session, string path)
        {
            if (Path.IsPathRooted(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
            }
            session.Image = AssetDatabase.LoadAssetAtPath<Image>(path);
            InitImageSession(session);
            return true;
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
        #endregion

        #region Export
        public static void SaveImageSources(SessionData session, bool spriteSheet = false)
        {
            if (spriteSheet)
            {
                var linkedFrames = session.Image.Frames.Where(f => f.SourceSprite != null).ToArray();
                var unlinkedFrame = session.Image.Frames.Where(f => f.SourceSprite == null).ToArray();
                // If spriteSheet: bundle together all unlinked frame. Create sprite sheet
                // TODO Export
            }
            else
            {
                var basePath = UniPixUtils.GetBasePath(session.ImagePath);
                for (int i = 0; i < session.Image.Frames.Count; i++)
                {
                    var frame = session.Image.Frames[i];
                    if (frame.SourceSprite == null)
                    {
                        // One image per frame
                        var framePath = UniPixUtils.GetUniquePath(basePath, ".png", i);
                        framePath = ExportFrame(frame, framePath);
                        frame.SourceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
                    }
                    else
                    {
                        UpdateFrameSprite(frame);
                    }
                }
            }
        }

        // Export is not linked at all to the image
        public static void ExportFrames(SessionData session, Frame[] frames = null)
        {
            // ask user for base name: give image as base name
            // Save each image separately
            // Ensure to properly update SpriteMetadata
            frames = frames ?? session.Image.Frames.ToArray();
            if (frames.Length == 0)
                return;

            string path = EditorUtility.SaveFilePanel(
                "Export as image",
                "Assets/", string.IsNullOrEmpty(session.ImagePath) ? "pix.png" : UniPixUtils.GetBaseName(session.ImagePath), "png");
            if (path == "")
            {
                return;
            }

            var basePath = UniPixUtils.GetBasePath(path);

            for (var i = 0; i < session.Image.Frames.Count; ++i)
            {
                var frame = session.Image.Frames[i];
                ExportFrame(frame, UniPixUtils.GetUniquePath(basePath, ".png", i));
            }
        }

        // Export is not linked to the image
        public static void ExportFramesToSpriteSheet(SessionData session)
        {
            // ask user for base name: give image as base name
            // Save as a sprite sheet
            // Ensure to properly update SpriteMetadata

            // TODO Export
            throw new Exception("Not Implemented");
        }

        public static void UpdateFrameSprite(Frame frame)
        {
            if (frame.SourceSprite == null)
                throw new Exception("Not implemented");

            var spriteSize = UniPixUtils.GetSpriteSize(frame.SourceSprite);
            if (spriteSize.x != frame.Width || spriteSize.y != frame.Height)
                throw new Exception("UpdateFrameSprite: Frame doesn't match sprite size");

            var texture = frame.SourceSprite.texture;
            var texturePath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(texturePath))
                throw new Exception("Texture not bound to a path");

            UniPixUtils.MakeUncompressed(texturePath, texture);

            var textureRect = frame.SourceSprite.rect;
            var frameX = 0;
            var frameY = 0;
            for (var x = textureRect.x; x < textureRect.xMax; ++x, ++frameX)
            {
                for (var y = textureRect.y; y < textureRect.yMax; ++y, ++frameY)
                {
                    texture.SetPixel((int)x, (int)y, frame.Texture.GetPixel(frameX, frameY));
                }
            }
            texture.Apply();

            var frameContent = texture.EncodeToPNG();
            if (frameContent == null)
                throw new Exception("Texture cannot be converted to PNG: " + texturePath);
            File.WriteAllBytes(texturePath, frameContent);
            AssetDatabase.Refresh();
        }

        private static string ExportFrame(Frame frame, string path)
        {
            var updateMetaFile = !File.Exists(path);
            var frameTex = frame.Texture;
            var frameContent = frameTex.EncodeToPNG();
            File.WriteAllBytes(path, frameContent);
            AssetDatabase.Refresh();

            if (updateMetaFile)
            {
                TextureImporter importer = TextureImporter.GetAtPath(path) as TextureImporter;
                importer.textureType = TextureImporterType.Sprite;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                AssetDatabase.Refresh();
            }
            return path;
        }

        #endregion

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

        public static void SetBrushColor(SessionData session, int colorType, Color color)
        {
            if (colorType == 0)
            {
                session.CurrentColor = color;
                session.CurrentColorPaletteIndex = session.Palette.Colors.FindIndex(c => c == color);
            }
            else
            {
                session.SecondaryColor = color;
                session.SecondaryColorPaletteIndex = session.Palette.Colors.FindIndex(c => c == color);
            }
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

        private static void InitImageSession(SessionData session)
        {
            if (session.Image == null)
            {
                session.Image = UniPixUtils.CreateImage(32, 32, Color.clear);
            }

            session.IsImageDirty = false;
            UpdateImageTitle(session);
            EditorPrefs.SetString(PixEditor.Prefs.kCurrentImg, string.IsNullOrEmpty(session.ImagePath) ? "" : session.ImagePath);

            session.CurrentFrameIndex = 0;
            session.CurrentLayerIndex = 0;
            session.PreviewFrameIndex = 0;

            OnNewFrame(session);
        }

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

            SetBrushColor(session, 0, session.Palette.Colors.Count > 0 ? session.Palette.Colors[0] : Color.black);
            SetBrushColor(session, 1, session.Palette.Colors.Count > 1 ? session.Palette.Colors[1] : Color.white);
        }
        #endregion
    }
}