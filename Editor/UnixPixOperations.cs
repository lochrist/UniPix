using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace UniPix
{
    public static class UnixPixOperations
    {
        public static Image CreateImageFromTexture(string path)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                MakeReadable(path, tex);
                var img = Image.CreateImage(tex.width, tex.height);
                ImportTexture2D(tex, img);
                return img;
            }
            return null;
        }

        public static Layer ImportTexture2D(string path, Image img)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null)
            {
                MakeReadable(path, tex);
                return ImportTexture2D(tex, img);
            }
            return null;
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
                tImporter.textureType = TextureImporterType.Default;
                tImporter.isReadable = true;
                AssetDatabase.ImportAsset(path);
                AssetDatabase.Refresh();
                return true;
            }
            return false;
        }

        public static Layer ImportTexture2D(Texture2D tex, Image img)
        {
            // TODO : Crop
            if (tex.width > img.Width)
                throw new Exception($"Texture doesn't width {tex.width} with img width {img.Width}");
            if (tex.height > img.Height)
                throw new Exception($"Texture doesn't width {tex.height} with img width {img.Height}");

            img.Frames[0].AddLayer(img.Width, img.Height);
            var layer = img.Frames[0].Layers[img.Frames[0].Layers.Count - 1];
            layer.Pixels = new Color[img.Width * img.Height];
            for (var x = 0; x < tex.width; ++x)
            {
                for (var y = 0; y < tex.height; ++y)
                {
                    var pixIndex = x + y * tex.height;
                    layer.Pixels[pixIndex] = tex.GetPixel(x, y);
                }
            }

            return layer;
        }

        public static Texture2D CreateTextureFromImg(Image img, int frameIndex)
        {
            var frame = img.Frames[frameIndex];
            for(var layerIndex = 0; layerIndex < frame.Layers.Count; ++layerIndex)
            {
                if (layerIndex == 0)
                {
                    Blend(frame.Layers[layerIndex], frame.Layers[layerIndex], frame.BlendedLayer);
                }
                else
                {
                    Blend(frame.BlendedLayer, frame.Layers[layerIndex], frame.BlendedLayer);
                }
                
            }
            var tex = new Texture2D(img.Width, img.Height);
            tex.SetPixels(frame.BlendedLayer.Pixels);
            tex.Apply();
            return tex;
        }

        public static void Blend(Layer srcLayer, Layer dstLayer, Layer result)
        {
            // Simple alpha blend: https://en.wikipedia.org/wiki/Alpha_compositing
            // outA = srcA + dstA (1 - srcA)
            // outRGB = (srcRGB * srcA + dstRGB * dstA (1 - srcA)) / outA
            
            for(var i = 0; i < srcLayer.Pixels.Length; ++i)
            {
                var src = srcLayer.Pixels[i];
                var dst = dstLayer.Pixels[i];
                var srcA = src.a * srcLayer.Opacity;
                var dstA = src.a * dstLayer.Opacity;
                var outA = srcA + dstA * (1 - srcA);
                result.Pixels[i] = new Color(
                    (src.r * srcA + dst.r * dstA * (1 - srcA)) / outA,
                    (src.g * srcA + dst.g * dstA * (1 - srcA)) / outA,
                    (src.b * srcA + dst.b * dstA * (1 - srcA)) / outA,
                    outA
                );
            }
        }
    }
}