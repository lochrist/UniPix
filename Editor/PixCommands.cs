using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEngine.PlayerLoop;

namespace UniPix
{
    public static class PixCommands
    {
        #region SaveAndLoad
        public static bool LoadPix(PixSession session)
        {
            var path = EditorUtility.OpenFilePanel(
                    "Find Pix (.unipix.asset | .png | .jpg)",
                    "Assets/",
                    "Image Files;*.unipix.asset;*.jpg;*.png");
            if (path == "")
                return false;
            return LoadPix(session, path);
        }

        public static bool LoadPix(PixSession session, UnityEngine.Object[] pixSources)
        {
            session.Image = pixSources.FirstOrDefault(s => s is PixImage) as PixImage;
            if (session.Image == null)
            {
                foreach (var pixSource in pixSources)
                {
                    if (pixSource is Texture2D tex)
                    {
                        PixUtils.MakeReadable(tex);
                        var texPath = AssetDatabase.GetAssetPath(tex);
                        var sprites = AssetDatabase.LoadAllAssetsAtPath(texPath).Select(a => a as Sprite).Where(s => s != null).ToArray();                        
                        if (sprites.Length > 0)
                        {
                            PixUtils.ImportFrames(ref session.Image, sprites);
                        }
                        else
                        {
                            PixUtils.ImportFrame(ref session.Image, tex);
                        }
                    }
                    else if (pixSource is Sprite sprite)
                    {
                        PixUtils.ImportFrame(ref session.Image, sprite);
                    }
                }
            }

            InitImageSession(session);
            return true;
        }

        public static bool LoadPix(PixSession session, string path)
        {
            if (Path.IsPathRooted(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
            }
            session.Image = AssetDatabase.LoadAssetAtPath<PixImage>(path);
            InitImageSession(session);
            return true;
        }

        public static void CreatePix(PixSession session, int w, int h)
        {
            string path = EditorUtility.SaveFilePanel(
                "Create UniPix",
                "Assets/", "Pix.unipix", "asset");
            if (path == "")
            {
                return;
            }

            path = FileUtil.GetProjectRelativePath(path);
            PixImage img = PixUtils.CreateImage(w, h, Color.clear);
            AssetDatabase.CreateAsset(img, path);
            EditorUtility.SetDirty(img);
            AssetDatabase.SaveAssets();

            LoadPix(session, AssetDatabase.GetAssetPath(img));
        }

        public static void SavePix(PixSession session)
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
        public static void SaveImageSources(PixSession session, bool spriteSheet = false)
        {
            PixCommands.SavePix(session);
            if (string.IsNullOrEmpty(session.ImagePath))
            {
                // SavePix was cancelled.
                return;
            }

            if (spriteSheet)
            {
                var linkedFrames = session.Image.Frames.Where(f => f.SourceSprite != null).ToArray();
                foreach (var linkedFrame in linkedFrames)
                {
                    UpdateFrameSprite(linkedFrame);
                }

                // TODO Export => not linked
                var unlinkedFrame = session.Image.Frames.Where(f => f.SourceSprite == null).ToArray();
                var sheet = ExportFramesToSpriteSheet(session, unlinkedFrame);
                // Need to relink each sprite to their frame.
            }
            else
            {
                string basePath =null;
                for (int i = 0; i < session.Image.Frames.Count; i++)
                {
                    var frame = session.Image.Frames[i];
                    if (frame.SourceSprite == null)
                    {
                        if (basePath == null)
                        {
                            string path = EditorUtility.SaveFilePanel(
                                "Export as image",
                                "Assets/", string.IsNullOrEmpty(session.ImagePath) ? "pix.png" : PixUtils.GetBaseName(session.ImagePath), "png");
                            basePath = path == "" ? PixUtils.GetBasePath(session.ImagePath) : PixUtils.GetBasePath(path);
                        }

                        // One image per frame
                        var framePath = PixUtils.GetUniquePath(basePath, ".png", i);
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

        public static void ReplaceSourceSprite(PixSession session, Sprite sourceSprite)
        {
            var spriteSize = PixUtils.GetSpriteSize(sourceSprite);
            if (session.CurrentFrame.Width != spriteSize.x || session.CurrentFrame.Height != spriteSize.y)
            {
                throw new Exception("New sprite size doesn't match frame.");
            }

            PixUtils.MakeReadable(sourceSprite.texture);

            RecordUndo(session, "Replace Source sprite");

            session.CurrentFrame.SourceSprite = sourceSprite;
            session.CurrentFrame.Layers.Clear();
            var layer = session.CurrentFrame.AddLayer();
            layer.Pixels = sourceSprite.texture.GetPixels((int)sourceSprite.rect.x, (int)sourceSprite.rect.y, spriteSize.x, spriteSize.y);
            DirtyImage(session);
        }

        // Export is not linked at all to the image
        public static string[] ExportFrames(PixSession session, Frame[] frames = null)
        {
            // ask user for base name: give image as base name
            // Save each image separately
            // Ensure to properly update SpriteMetadata
            frames = frames ?? session.Image.Frames.ToArray();
            if (frames.Length == 0)
                return null;

            var baseFolder = string.IsNullOrEmpty(session.ImagePath) ? "Assets/" : Path.GetDirectoryName(session.ImagePath);
            var baseName = string.IsNullOrEmpty(session.ImagePath) ? "pix.png" : PixUtils.GetBaseName(session.ImagePath);
            string path = EditorUtility.SaveFilePanel("Export as image", baseFolder, baseName, "png");
            if (path == "")
            {
                return null;
            }

            var basePath = PixUtils.GetBasePath(path);
            var frameFilePaths = new List<string>();
            for (var i = 0; i < frames.Length; ++i)
            {
                var frame = session.Image.Frames[i];
                frameFilePaths.Add(ExportFrame(frame, PixUtils.GetUniquePath(basePath, ".png", i)));
            }

            return frameFilePaths.ToArray();
        }

        // Export is not linked to the image
        public static string ExportFramesToSpriteSheet(PixSession session, Frame[] frames = null)
        {
            // ask user for base name: give image as base name
            // Save as a sprite sheet
            // Ensure to properly update SpriteMetadata
            frames = frames ?? session.Image.Frames.ToArray();
            var baseFolder = string.IsNullOrEmpty(session.ImagePath) ? "Assets/" : Path.GetDirectoryName(session.ImagePath);
            var baseName = string.IsNullOrEmpty(session.ImagePath) ? "sprite_sheet.png" : $"{PixUtils.GetBaseName(session.ImagePath)}_sheet";
            string path = EditorUtility.SaveFilePanel("Save spritesheet", baseFolder, baseName, "png");
            if (path == "")
            {
                return null;
            }

            var frameWidth = session.Image.Width;
            var frameHeight = session.Image.Height;
            var rows = (int)Mathf.Sqrt(frames.Length);
            var spriteSheetWidth = (frames.Length * frameWidth) / rows;
            spriteSheetWidth += spriteSheetWidth % frameWidth;

            var spriteSheetHeight = frameHeight * rows;
            spriteSheetHeight += spriteSheetHeight % frameHeight;

            var spriteSheet = PixUtils.CreateTexture(spriteSheetWidth, spriteSheetHeight);
            spriteSheet.name = PixUtils.GetBaseName(path);
            var offsetX = 0;
            var offsetY = spriteSheetHeight - frameHeight;

            for (int i = 0; i < frames.Length; i++)
            {
                if (i != 0 && (frameWidth * i) % spriteSheetWidth == 0)
                {
                    offsetY -= frameHeight;
                    offsetX = 0;
                }

                for (var x = 0; x < frameWidth; x++)
                {
                    for (var y = 0; y < frameHeight; y++)
                    {
                        var framePixelColor = frames[i].Texture.GetPixel(x, y);
                        spriteSheet.SetPixel(x + offsetX, y + offsetY, framePixelColor);
                        spriteSheet.Apply();
                    }
                }
                offsetX += frameWidth;
            }

            var content = spriteSheet.EncodeToPNG();
            File.WriteAllBytes(path, content);
            AssetDatabase.Refresh();

            // Slice the sprite:
            var importer = AssetImporter.GetAtPath(FileUtil.GetProjectRelativePath(path)) as TextureImporter;
            if (importer != null)
            {
                importer.isReadable = true;
                importer.spriteImportMode = SpriteImportMode.Multiple;

                var spritesheetMetaData = new List<SpriteMetaData>();
                for (int x = 0; x < spriteSheet.width; x += frameWidth)
                {
                    for (int y = spriteSheet.height; y > 0; y -= frameHeight)
                    {
                        SpriteMetaData data = new SpriteMetaData();
                        data.pivot = new Vector2(0.5f, 0.5f);
                        data.alignment = 9;
                        data.name = spriteSheet.name + x + "_" + y;
                        data.rect = new Rect(x, y - frameHeight, frameWidth, frameHeight);
                        spritesheetMetaData.Add(data);
                    }
                }

                importer.spritesheet = spritesheetMetaData.ToArray();
                AssetDatabase.Refresh();
                importer.SaveAndReimport();
            }

            return path;
        }

        public static void UpdateFrameSprite(Frame frame)
        {
            if (frame.SourceSprite == null)
                throw new Exception("Not implemented");

            var spriteSize = PixUtils.GetSpriteSize(frame.SourceSprite);
            if (spriteSize.x != frame.Width || spriteSize.y != frame.Height)
                throw new Exception("UpdateFrameSprite: Frame doesn't match sprite size");

            var texture = frame.SourceSprite.texture;
            var texturePath = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(texturePath))
                throw new Exception("Texture not bound to a path");

            PixUtils.MakeUncompressed(texturePath, texture);

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
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                    AssetDatabase.Refresh();
                }
            }
            return path;
        }

        #endregion

        #region UI
        public static void EditInPix(UnityEngine.Object[] sources)
        {
            var pix = EditorWindow.GetWindow<PixEditor>();
            pix.UpdateCanvasSize();
            LoadPix(pix.Session, sources);
        }

        public static void SetCurrentFrame(PixSession session, int frameIndex)
        {
            RecordUndo(session, "Change Current Frame", false, true);
            session.CurrentFrameIndex = frameIndex;
            OnNewFrame(session);
        }

        public static void SetCurrentLayer(PixSession session, int layerIndex)
        {
            RecordUndo(session, "Change Current Layer", false, true);
            session.CurrentLayerIndex = layerIndex;
        }

        public static void SetBrushColor(PixSession session, int colorType, Color color)
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

        public static void SwitchColor(PixSession session)
        {
            var primaryColor = session.CurrentColor;
            var primaryColorIndex = session.CurrentColorPaletteIndex;
            session.CurrentColor = session.SecondaryColor;
            session.CurrentColorPaletteIndex = session.SecondaryColorPaletteIndex;
            session.SecondaryColorPaletteIndex = primaryColorIndex;
            session.SecondaryColor = primaryColor;
        }

        public static void FrameImage(PixSession session)
        {
            session.ZoomLevel = 1f;
            if (session.CanvasSize.x > session.CanvasSize.y)
            {
                session.ZoomLevel = (int)session.CanvasSize.y / session.Image.Height;
            }
            else
            {
                session.ZoomLevel = (int)session.CanvasSize.x / session.Image.Width;
            }
            session.ScaledImgRect.width = session.Image.Width * session.ZoomLevel;
            session.ScaledImgRect.height = session.Image.Height * session.ZoomLevel;
            CenterImage(session);
        }

        public static void CenterImage(PixSession session)
        {
            var imageOffset = (session.CanvasSize / 2) - (session.ScaledImgRect.size / 2);
            session.ImageOffsetX = imageOffset.x;
            session.ImageOffsetY = imageOffset.y;
        }
        #endregion

        // TODO: should it be part of the model?
        public static void AddPaletteColor(PixSession session, Color newColor)
        {
            session.Palette.Colors.Add(newColor);
        }

        #region ModelChanged
        public struct SessionChangeScope : IDisposable
        {
            PixSession m_Session;
            public SessionChangeScope(PixSession session, string title, bool saveImage= true, bool saveImgSessionState = true)
            {
                m_Session = session;
                RecordUndo(session, title, saveImage, saveImgSessionState);
            }

            public void Dispose()
            {
                DirtyImage(m_Session);
            }
        }
        public static void NewFrame(PixSession session)
        {
            RecordUndo(session, "Add Frame");
            var newFrame = session.Image.AddFrame();
            newFrame.AddLayer();
            session.CurrentFrameIndex = session.Image.Frames.Count - 1;
            DirtyImage(session);
        }

        public static void DeleteFrame(PixSession session, int frameIndex)
        {
            RecordUndo(session, "Delete Frame");
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

        public static void CloneFrame(PixSession session, int frameIndex)
        {
            RecordUndo(session, "Clone frame");
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

        public static void CreateLayer(PixSession session)
        {
            RecordUndo(session, "Add Layer");
            session.CurrentFrame.AddLayer(session.CurrentLayerIndex + 1);
            session.CurrentLayerIndex = session.CurrentLayerIndex + 1;
            DirtyImage(session);
        }

        public static void CloneLayer(PixSession session)
        {
            RecordUndo(session, "Clone Layer");
            var clonedLayer = session.CurrentFrame.AddLayer(session.CurrentLayerIndex + 1);
            var clonedLayerName = clonedLayer.Name;
            var currentLayer = session.CurrentLayer;
            CopyLayer(currentLayer, clonedLayer);
            clonedLayer.Name = clonedLayerName;

            session.CurrentLayerIndex = session.CurrentLayerIndex + 1;
            DirtyImage(session);
        }

        public static void DeleteLayer(PixSession session)
        {
            RecordUndo(session, "Delete Layer");
            session.CurrentFrame.Layers.Remove(session.CurrentLayer);
            if (session.CurrentFrame.Layers.Count == 0)
            {
                session.CurrentFrame.AddLayer();
            }

            if (session.CurrentLayerIndex >= session.CurrentFrame.Layers.Count)
                session.CurrentLayerIndex = session.CurrentFrame.Layers.Count - 1;
            DirtyImage(session);
        }

        public static void SetLayerOpacity(PixSession session, int layerIndex, float opacity)
        {
            RecordUndo(session, "Layer Opacity");
            session.CurrentFrame.Layers[layerIndex].Opacity = opacity;
            DirtyImage(session);
        }

        public static void SetLayerVisibility(PixSession session, int layerIndex, bool isVisible)
        {
            RecordUndo(session, "Layer Visibility");
            session.CurrentFrame.Layers[layerIndex].Visible = isVisible;
            DirtyImage(session);
        }

        public static void SwapLayers(PixSession session, int layerIndex1, int layerIndex2)
        {
            RecordUndo(session, "Move Layer");
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

        public static void MergeLayers(PixSession session, int src1, int dst2)
        {
            RecordUndo(session, "Merge Layer");
            var srcLayer1 = session.CurrentFrame.Layers[src1];
            var dstLayer2 = session.CurrentFrame.Layers[dst2];
            
            PixUtils.Blend(srcLayer1, dstLayer2, dstLayer2);

            session.CurrentFrame.Layers.Remove(srcLayer1);

            if (session.CurrentLayerIndex == src1)
                session.CurrentLayerIndex = dst2;

            DirtyImage(session);
        }

        public static void SetPixelsUnderBrush(PixSession session, Color color)
        {
            RecordUndo(session, "Pixels change");
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
        private static void InitImageSession(PixSession session)
        {
            if (session.Image == null)
            {
                session.Image = PixUtils.CreateImage(32, 32, Color.clear);
            }

            UpdateImageTitle(session);
            session.IsImageDirty = false;
            EditorPrefs.SetString(PixEditor.Prefs.kCurrentImg, string.IsNullOrEmpty(session.ImagePath) ? "" : session.ImagePath);

            session.CurrentFrameIndex = 0;
            session.CurrentLayerIndex = 0;
            session.PreviewFrameIndex = 0;

            OnNewFrame(session);
            FrameImage(session);
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

        private static void RecordUndo(PixSession session, string info, bool saveImage = true, bool saveImageSessionState = true)
        {
            if (saveImage)
                Undo.RecordObject(session.Image, info);
            if (saveImageSessionState)
                Undo.RecordObject(session.ImageSessionState, info);
        }

        private static void DirtyImage(PixSession session)
        {
            session.CurrentFrame.UpdateFrame();
            if (!session.IsImageDirty)
            {
                session.IsImageDirty = true;
                UpdateImageTitle(session);
            }
        }

        private static void UpdateImageTitle(PixSession session)
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

        private static void OnNewFrame(PixSession session)
        {
            session.CurrentLayerIndex = 0;
            session.Palette = new Palette();
            PixUtils.ExtractPaletteFrom(session.CurrentFrame, session.Palette.Colors);

            SetBrushColor(session, 0, session.Palette.Colors.Count > 0 ? session.Palette.Colors[0] : Color.black);
            SetBrushColor(session, 1, session.Palette.Colors.Count > 1 ? session.Palette.Colors[1] : Color.white);
        }
        #endregion
    }
}