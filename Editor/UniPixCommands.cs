using System;
using System.Collections.Generic;
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
            session.Image = pixSources.FirstOrDefault(s => s is Image) as Image;
            if (session.Image == null)
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

        public static bool LoadPix(PixSession session, string path)
        {
            if (Path.IsPathRooted(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
            }
            session.Image = AssetDatabase.LoadAssetAtPath<Image>(path);
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
            Image img = UniPixUtils.CreateImage(w, h, Color.clear);
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
            UniPixCommands.SavePix(session);
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
                                "Assets/", string.IsNullOrEmpty(session.ImagePath) ? "pix.png" : UniPixUtils.GetBaseName(session.ImagePath), "png");
                            basePath = path == "" ? UniPixUtils.GetBasePath(session.ImagePath) : UniPixUtils.GetBasePath(path);
                        }

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

        public static void ReplaceSourceSprite(PixSession session, Sprite sourceSprite)
        {
            var spriteSize = UniPixUtils.GetSpriteSize(sourceSprite);
            if (session.CurrentFrame.Width != spriteSize.x || session.CurrentFrame.Height != spriteSize.y)
            {
                throw new Exception("New sprite size doesn't match frame.");
            }

            UniPixUtils.MakeReadable(sourceSprite.texture);

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
            var baseName = string.IsNullOrEmpty(session.ImagePath) ? "pix.png" : UniPixUtils.GetBaseName(session.ImagePath);
            string path = EditorUtility.SaveFilePanel("Export as image", baseFolder, baseName, "png");
            if (path == "")
            {
                return null;
            }

            var basePath = UniPixUtils.GetBasePath(path);
            var frameFilePaths = new List<string>();
            for (var i = 0; i < frames.Length; ++i)
            {
                var frame = session.Image.Frames[i];
                frameFilePaths.Add(ExportFrame(frame, UniPixUtils.GetUniquePath(basePath, ".png", i)));
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
            var baseName = string.IsNullOrEmpty(session.ImagePath) ? "sprite_sheet.png" : $"{UniPixUtils.GetBaseName(session.ImagePath)}_sheet";
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

            var spriteSheet = new Texture2D(spriteSheetWidth, spriteSheetHeight) { filterMode = FilterMode.Point };
            spriteSheet.name = UniPixUtils.GetBaseName(path);
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
            LoadPix(pix.Session, sources);
        }

        public static void SetCurrentFrame(PixSession session, int frameIndex)
        {
            session.CurrentFrameIndex = frameIndex;
            OnNewFrame(session);
        }

        public static void SetCurrentLayer(PixSession session, int layerIndex)
        {
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

        public static void CenterCanvas(PixSession session)
        {

        }
        #endregion

        // TODO: should it be part of the model?
        public static void AddPaletteColor(PixSession session, Color newColor)
        {
            session.Palette.Colors.Add(newColor);
        }

        #region ModelChanged
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
            
            UniPixUtils.Blend(srcLayer1, dstLayer2, dstLayer2);

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
                session.Image = UniPixUtils.CreateImage(32, 32, Color.clear);
            }

            session.IsImageDirty = false;
            UpdateImageTitle(session);
            EditorPrefs.SetString(PixEditor.Prefs.kCurrentImg, string.IsNullOrEmpty(session.ImagePath) ? "" : session.ImagePath);

            session.CurrentFrameIndex = 0;
            session.CurrentLayerIndex = 0;
            session.PreviewFrameIndex = 0;

            OnNewFrame(session);

            CenterCanvas(session);
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

        private static void RecordUndo(PixSession session, string info)
        {
            Undo.RecordObject(session.Image, info);
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
            UniPixUtils.ExtractPaletteFrom(session.CurrentFrame, session.Palette.Colors);

            SetBrushColor(session, 0, session.Palette.Colors.Count > 0 ? session.Palette.Colors[0] : Color.black);
            SetBrushColor(session, 1, session.Palette.Colors.Count > 1 ? session.Palette.Colors[1] : Color.white);
        }
        #endregion

        #region Menu
        [CommandHandler("UniPix/NewImage")]
        private static void NewImage(CommandExecuteContext context)
        {
            // TODO: allow selection of image size
            CreatePix(PixEditor.s_Session, 32, 32);
        }

        [CommandHandler("UniPix/Open")]
        private static void Open(CommandExecuteContext context)
        {
            LoadPix(PixEditor.s_Session);
        }

        [CommandHandler("UniPix/Save")]
        private static void Save(CommandExecuteContext context)
        {
            SavePix(PixEditor.s_Session);
        }

        [CommandHandler("UniPix/Sync")]
        private static void Sync(CommandExecuteContext context)
        {
            // TODO with a validator
        }

        [CommandHandler("UniPix/ExportCurrentFrame")]
        private static void ExportCurrentFrame(CommandExecuteContext context)
        {
            ExportFrames(PixEditor.s_Session, new[] { PixEditor.s_Session.CurrentFrame });
        }

        [CommandHandler("UniPix/ExportFrames")]
        private static void ExportFrames(CommandExecuteContext context)
        {
            ExportFrames(PixEditor.s_Session);
        }

        [CommandHandler("UniPix/ExportSpriteSheet")]
        private static void ExportSpriteSheet(CommandExecuteContext context)
        {
            ExportFramesToSpriteSheet(PixEditor.s_Session);
        }

        [CommandHandler("UniPix/GotoDefaultMode")]
        private static void GotoDefaultMode(CommandExecuteContext context)
        {
            ModeService.ChangeModeById("default");
        }

        [CommandHandler("UniPix/GotoNextFrame")]
        private static void GotoNextFrame(CommandExecuteContext context)
        {
            
        }

        [CommandHandler("UniPix/GotoPreviousFrame")]
        private static void GotoPreviousFrame(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/DeleteCurrentFrame")]
        private static void DeleteCurrentFrame(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/CloneCurrentFrame")]
        private static void CloneCurrentFrame(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/NewFrame")]
        private static void NewFrame(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/GotoNextLayer")]
        private static void GotoNextLayer(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/GotoPreviousLayer")]
        private static void GotoPreviousLAyer(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/DeleteCurrentLayer")]
        private static void DeleteCurrentLayer(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/CloneCurrentLayer")]
        private static void CloneCurrentLayer(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/NewLayer")]
        private static void NewLayer(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/MergeCurrentLayer")]
        private static void MergeCurrentLayer(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/MoveLayerUp")]
        private static void MoveLayerUp(CommandExecuteContext context)
        {

        }
        [CommandHandler("UniPix/MoveLayerDown")]
        private static void MoveLayerDown(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/Zoom")]
        private static void Zoom(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/ZoomBack")]
        private static void ZoomBack(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/CenterCanvas")]
        private static void CenterCanvas(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/ToggleGrid")]
        private static void ToggleGrid(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/ToggleAnimation")]
        private static void ToggleAnimation(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/IncreaseAnimationSpeed")]
        private static void IncreaseAnimationSpeed(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/DecreaseAnimationSpeed")]
        private static void DecreaseAnimationSpeed(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/ActivateBrush")]
        private static void ActivateBrush(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/ActivateEraser")]
        private static void ActivateEraser(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/IncreaseBrushSize")]
        private static void IncreaseBrushSize(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/DecreaseBrushSize")]
        private static void DecreaseBrushSize(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/IncreaseColor")]
        private static void IncreaseColor(CommandExecuteContext context)
        {
            Debug.Log("Color ->");
        }

        [CommandHandler("UniPix/DecreaseColor")]
        private static void DecreaseColor(CommandExecuteContext context)
        {
            Debug.Log("Color <-");
        }

        [CommandHandler("UniPix/SwapColor")]
        private static void SwapColor(CommandExecuteContext context)
        {

        }

        [CommandHandler("UniPix/OpenProjectBrowse")]
        private static void OpenProjectBrowse(CommandExecuteContext context)
        {
            LayoutUtils.CreateProject();
        }

        private static void LoadLayout(string name)
        {
            if (ModeService.currentId == "unipix")
            {
                var layouts = ModeService.GetModeDataSectionList<string>(ModeService.currentIndex, "layouts");
                foreach (var layout in layouts)
                {
                    var layoutName = Path.GetFileNameWithoutExtension(layout);
                    if (name == layoutName)
                    {
                        WindowLayout.LoadWindowLayout(layout, false);
                    }
                }
            }
        }

        [CommandHandler("UniPix/Layout/Pix")]
        private static void LayoutPix(CommandExecuteContext context)
        {
            LoadLayout("Pix");
        }

        [CommandHandler("UniPix/Layout/PixBrowse")]
        private static void LayoutPixBrowse(CommandExecuteContext context)
        {
            LoadLayout("Browse");
        }

        [CommandHandler("UniPix/Layout/PixDebug")]
        private static void LayoutPixDebug(CommandExecuteContext context)
        {
            LoadLayout("Debug");
        }
        #endregion
    }
}
 
 