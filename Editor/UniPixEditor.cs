using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public class PixEditor : EditorWindow
    {
        Rect m_CanvasRect;
        Rect m_ViewportRect;
        Rect m_ScaledImgRect;
        float m_ZoomLevel = 20f;
        int m_ImageWidth = 16;
        int m_ImageHeight = 32;

        float m_imageOffsetX;
        float m_imageOffsetY;
        Texture2D m_TransparentTex;

        private void OnEnable()
        {
            m_ZoomLevel = 10f;
            Debug.Log(m_ZoomLevel);

            m_TransparentTex = new Texture2D(1, 1);
            m_TransparentTex.SetPixel(0, 0, Color.clear);
            m_TransparentTex.Apply();
        }

        private void OnDisable()
        {

        }

        private void OnGUI()
        {
            ProcessEvents();
            ComputeLayout();
            DrawPixEditor();
        }

        private void ComputeLayout()
        {
            const float toolPaletteWidth = 100;
            const float layerWidth = 100;
            const float toolbarHeight = 35;
            const float statusbarHeight = 35;
            var canvasWidth = position.width - toolPaletteWidth - layerWidth;

            m_CanvasRect = new Rect(toolPaletteWidth, toolbarHeight, position.width - toolPaletteWidth - layerWidth, position.height - toolbarHeight - statusbarHeight);
        }

        private void ProcessEvents()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.W)
                {
                    m_imageOffsetY -= 20f;
                }
                if (e.keyCode == KeyCode.S)
                {
                    m_imageOffsetY += 20f;
                }
                if (e.keyCode == KeyCode.A)
                {
                    m_imageOffsetX -= 20f;
                }
                if (e.keyCode == KeyCode.D)
                {
                    m_imageOffsetX += 20f;
                }
                if (e.keyCode == KeyCode.UpArrow)
                {
                    m_ZoomLevel += 2f;
                }
                if (e.keyCode == KeyCode.DownArrow)
                {
                    m_ZoomLevel -= 2f;
                    m_ZoomLevel = Mathf.Max(1, m_ZoomLevel);
                }
                Repaint();
            }
            else if (e.type == EventType.ScrollWheel)
            {
                m_ZoomLevel -= e.delta.y;
                m_ZoomLevel = Mathf.Max(1, m_ZoomLevel);
                Repaint();
            }
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
            // Pencil (p)
            // erase (e)
            // Bucket (b)
        }

        private void DrawFrames()
        {
            // Add frames
            // Clone frames (shift + N)
            // reorder frames

            // New frame (n)
            // Select previous frame (up arrow)
            // Select next frame (up arrow)

        }

        private void DrawColorSwitcher()
        {
            // Similar to Paint : current color + secondary color (on right click) (X)
            // Swap Color
            // Reset default color (d)
            // Open palette creation (alt + p)
        }

        private void DrawPixEditor()
        {
            // Draw the image itself
            // Grid

            // Zoom
            // reset zoom level (0)
            // increase zoom level (+)
            // decrease zoom level (-)
            // increase pen size ([)
            // decrease pen size (])

            var xScale = m_ImageWidth * m_ZoomLevel;
            var yScale = m_ImageHeight * m_ZoomLevel;
            m_ScaledImgRect = new Rect((m_CanvasRect.width / 2 - xScale / 2) + m_imageOffsetX, m_imageOffsetY, xScale, yScale);

            EditorGUI.DrawRect(m_CanvasRect, new Color(0.4f, 0.4f, 0.4f));
            GUILayout.BeginArea(m_CanvasRect);
            {
                EditorGUI.DrawTextureTransparent(m_ScaledImgRect, m_TransparentTex);

                if (m_ZoomLevel > 2)
                {
                    for (int x = 0; x <= m_ImageWidth; x += 1)
                    {
                        float posX = m_ScaledImgRect.xMin + m_ZoomLevel * x/* - 0.2f*/;
                        EditorGUI.DrawRect(new Rect(posX, m_ScaledImgRect.yMin, 1, m_ScaledImgRect.height), Color.black);
                    }
                    // Then x axis
                    for (int y = 0; y <= m_ImageHeight; y += 1)
                    {
                        float posY = m_ScaledImgRect.yMin + m_ZoomLevel * y/* - 0.2f*/;
                        EditorGUI.DrawRect(new Rect(m_ScaledImgRect.xMin, posY, m_ScaledImgRect.width, 1), Color.black);
                    }
                }
            }

            GUILayout.EndArea();
        }

        private void DrawAnimationPreview()
        {
            // Preview
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

        private void DrawSettings()
        {
            // Toggle grid (alt + g)

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
