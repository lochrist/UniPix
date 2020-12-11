using System;
using UniPix;
using UnityEngine;

public static class PixCore
{
    #region Image
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

    #endregion

    #region Layer
    public static void SetLayerColor(Layer layer, Color color)
    {
        for (var i = 0; i < layer.Pixels.Length; ++i)
        {
            layer.Pixels[i] = color;
        }
    }
    #endregion

    #region Texture
    public static Texture2D CreateTexture(int width, int height)
    {
        return new Texture2D(width, height) { filterMode = FilterMode.Point };
    }

    public static void SetTextureColor(Texture2D tex, Color color, bool apply = true)
    {
        for (var x = 0; x < tex.width; ++x)
            for (var y = 0; y < tex.height; ++y)
                tex.SetPixel(x, y, color);
        tex.Apply();
    }
    #endregion

    #region Draw
    public static void BlendLayer(Layer srcLayer, Layer dstLayer, Layer result)
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

    public static Color BlendPixel(Color src, Color dst)
    {
        // Simple alpha blend: https://en.wikipedia.org/wiki/Alpha_compositing
        // outA = srcA + dstA (1 - srcA)
        // outRGB = (srcRGB * srcA + dstRGB * dstA (1 - srcA)) / outA
        var srcA = src.a;
        var dstA = dst.a;
        var outA = srcA + dstA * (1 - srcA);
        var safeOutA = outA == 0.0f ? 1.0f : outA;
        return new Color(
            (src.r * srcA + dst.r * dstA * (1 - srcA)) / safeOutA,
            (src.g * srcA + dst.g * dstA * (1 - srcA)) / safeOutA,
            (src.b * srcA + dst.b * dstA * (1 - srcA)) / safeOutA,
            outA
        );
    }

    public static int ImgCoordToPixelIndex(PixImage img, int imgCoordX, int imgCoordY)
    {
        // Texture are Bottom to Top
        // Our cord system is top to bottom.
        var y = img.Height - imgCoordY - 1;
        return imgCoordX + y * img.Width;
    }

    public class Region
    {
        public int Width;
        public int Height;
        public Color[] Pixels;

        public Region(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Color[Width * Height];
        }
    }

    public static Region GetRegion(PixImage img, RectInt regionRect, Color[] input)
    {
        var region = new Region(regionRect.width, regionRect.height);
        var regionIndex = 0;
        IterateImgRect(img, regionRect, (x, y, pixelIndex) =>
        {
            region.Pixels[regionIndex++] = input[pixelIndex];
        });

        return region;
    }

    public static void DrawRegion(PixImage img, Vector2Int origin, Region region, Color[] output)
    {
        var r = new RectInt(origin.x, origin.y, region.Width, region.Height);
        var regionPixelIndex = 0;
        IterateImgRect(img, r, (x, y, pixelIndex) =>
        {
            output[pixelIndex] = region.Pixels[regionPixelIndex++];
        });
    }

    public static void IterateImgRect(PixImage img, RectInt r, Action<int, int, int> action)
    {
        r = ClipRectangleToImg(img, r);
        for (var x = r.xMin; x < r.xMax; ++x)
        {
            for (var y = r.yMin; y < r.yMax; ++y)
            {
                var pixelIndex = ImgCoordToPixelIndex(img, x, y);
                action(x, y, pixelIndex);
            }
        }
    }

    public static RectInt ClipRectangleToImg(PixImage img, RectInt rect)
    {
        var minX = Mathf.Max(rect.x, 0);
        var minY = Mathf.Max(rect.y, 0);
        var maxX = Mathf.Min(minX + rect.width, img.Width);
        var maxY = Mathf.Min(minY + rect.height, img.Height);
        return new RectInt(minX, minY, maxX - minX, maxY - minY);
    }

    public static RectInt GetRect(Vector2Int p1, Vector2Int p2)
    {
        var minX = Mathf.Min(p1.x, p2.x);
        var maxX = Mathf.Max(p1.x, p2.x);
        var minY = Mathf.Min(p1.y, p2.y);
        var maxY = Mathf.Max(p1.y, p2.y);
        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public static void DrawRectangle(PixImage img, Vector2Int start, Vector2Int end, Color color, int brushSize, Color[] output)
    {
        DrawRectangle(img, GetRect(start, end), color, brushSize, output);
    }

    public static void DrawRectangle(PixImage img, RectInt r, Color color, int brushSize, Color[] output)
    {
        IterateImgRect(img, r, (x, y, pixelIndex) =>
        {
            if (x < r.xMin + brushSize || y < r.yMin + brushSize ||
                x >= r.xMax - brushSize || y >= r.yMax - brushSize)
            {
                output[pixelIndex] = color;
            }
        });
    }

    public static void DrawFilledRectangle(PixImage img, Vector2Int start, Vector2Int end, Color color, Color[] output)
    {
        DrawFilledRectangle(img, GetRect(start, end), color, output);
    }

    public static void DrawFilledRectangle(PixImage img, RectInt rect, Color color, Color[] output)
    {
        IterateImgRect(img, rect, (x, y, pixelIndex) =>
        {
            output[pixelIndex] = color;
        });
    }

    public static void DrawLine(PixImage img, Vector2Int start, Vector2Int end, Color color, int brushSize, Color[] output)
    {
        // Bresenham line algorithm
        var w = end.x - start.x;
        var h = end.y - start.y;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
        if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
        int longest = Math.Abs(w);
        int shortest = Math.Abs(h);
        if (!(longest > shortest))
        {
            longest = Math.Abs(h);
            shortest = Math.Abs(w);
            if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
            dx2 = 0;
        }
        int numerator = longest >> 1;
        int x = start.x, y = start.y;
        for (int i = 0; i <= longest; i++)
        {
            var pixelIndex = ImgCoordToPixelIndex(img, x, y);
            output[pixelIndex] = color;
            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }
    }

    public static void FloodFill(PixImage img, Vector2Int imgCoord, Color targetColor, Color newColor, Color[] output)
    {
        if (targetColor == newColor)
            return;

        var pixelIndex = ImgCoordToPixelIndex(img, imgCoord.x, imgCoord.y);
        var color = output[pixelIndex];
        if (color != targetColor)
            return;

        output[pixelIndex] = newColor;
        if (imgCoord.y - 1 >= 0)
            FloodFill(img, new Vector2Int(imgCoord.x, imgCoord.y - 1), targetColor, newColor, output);
        if (imgCoord.y + 1 < img.Height)
            FloodFill(img, new Vector2Int(imgCoord.x, imgCoord.y + 1), targetColor, newColor, output);
        if (imgCoord.x - 1 >= 0)
            FloodFill(img, new Vector2Int(imgCoord.x - 1, imgCoord.y), targetColor, newColor, output);
        if (imgCoord.x + 1 < img.Width)
            FloodFill(img, new Vector2Int(imgCoord.x + 1, imgCoord.y), targetColor, newColor, output);
    }

    #endregion
}
