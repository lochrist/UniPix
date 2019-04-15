using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public class PixEditor : EditorWindow
    {
        private void OnEnable()
        {

        }

        private void OnDisable()
        {

        }

        private void DrawLayers()
        {
            // Add
            // Delete
            // Move
            // Opacity
            // Hide
            // Lock
            // Name
            // Merge with layer below
        }

        private void DrawToolPalette()
        {
            // Pencil
            // erase
            // Bucket
        }

        private void DrawFrames()
        {
            // Add frames
            // Clone frames
            // reorder frames
        }

        private void DrawColorSwitcher()
        {
            // Similar to Paint : current color + secondary color (on right click)
        }

        private void DrawPixEditor()
        {
            // Draw the image itself
            // Grid
            // Zoom
        }

        private void DrawAnimationPreview()
        {
            // PReview
            // FPS
        }

        private void DrawTools()
        {
            // PEncil
            // Eraser
            // Bucket
        }

        private void DrawPalette()
        {
            // Draw Tiles with each different colors in image
            // allow save + load of a palette
        }

        private void DrawToolbar()
        {
            // settings
            // Save
            // Export
            // New
            // Import
            // Duplicate
        }

        private void DrawStatus()
        {
            // Mouse pos
            // Zoom factor
            // image size
            // Frame Index
            // Active layer
        }

        [MenuItem("Window/UniPix")]
        static void ShowUniPix()
        {
            GetWindow<PixEditor>();
        }



    }


    public static class UniPixMisc
    {
        public static UniPix.Image CreateDummyImg()
        {
            var img = ScriptableObject.CreateInstance<UniPix.Image>();
            img.Height = 12;
            img.Width = 12;

            img.Frames = new List<UniPix.Frame>();

            {
                var f = new UniPix.Frame();
                f.Layers = new List<UniPix.Layer>();
                var l = new UniPix.Layer();
                l.Name = "Background";
                l.Opacity = 0.3f;
                l.Pixels = new Color[16];
                l.Pixels[0] = Color.black;
                l.Pixels[1] = Color.green;
                f.Layers.Add(l);

                l = new UniPix.Layer();
                l.Name = "L1";
                l.Opacity = 0.3f;
                l.Pixels = new Color[16];
                l.Pixels[0] = Color.blue;
                l.Pixels[1] = Color.red;
                f.Layers.Add(l);

                img.Frames.Add(f);
            }

            {
                var f = new UniPix.Frame();
                f.Layers = new List<UniPix.Layer>();
                var l = new UniPix.Layer();
                l.Name = "Background";
                l.Opacity = 0.3f;
                l.Pixels = new Color[16];
                l.Pixels[0] = Color.cyan;
                l.Pixels[1] = Color.gray;
                f.Layers.Add(l);
                img.Frames.Add(f);
            }

            return img;
        }

        [MenuItem("Tools/Create and Save Dummy")]
        static void CreateAndSave()
        {
            var img = CreateDummyImg();
            AssetDatabase.CreateAsset(img, "Assets/Dummy.asset");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Create Mods")]
        static void CreateAndMods()
        {
            var img = AssetDatabase.LoadAssetAtPath<UniPix.Image>("Assets/Dummy.asset");
            Undo.RecordObject(img, "Img width");
            img.Width = 7;
            Undo.FlushUndoRecordObjects();
        }
    }
}
