using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace UniPix
{
    public static class UniPixUtils
    {
        public static Image CreateImage(int width, int height, Color baseColor)
        {
            var img = Image.CreateImage(width, height);
            var newFrame = img.AddFrame();
            var layer = newFrame.AddLayer();
            for (var i = 0; i < layer.Pixels.Length; ++i)
            {
                layer.Pixels[i] = baseColor;
            }

            return img;
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

        public static Layer ImportFrame(ref Image img, Texture2D tex)
        {
            if (img == null)
            {
                img = Image.CreateImage(tex.width, tex.height);
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

        public static void ImportFrames(ref Image img, Sprite[] sprites)
        {
            foreach (var sprite in sprites)
            {
                ImportFrame(ref img, sprite);
            }
        }

        public static void ImportFrame(ref Image img, Sprite sprite)
        {
            var texture = sprite.texture;
            if (!texture || texture == null)
                return;

            var frameSize = GetSpriteSize(sprite);
            if (img == null)
            {
                img = Image.CreateImage(frameSize.x, frameSize.y);
            }

            if (frameSize.x > img.Height)
                return;

            if (frameSize.y > img.Width)
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
    }
}