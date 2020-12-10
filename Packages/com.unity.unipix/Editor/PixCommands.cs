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
        public static bool OpenImage(PixSession session)
        {
            // TODO useproject: open file must be aware of project specific 
            var path = EditorUtility.OpenFilePanel(
                    "Find Pix (.asset | .png | .jpg)",
                    "Assets/",
                    "Image Files;*.asset;*.jpg;*.png");
            if (path == "")
                return false;
            return OpenImage(session, path);
        }

        public static bool OpenImage(PixSession session, UnityEngine.Object[] pixSources)
        {
            session.Image = pixSources.FirstOrDefault(s => s is PixImage) as PixImage;
            if (session.Image == null)
            {
                foreach (var pixSource in pixSources)
                {
                    if (pixSource is Texture2D tex)
                    {
                        PixIO.MakeReadable(tex);
                        var texPath = AssetDatabase.GetAssetPath(tex);
                        var sprites = AssetDatabase.LoadAllAssetsAtPath(texPath).Select(a => a as Sprite).Where(s => s != null).ToArray();                        
                        if (sprites.Length > 0)
                        {
                            PixCore.ImportFrames(ref session.Image, sprites);
                        }
                        else
                        {
                            PixCore.ImportFrame(ref session.Image, tex);
                        }
                    }
                    else if (pixSource is Sprite sprite)
                    {
                        PixCore.ImportFrame(ref session.Image, sprite);
                    }
                }
            }

            InitImageSession(session);
            return true;
        }

        public static bool OpenImage(PixSession session, string path)
        {
            if (Path.IsPathRooted(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
            }
            var contentToLoad = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            return OpenImage(session, new [] { contentToLoad } );
        }

        public static void NewImage(PixSession session, int w, int h)
        {
            var img = PixCore.CreateImage(w, h, Color.clear);
            OpenImage(session, new [] { img });
        }

        public static void SaveImage(PixSession session)
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

        #region UI
        public static void EditInPix(UnityEngine.Object[] sources)
        {
            var pix = EditorWindow.GetWindow<PixEditor>();
            pix.UpdateCanvasSize();
            OpenImage(pix.Session, sources);
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

        public static void IncreaseZoom(PixSession session)
        {
            session.ZoomLevel += 2f;
        }

        public static void DecreaseZoom(PixSession session)
        {
            session.ZoomLevel -= 2f;
            session.ZoomLevel = Mathf.Max(1, session.ZoomLevel);
        }

        public static void ToggleGrid(PixSession session, PixEditor editor)
        {
            session.ShowGrid = !session.ShowGrid;
            editor.ResetGrid();
        }

        public static void SetGridSize(PixSession session, PixEditor editor, int gridSize)
        {
            session.GridSize = gridSize;
            editor.ResetGrid();
        }

        public static void SetGridColor(PixSession session, PixEditor editor, Color color)
        {
            session.GridColor = color;
            editor.ResetGrid();
        }

        public static void ToggleAnimation(PixSession session)
        {
            session.IsPreviewPlaying = !session.IsPreviewPlaying;
            session.PreviewTimer = 0;
        }

        public static void SetPreviewFps(PixSession session, int fps)
        {
            session.PreviewFps = fps;
        }

        public static void SetTool(PixSession session, PixEditor editor, string toolName)
        {
            var index = Array.FindIndex(editor.Tools, tool => tool.Name == toolName);
            if (index != -1)
                session.CurrentToolIndex = index;
        }

        public static void SetBrushSize(PixSession session, int brushSize)
        {
            if (brushSize < PixSession.k_MinBrushSize)
                brushSize = PixSession.k_MinBrushSize;
            if (brushSize > PixSession.k_MaxBrushSize)
                brushSize = PixSession.k_MaxBrushSize;
            session.BrushSize = brushSize;
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

        public static void CloneCurrentLayer(PixSession session)
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

        public static void DeleteCurrentLayer(PixSession session)
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

        public static void NextLayer(PixSession session)
        {
            var newLayerIndex = session.CurrentLayerIndex + 1;
            if (newLayerIndex >= session.CurrentFrame.Layers.Count)
            {
                newLayerIndex = 0;
            }
            SetCurrentLayer(session, newLayerIndex);
        }

        public static void PreviousLayer(PixSession session)
        {
            var newLayerIndex = session.CurrentLayerIndex - 1;
            if (newLayerIndex < 0)
            {
                newLayerIndex = session.CurrentFrame.Layers.Count - 1;
            }
            SetCurrentLayer(session, newLayerIndex);
        }

        public static void MoveCurrentLayerDown(PixSession session)
        {
            if (session.CurrentLayerIndex - 1 >= 0)
                SwapLayers(session, session.CurrentLayerIndex, session.CurrentLayerIndex - 1);
        }

        public static void MoveCurrentLayerUp(PixSession session)
        {
            if (session.CurrentLayerIndex - 1 < session.CurrentFrame.Layers.Count)
                SwapLayers(session, session.CurrentLayerIndex, session.CurrentLayerIndex + 1);
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

        public static void MergeLayers(PixSession session)
        {
            if (session.CurrentLayerIndex > 0)
                PixCommands.MergeLayers(session, session.CurrentLayerIndex, session.CurrentLayerIndex - 1);
        }

        public static void MergeLayers(PixSession session, int src1, int dst2)
        {
            RecordUndo(session, "Merge Layer");
            var srcLayer1 = session.CurrentFrame.Layers[src1];
            var dstLayer2 = session.CurrentFrame.Layers[dst2];

            PixCore.BlendLayer(srcLayer1, dstLayer2, dstLayer2);

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

        #region Model Changed Project Specific
        public static void ReplaceSourceSprite(PixSession session, Sprite sourceSprite)
        {
            Debug.Assert(PixIO.useProject);
            var spriteSize = PixCore.GetSpriteSize(sourceSprite);
            if (session.CurrentFrame.Width != spriteSize.x || session.CurrentFrame.Height != spriteSize.y)
            {
                throw new Exception("New sprite size doesn't match frame.");
            }

            PixIO.MakeReadable(sourceSprite.texture);

            RecordUndo(session, "Replace Source sprite");

            session.CurrentFrame.SourceSprite = sourceSprite;
            session.CurrentFrame.Layers.Clear();
            var layer = session.CurrentFrame.AddLayer();
            layer.Pixels = sourceSprite.texture.GetPixels((int)sourceSprite.rect.x, (int)sourceSprite.rect.y, spriteSize.x, spriteSize.y);
            DirtyImage(session);
        }
        #endregion

        #region Impl
        private static void InitImageSession(PixSession session)
        {
            if (session.Image == null)
            {
                session.Image = PixCore.CreateImage(32, 32, Color.clear);
            }

            UpdateImageTitle(session);
            session.IsImageDirty = false;
            EditorPrefs.SetString(PixEditor.Prefs.kCurrentImg, string.IsNullOrEmpty(session.Image.Path) ? "" : session.Image.Path);

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
            if (string.IsNullOrEmpty(session.Image.Path))
            {
                session.ImageTitle = "*Untitled*";
            }
            else
            {
                session.ImageTitle = session.Image.Path;
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
