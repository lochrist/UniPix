using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniPix;
using UnityEditor;
using UnityEngine;

public static class PixIO
{
    public static bool useProject = false;

    public static string GetImagePath(PixImage img, string defaultValue)
    {
        return useProject ? AssetDatabase.GetAssetPath(img) : defaultValue;
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

        if (!PixIO.useProject)
        {
            throw new Exception("Texture not readable and using Project");
        }

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
        if (!PixIO.useProject)
            return;
        var tImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        if (tImporter != null && tImporter.textureCompression != TextureImporterCompression.Uncompressed)
        {
            tImporter.textureCompression = TextureImporterCompression.Uncompressed;
            AssetDatabase.ImportAsset(path);
            AssetDatabase.Refresh();
        }
    }


    public static string SaveFrame(Frame frame, string path)
    {
        var frameTex = frame.Texture;
        var frameContent = frameTex.EncodeToPNG();
        File.WriteAllBytes(path, frameContent);

        if (useProject)
        {
            AssetDatabase.Refresh();
            var updateMetaFile = !File.Exists(path);
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
        }
        
        return path;
    }

    public static void SaveSpriteSheet(Texture2D spriteSheet, int frameWidth, int frameHeight, string path)
    {
        var content = spriteSheet.EncodeToPNG();
        File.WriteAllBytes(path, content);
        if (useProject)
        {
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
        }
    }

    public static string ExportFramesToSpriteSheet(PixSession session, Frame[] frames = null)
    {
        // ask user for base name: give image as base name
        // Save as a sprite sheet
        // Ensure to properly update SpriteMetadata

        // TODO useproject -> ensure we do not choose Assets folder
        frames = frames ?? session.Image.Frames.ToArray();
        var baseFolder = String.IsNullOrEmpty(session.Image.Path) ? "Assets/" : Path.GetDirectoryName(session.Image.Path);
        var baseName = String.IsNullOrEmpty(session.Image.Path) ? "sprite_sheet.png" : $"{PixUtils.GetBaseName(session.Image.Path)}_sheet";
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

        var spriteSheet = PixCore.CreateTexture(spriteSheetWidth, spriteSheetHeight);
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

        PixIO.SaveSpriteSheet(spriteSheet, frameWidth, frameHeight, path);
        return path;
    }

    public static string[] ExportFrames(PixSession session, Frame[] frames = null)
    {
        // ask user for base name: give image as base name
        // Save each image separately
        // Ensure to properly update SpriteMetadata
        frames = frames ?? session.Image.Frames.ToArray();
        if (frames.Length == 0)
            return null;

        // TODO useproject -> ensure we do not choose Assets folder
        var baseFolder = String.IsNullOrEmpty(session.Image.Path) ? "Assets/" : Path.GetDirectoryName(session.Image.Path);
        var baseName = String.IsNullOrEmpty(session.Image.Path) ? "pix.png" : PixUtils.GetBaseName(session.Image.Path);
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
            frameFilePaths.Add(PixIO.SaveFrame(frame, PixUtils.GetUniquePath(basePath, ".png", i)));
        }

        return frameFilePaths.ToArray();
    }

    #region Project Specific
    public static void UpdateFrameSourceSprite(Frame frame)
    {
        Debug.Assert(useProject);
        if (frame.SourceSprite == null)
            throw new Exception("Not implemented");

        var spriteSize = PixCore.GetSpriteSize(frame.SourceSprite);
        if (spriteSize.x != frame.Width || spriteSize.y != frame.Height)
            throw new Exception("UpdateFrameSourceSprite: Frame doesn't match sprite size");

        var texture = frame.SourceSprite.texture;
        var texturePath = AssetDatabase.GetAssetPath(texture);
        if (String.IsNullOrEmpty(texturePath))
            throw new Exception("Texture not bound to a path");

        MakeUncompressed(texturePath, texture);

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

    public static void UpdateImageSourceSprites(PixSession session, bool spriteSheet = false)
    {
        PixCommands.SavePix(session);
        if (String.IsNullOrEmpty(session.Image.Path))
        {
            // SavePix was cancelled.
            return;
        }

        if (spriteSheet)
        {
            var linkedFrames = session.Image.Frames.Where(f => f.SourceSprite != null).ToArray();
            foreach (var linkedFrame in linkedFrames)
            {
                UpdateFrameSourceSprite(linkedFrame);
            }

            // TODO Export => not linked
            var unlinkedFrame = session.Image.Frames.Where(f => f.SourceSprite == null).ToArray();
            var sheet = ExportFramesToSpriteSheet(session, unlinkedFrame);
            // Need to relink each sprite to their frame.
        }
        else
        {
            string basePath = null;
            for (int i = 0; i < session.Image.Frames.Count; i++)
            {
                var frame = session.Image.Frames[i];
                if (frame.SourceSprite == null)
                {
                    if (basePath == null)
                    {
                        string path = EditorUtility.SaveFilePanel(
                            "Export as image",
                            "Assets/", String.IsNullOrEmpty(session.Image.Path) ? "pix.png" : PixUtils.GetBaseName(session.Image.Path), "png");
                        basePath = path == "" ? PixUtils.GetBasePath(session.Image.Path) : PixUtils.GetBasePath(path);
                    }

                    // One image per frame
                    var framePath = PixUtils.GetUniquePath(basePath, ".png", i);
                    framePath = PixIO.SaveFrame(frame, framePath);
                    frame.SourceSprite = AssetDatabase.LoadAssetAtPath<Sprite>(framePath);
                }
                else
                {
                    UpdateFrameSourceSprite(frame);
                }
            }
        }
    }

    
    #endregion
}
