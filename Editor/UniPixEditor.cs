using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    [System.Serializable]
    public class SessionData
    {
        public float ZoomLevel = 20f;
        public Image Image;
        public int CurrentLayer = 0;
        public int CurrentFrame = 0;
    }

    public class PixEditor : EditorWindow
    {
        Rect m_CanvasRect;
        Rect m_StatusRect;
        Rect m_ViewportRect;
        Rect m_ScaledImgRect;
        Texture2D m_TransparentTex;
        
        float m_imageOffsetX;
        float m_imageOffsetY;

        SessionData m_Session;

        Color m_CurrentColor = new Color(1, 0, 0);
        Color m_SecondaryColor = new Color(0, 1, 0);
        // TODO: cursor position uses color swapper
        readonly Color kCursorColor = new Color(1, 1, 1, 0.5f);

        private void OnEnable()
        {
            m_Session = new SessionData();
            m_Session.Image = UnixPixOperations.CreateImageFromTexture("Assets/Sprites/archer_1.png");
            m_Session.ZoomLevel = 10f;

            m_TransparentTex = new Texture2D(1, 1);
            m_TransparentTex.SetPixel(0, 0, Color.clear);
            m_TransparentTex.Apply();

            wantsMouseMove = true;
        }

        private void OnDisable()
        {

        }

        private void OnGUI()
        {
            ProcessEvents();
            ComputeLayout();
            DrawPixEditor();
            DrawStatus();
        }

        private void ComputeLayout()
        {
            const float toolPaletteWidth = 100;
            const float layerWidth = 100;
            const float toolbarHeight = 35;
            const float statusbarHeight = 35;
            var canvasWidth = position.width - toolPaletteWidth - layerWidth;

            m_CanvasRect = new Rect(toolPaletteWidth, toolbarHeight, position.width - toolPaletteWidth - layerWidth, position.height - toolbarHeight - statusbarHeight);

            const float statusHeight = 75;
            m_StatusRect = new Rect(m_CanvasRect.xMax, position.height - statusbarHeight - statusHeight, layerWidth, statusHeight);
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
                    m_Session.ZoomLevel += 2f;
                }
                if (e.keyCode == KeyCode.DownArrow)
                {
                    m_Session.ZoomLevel -= 2f;
                    m_Session.ZoomLevel = Mathf.Max(1, m_Session.ZoomLevel);
                }
                Repaint();
            }
            else if (e.type == EventType.ScrollWheel)
            {
                m_Session.ZoomLevel -= e.delta.y;
                m_Session.ZoomLevel = Mathf.Max(1, m_Session.ZoomLevel);
                Repaint();
            }
            else if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag)
            {
                // Repaint to update cursor position
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
            var xScale = m_Session.Image.Width * m_Session.ZoomLevel;
            var yScale = m_Session.Image.Height * m_Session.ZoomLevel;
            m_ScaledImgRect = new Rect((m_CanvasRect.width / 2 - xScale / 2) + m_imageOffsetX, m_imageOffsetY, xScale, yScale);

            EditorGUI.DrawRect(m_CanvasRect, new Color(0.4f, 0.4f, 0.4f));
            GUILayout.BeginArea(m_CanvasRect);
            {
                EditorGUI.DrawTextureTransparent(m_ScaledImgRect, m_TransparentTex);
                var tex = UnixPixOperations.CreateTextureFromImg(m_Session.Image, m_Session.CurrentFrame);
                GUI.DrawTexture(m_ScaledImgRect, tex);

                if (m_ScaledImgRect.Contains(Event.current.mousePosition))
                {
                    var mousePosInImg = Event.current.mousePosition - m_ScaledImgRect.position;
                    var pixelPos = new Vector2Int((int)(mousePosInImg.x / m_Session.ZoomLevel), (int)(mousePosInImg.y / m_Session.ZoomLevel));
                    var cursorPos = new Vector2(pixelPos.x * m_Session.ZoomLevel, pixelPos.y * m_Session.ZoomLevel) + m_ScaledImgRect.position;
                    EditorGUI.DrawRect(new Rect(cursorPos, new Vector2(m_Session.ZoomLevel, m_Session.ZoomLevel)), kCursorColor);

                    if (Event.current.isMouse && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag))
                    {
                        // TODO: undo + toolhandling
                        var pixelIndex = pixelPos.x + (m_Session.Image.Height - pixelPos.y - 1) * m_Session.Image.Height;
                        m_Session.Image.Frames[m_Session.CurrentFrame].Layers[m_Session.CurrentLayer].Pixels[pixelIndex] = m_CurrentColor;
                        Repaint();
                    }
                }

                if (m_Session.ZoomLevel > 2)
                {
                    for (int x = 0; x <= m_Session.Image.Width; x += 1)
                    {
                        float posX = m_ScaledImgRect.xMin + m_Session.ZoomLevel * x/* - 0.2f*/;
                        EditorGUI.DrawRect(new Rect(posX, m_ScaledImgRect.yMin, 1, m_ScaledImgRect.height), Color.black);
                    }
                    // Then x axis
                    for (int y = 0; y <= m_Session.Image.Height; y += 1)
                    {
                        float posY = m_ScaledImgRect.yMin + m_Session.ZoomLevel * y/* - 0.2f*/;
                        EditorGUI.DrawRect(new Rect(m_ScaledImgRect.xMin, posY, m_ScaledImgRect.width, 1), Color.black);
                    }
                }

                // var mousePos = Event.current.mousePosition - m_CanvasRect.position;
                // Debug.Log($"Event.current.mousePosition: {Event.current.mousePosition} mousePos: {mousePos} pos: {m_CanvasRect.position} rec: {m_CanvasRect}");
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
            // EditorGUI.DrawRect(m_StatusRect, new Color(1f, 0.4f, 0.4f));

            GUILayout.BeginArea(m_StatusRect);
            {
                GUILayout.BeginVertical();
                GUILayout.Label($"x{m_Session.ZoomLevel}");
                GUILayout.Label($"[{m_Session.Image.Width}x{m_Session.Image.Height}]");
                // GUILayout.Label($"x{m_ZoomLevel}");
                GUILayout.EndVertical();
            }
            GUILayout.EndArea();

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

}
