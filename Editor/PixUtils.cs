using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace UniPix
{
    public static class PixUtils
    {
        public static PixImage CreateImage(int width, int height, Color baseColor)
        {
            var img = PixImage.CreateImage(width, height);
            var newFrame = img.AddFrame();
            var layer = newFrame.AddLayer();
            for (var i = 0; i < layer.Pixels.Length; ++i)
            {
                layer.Pixels[i] = baseColor;
            }

            return img;
        }

        public static Texture2D CreateTexture(int width, int height)
        {
            return new Texture2D(width, height) { filterMode = FilterMode.Point };
        }

        public static void SetTextureColor(Texture2D tex, Color color, bool apply = true)
        {
            for(var x = 0; x < tex.width; ++x)
            for (var y = 0; y < tex.height; ++y)
                tex.SetPixel(x, y, color);

            tex.Apply();
        }

        public static bool MakeReadable(Texture2D tex)
        {
            if (tex.isReadable)
                return true;
            string assetPath = AssetDatabase.GetAssetPath(tex);
            return MakeReadable(assetPath, tex);
        }

        public static bool MakeReadable(string path, Texture2D tex)
        {
            if (tex.isReadable)
                return true;

            var tImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.isReadable = true;
                AssetDatabase.ImportAsset(path);
                AssetDatabase.Refresh();
                return true;
            }
            return false;
        }

        public static void MakeUncompressed(string path, Texture2D tex)
        {
            var tImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (tImporter != null && tImporter.textureCompression != TextureImporterCompression.Uncompressed)
            {
                tImporter.textureCompression = TextureImporterCompression.Uncompressed;
                AssetDatabase.ImportAsset(path);
                AssetDatabase.Refresh();
            }
        }

        public static Layer ImportFrame(ref PixImage img, Texture2D tex)
        {
            if (img == null)
            {
                img = PixImage.CreateImage(tex.width, tex.height);
            }

            // TODO : Crop
            if (tex.width > img.Width)
                throw new Exception($"Texture doesn't width {tex.width} with img width {img.Width}");
            if (tex.height > img.Height)
                throw new Exception($"Texture doesn't width {tex.height} with img width {img.Height}");

            var newFrame = img.AddFrame();
            var layer = newFrame.AddLayer();
            layer.Pixels = tex.GetPixels();
            return layer;
        }

        public static void ImportFrames(ref PixImage img, Sprite[] sprites)
        {
            foreach (var sprite in sprites)
            {
                ImportFrame(ref img, sprite);
            }
        }

        public static void ImportFrame(ref PixImage img, Sprite sprite)
        {
            var texture = sprite.texture;
            if (!texture || texture == null)
                return;

            var frameSize = GetSpriteSize(sprite);
            if (img == null)
            {
                img = PixImage.CreateImage(frameSize.x, frameSize.y);
            }

            if (frameSize.x > img.Width)
                return;

            if (frameSize.y > img.Height)
                return;

            var newFrame = img.AddFrame();
            newFrame.SourceSprite = sprite;
            var layer = newFrame.AddLayer();
            // TODO : handle imge with sprites of multi dimensions
            layer.Pixels = texture.GetPixels((int)sprite.rect.x, (int)sprite.rect.y, frameSize.x, frameSize.y);
        }

        public static Vector2Int GetSpriteSize(Sprite sprite)
        {
            return new Vector2Int((int)sprite.rect.width, (int)sprite.rect.height);
        }

        public static void SetLayerColor(Layer layer, Color color)
        {
            for (var i = 0; i < layer.Pixels.Length; ++i)
            {
                layer.Pixels[i] = color;
            }
        }

        public static void Blend(Layer srcLayer, Layer dstLayer, Layer result)
        {
            // Simple alpha blend: https://en.wikipedia.org/wiki/Alpha_compositing
            // outA = srcA + dstA (1 - srcA)
            // outRGB = (srcRGB * srcA + dstRGB * dstA (1 - srcA)) / outA
            for (var i = 0; i < srcLayer.Pixels.Length; ++i)
            {
                var src = srcLayer.Pixels[i];
                var dst = dstLayer.Pixels[i];
                var srcA = src.a * srcLayer.Opacity;
                var dstA = dst.a * dstLayer.Opacity;
                var outA = srcA + dstA * (1 - srcA);
                var safeOutA = outA == 0.0f ? 1.0f : outA;
                result.Pixels[i] = new Color(
                    (src.r * srcA + dst.r * dstA * (1 - srcA)) / safeOutA,
                    (src.g * srcA + dst.g * dstA * (1 - srcA)) / safeOutA,
                    (src.b * srcA + dst.b * dstA * (1 - srcA)) / safeOutA,
                    outA
                );
            }
        }

        public static void ExtractPaletteFrom(Frame frame, List<Color> colors)
        {
            foreach (var layer in frame.Layers)
            {
                ExtractPaletteFrom(layer, colors);
            }
        }

        public static void ExtractPaletteFrom(Layer layer, List<Color> colors)
        {
            foreach (var pixel in layer.Pixels)
            {
                if (pixel.a == 1f && !colors.Contains(pixel))
                {
                    colors.Add(pixel);
                }
            }
        }

        public static float Slider(string title, float value, float left, float right, GUIStyle sliderStyle = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, GUILayout.ExpandWidth(false));
            var result = GUILayout.HorizontalSlider(value, left, right, sliderStyle ?? GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            return result;
        }

        public static Rect LayoutFrameTile(Frame frame, bool currentFrame = false)
        {
            var tex = frame.Texture;
            var frameRect = GUILayoutUtility.GetRect(PixEditor.Styles.kFramePreviewSize, PixEditor.Styles.kFramePreviewWidth, PixEditor.Styles.pixBox, GUILayout.Width(PixEditor.Styles.kFramePreviewSize), GUILayout.Height(PixEditor.Styles.kFramePreviewSize));
            GUI.Box(frameRect, "", currentFrame ? PixEditor.Styles.selectedPixBox : PixEditor.Styles.pixBox);
            GUI.DrawTexture(frameRect, tex, ScaleMode.ScaleToFit);
            return frameRect;
        }

        public static string GetBasePath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                path = FileUtil.GetProjectRelativePath(path);
            }

            var directoryName = Path.GetDirectoryName(path).Replace("\\", "/");
            return $"{Path.GetDirectoryName(path)}/{GetBaseName(path)}";
        }

        public static string GetBaseName(string path)
        {
            var name = Path.GetFileName(path);
            var firstDot = name.IndexOf(".");
            var baseName = name.Substring(0, firstDot);
            return baseName;
        }

        public static string GetUniquePath(string basePath, string extension, int index = 1)
        {
            var path = $"{basePath}_{index}{extension}";
            while (File.Exists(path))
            {
                ++index;
                path = $"{basePath}_{index}{extension}";
            }
            return path;
        }

        public static int ImgCoordToPixelIndex(PixImage img, int imgCoordX, int imgCoordY)
        {
            return imgCoordX + (img.Height - imgCoordY - 1) * img.Height;
        }

        public static void DrawRectangle(PixImage img, Vector2Int start, Vector2Int end, Color color, int brushSize, Color[] output)
        {
            var minX = Mathf.Min(start.x, end.x);
            var maxX = Mathf.Max(start.x, end.x);
            var minY = Mathf.Min(start.y, end.y);
            var maxY = Mathf.Max(start.y, end.y);
            for (int x = minX; x <= maxX; ++x)
            {
                for (int y = minY; y <= maxY; ++y)
                {
                    if (x < minX + brushSize || y < minY + brushSize ||
                        x > maxX - brushSize || y > maxY - brushSize)
                    {
                        var pixelIndex = ImgCoordToPixelIndex(img, x, y);
                        output[pixelIndex] = color;
                    }
                }
            }
        }

        public static void DrawFilledRectangle(PixImage img, Vector2Int start, Vector2Int end, Color color, Color[] output)
        {
            var minX = Mathf.Min(start.x, end.x);
            var maxX = Mathf.Max(start.x, end.x);
            var minY = Mathf.Min(start.y, end.y);
            var maxY = Mathf.Max(start.y, end.y);
            for (int x = minX; x <= maxX; ++x)
            {
                for (int y = minY; y <= maxY; ++y)
                {
                    var pixelIndex = ImgCoordToPixelIndex(img, x, y);
                    output[pixelIndex] = color;
                }
            }
        }

        public static void DrawLine(PixImage img, Vector2Int start, Vector2Int end, Color color, int brushSize, Color[] output)
        {
        }

        public struct FieldWidthScope : IDisposable
        {
            float m_LabelWidth;
            float m_FieldWidth;

            public FieldWidthScope(float labelWidth, float fieldWidth = 0)
            {
                m_LabelWidth = EditorGUIUtility.labelWidth;
                m_FieldWidth = EditorGUIUtility.fieldWidth;

                EditorGUIUtility.labelWidth = labelWidth;
                if (fieldWidth != 0)
                    EditorGUIUtility.fieldWidth = fieldWidth;
            }

            public void Dispose()
            {
                EditorGUIUtility.labelWidth = m_LabelWidth;
                EditorGUIUtility.fieldWidth = m_FieldWidth;
            }
        }

        public static bool IsValidPixSource(UnityEngine.Object obj)
        {
            return obj is PixImage || obj is Sprite || obj is Texture2D;
        }
    }
}