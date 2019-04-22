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
            var layer = new Layer();
            // TODO : Crop
            if (tex.width > img.Width)
                throw new Exception($"Texture doesn't width {tex.width} with img width {img.Width}");
            if (tex.height > img.Height)
                throw new Exception($"Texture doesn't width {tex.height} with img width {img.Height}");

            layer.Pixels = new Color[img.Width * img.Height];
            for (var x = 0; x < tex.width; ++x)
            {
                for (var y = 0; y < tex.height; ++y)
                {
                    var pixIndex = x * tex.width + y;
                    layer.Pixels[pixIndex] = tex.GetPixel(x, y);
                }
            }

            img.Frames[0].Layers.Add(layer);

            return layer;
        }

    }

}