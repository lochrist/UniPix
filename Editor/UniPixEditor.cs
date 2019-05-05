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
        public int CurrentLayerIndex = 0;
        public int CurrentFrameIndex = 0;

        public Frame CurrentFrame => Image.Frames[CurrentFrameIndex];
        public Layer CurrentLayer => CurrentFrame.Layers[CurrentLayerIndex];

        public Color CurrentColor = new Color(1, 0, 0);
        public Color SecondaryColor = Color.clear;

        public Rect ScaledImgRect;
        public Vector2Int CursorImgCoord;
        public Vector2 CursorPos;
    }

    public class PixEditor : EditorWindow
    {
        Rect m_CanvasRect;
        Rect m_StatusRect;
        Rect m_ViewportRect;
        Texture2D m_TransparentTex;

        static class Styles
        {
            public const float kToolPaletteWidth = 100;
            public const float kLayerWidth = 200;
            public const float kToolbarHeight = 35;
            public const float kStatusbarHeight = 35;

            public static GUIStyle layerHeader = new GUIStyle(EditorStyles.boldLabel);
            public static GUIStyle layerName = new GUIStyle(EditorStyles.largeLabel);
            public static GUIStyle currentLayerName = new GUIStyle(EditorStyles.largeLabel);
            public static GUIStyle layerOpacity = new GUIStyle(EditorStyles.numberField);
            public static GUIStyle layerVisible = new GUIStyle(EditorStyles.toggle);
            public static GUIStyle layerLocked = new GUIStyle(EditorStyles.toggle);
            public static GUIStyle layerToolbarBtn = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = 25,
                fixedHeight = 25
            };


            static Styles()
            {
                currentLayerName.normal.textColor = Color.yellow;
            }
        }

        float m_imageOffsetX;
        float m_imageOffsetY;

        SessionData m_Session;

        PixTool m_CurrentTool;

        private void OnEnable()
        {
            m_CurrentTool = new BrushTool();
            // m_CurrentTool = new EraseTool();

            m_Session = new SessionData();

            ResetImage();

            m_Session.ZoomLevel = 10f;

            m_TransparentTex = new Texture2D(1, 1);
            m_TransparentTex.SetPixel(0, 0, Color.clear);
            m_TransparentTex.Apply();

            wantsMouseMove = true;
        }

        private void OnDisable()
        {

        }

        private void ResetImage()
        {
            m_Session.CurrentFrameIndex = 0;
            m_Session.CurrentLayerIndex = 0;

            // TODO: Hardcoded for now:
            // m_Session.Image = UnixPixOperations.CreateImageFromTexture("Assets/Sprites/archer_1.png");
            m_Session.Image = UnixPixOperations.CreateImage(2, 2, Color.yellow);
            m_Session.CurrentLayer.Pixels[0] = Color.clear;
            var newLayer = m_Session.CurrentFrame.AddLayer(m_Session.Image.Width, m_Session.Image.Height);
            for (int i = 0; i < newLayer.Pixels.Length; ++i)
                newLayer.Pixels[i] = Color.blue;
            newLayer.Opacity = 0.7f;
        }

        private void OnGUI()
        {
            ProcessEvents();
            ComputeLayout();
            DrawToolbar();
            DrawPixEditor();
            DrawLayers();
            DrawStatus();
        }

        private void ComputeLayout()
        {
            
            var canvasWidth = position.width - Styles.kToolPaletteWidth - Styles.kLayerWidth;

            m_CanvasRect = new Rect(Styles.kToolPaletteWidth, 
                Styles.kToolbarHeight, 
                position.width - Styles.kToolPaletteWidth - Styles.kLayerWidth, 
                position.height - Styles.kToolbarHeight - Styles.kStatusbarHeight);

            const float statusHeight = 75;
            m_StatusRect = new Rect(m_CanvasRect.xMax, position.height - Styles.kStatusbarHeight - statusHeight, Styles.kLayerWidth, statusHeight);
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

            var layerRect = new Rect(m_CanvasRect.xMax + 2, m_CanvasRect.y, Styles.kLayerWidth, m_CanvasRect.height - m_StatusRect.height);
            GUILayout.BeginArea(layerRect);
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("Layers", Styles.layerHeader);
                using (new GUILayout.HorizontalScope())
                {
                    
                    if (GUILayout.Button("Cr", Styles.layerToolbarBtn))
                    {

                    }
                    GUILayout.Button("Up", Styles.layerToolbarBtn);
                    GUILayout.Button("Dw", Styles.layerToolbarBtn);
                    GUILayout.Button("Mer", Styles.layerToolbarBtn);
                    GUILayout.Button("Del", Styles.layerToolbarBtn);
                }

                for (var i = m_Session.CurrentFrame.Layers.Count - 1; i >= 0; i--)
                {
                    var layer = m_Session.CurrentFrame.Layers[i];
                    GUILayout.BeginHorizontal();

                    // TODO: current selected layer update
                    if (GUILayout.Button(layer.Name, i == m_Session.CurrentLayerIndex ? Styles.currentLayerName : Styles.layerName))
                    {
                        m_Session.CurrentLayerIndex = i;
                        Repaint();
                    }

                    
                    EditorGUI.BeginChangeCheck();
                    // Doesn't format well??
                    // EditorGUILayout.FloatField("Alpha", 0.5f, Styles.layerOpacity);
                    layer.Opacity = Mathf.Clamp(EditorGUILayout.FloatField(layer.Opacity, Styles.layerOpacity), 0f, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // TODO undo
                        Repaint();
                    }

                    EditorGUI.BeginChangeCheck();
                    layer.Visible = EditorGUILayout.Toggle(layer.Visible, Styles.layerVisible);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // TODO undo
                        Repaint();
                    }

                    
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndArea();
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
            m_Session.ScaledImgRect = new Rect((m_CanvasRect.width / 2 - xScale / 2) + m_imageOffsetX, m_imageOffsetY, xScale, yScale);

            EditorGUI.DrawRect(m_CanvasRect, new Color(0.4f, 0.4f, 0.4f));
            GUILayout.BeginArea(m_CanvasRect);
            {
                EditorGUI.DrawTextureTransparent(m_Session.ScaledImgRect, m_TransparentTex);
                var tex = UnixPixOperations.CreateTextureFromImg(m_Session.Image, m_Session.CurrentFrameIndex);
                GUI.DrawTexture(m_Session.ScaledImgRect, tex);

                if (m_Session.ScaledImgRect.Contains(Event.current.mousePosition))
                {
                    m_Session.CursorPos = Event.current.mousePosition - m_Session.ScaledImgRect.position;
                    m_Session.CursorImgCoord = new Vector2Int((int)(m_Session.CursorPos.x / m_Session.ZoomLevel), (int)(m_Session.CursorPos.y / m_Session.ZoomLevel));
                    if (m_CurrentTool.OnEvent(Event.current, m_Session))
                    {
                        Repaint();
                    }
                }

                if (m_Session.ZoomLevel > 2)
                {
                    DrawGrid();
                }
                // var mousePos = Event.current.mousePosition - m_CanvasRect.position;
                // Debug.Log($"Event.current.mousePosition: {Event.current.mousePosition} mousePos: {mousePos} pos: {m_CanvasRect.position} rec: {m_CanvasRect}");
            }

            GUILayout.EndArea();
        }

        private void DrawGrid()
        {
            for (int x = 0; x <= m_Session.Image.Width; x += 1)
            {
                float posX = m_Session.ScaledImgRect.xMin + m_Session.ZoomLevel * x/* - 0.2f*/;
                EditorGUI.DrawRect(new Rect(posX, m_Session.ScaledImgRect.yMin, 1, m_Session.ScaledImgRect.height), Color.black);
            }
            // Then x axis
            for (int y = 0; y <= m_Session.Image.Height; y += 1)
            {
                float posY = m_Session.ScaledImgRect.yMin + m_Session.ZoomLevel * y/* - 0.2f*/;
                EditorGUI.DrawRect(new Rect(m_Session.ScaledImgRect.xMin, posY, m_Session.ScaledImgRect.width, 1), Color.black);
            }
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
            var toolbarRect = new Rect(0, 0, position.width, Styles.kToolbarHeight);
            GUILayout.BeginArea(toolbarRect);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Reset"))
            {
                ResetImage();
                Repaint();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
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
