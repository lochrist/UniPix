using System.Collections.Generic;
using System.Linq;
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
        public Color SecondaryColor = Color.black;

        public Rect ScaledImgRect;
        public Vector2Int CursorImgCoord;
        public Vector2 CursorPos;
        public int CursorPixelIndex => CursorImgCoord.x + (Image.Height - CursorImgCoord.y - 1) * Image.Height;

        public Palette palette;
    }

    public class PixEditor : EditorWindow
    {
        Rect m_CanvasRect;
        Rect m_StatusRect;
        Rect m_ViewportRect;
        Rect m_LayerRect;
        Rect m_PaletteRect;
        Rect m_ToolbarRect;
        Rect m_ColorPaletteRect;
        Rect m_SettingsRect;
        Texture2D m_TransparentTex;

        public static class Prefs
        {
            public static string kPrefix = "unixpix.";
            public static string kCurrentImg = $"{kPrefix}currentImg";
        }

        static class Styles
        {
            public const float kToolPaletteWidth = 100;
            public const float kLayerWidth = 200;
            public const float kToolbarHeight = 35;
            public const float kStatusbarHeight = 35;
            public const float kColorSwatchSize = 40;
            public const float kPaletteItemSize = 25;
            public const float kLayerHeight = 25;
            public const float kLayerRectHeight = 6 * kLayerHeight;
            public const float kSettingsHeight = 200;
            public const float kMargin = 2;

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

        bool m_ShowGrid = true;
        int m_GridSize = 1;
        Color m_GridColor = Color.black;

        PixTool m_CurrentTool;

        private void OnEnable()
        {
            m_CurrentTool = new BrushTool();
            // m_CurrentTool = new EraseTool();

            m_Session = new SessionData();

            UniPixCommands.LoadPix(m_Session, EditorPrefs.GetString(Prefs.kCurrentImg, null));

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

            // DrawDebugArea();
            
            DrawToolbar();
            DrawColorSwitcher();
            DrawPixEditor();
            DrawLayers();
            DrawColorPalette();
            DrawSettings();
            DrawStatus();
            
        }

        private void ComputeLayout()
        {
            m_ToolbarRect = new Rect(Styles.kMargin, Styles.kMargin, position.width - 2*Styles.kMargin, Styles.kToolbarHeight);
            m_CanvasRect = new Rect(Styles.kToolPaletteWidth, 
                m_ToolbarRect.yMax + Styles.kMargin,
                position.width - Styles.kToolPaletteWidth - Styles.kLayerWidth, 
                position.height - Styles.kToolbarHeight - Styles.kStatusbarHeight - 2*Styles.kMargin);

            const float kRightPanelWidth = Styles.kLayerWidth - 2 * Styles.kMargin;
            m_LayerRect = new Rect(m_CanvasRect.xMax + Styles.kMargin, m_CanvasRect.y, kRightPanelWidth, Styles.kLayerRectHeight);

            m_ColorPaletteRect = new Rect(m_CanvasRect.xMax + Styles.kMargin, m_LayerRect.yMax + Styles.kMargin, kRightPanelWidth, Styles.kLayerRectHeight);

            m_StatusRect = new Rect(m_CanvasRect.xMax + Styles.kMargin, position.height - Styles.kStatusbarHeight - Styles.kStatusbarHeight, kRightPanelWidth, Styles.kStatusbarHeight);

            m_SettingsRect = new Rect(m_CanvasRect.xMax + Styles.kMargin, m_ColorPaletteRect.yMax + Styles.kMargin, kRightPanelWidth, Styles.kSettingsHeight);
        }

        private void DrawDebugArea()
        {
            DrawDebugRect(m_CanvasRect, "canvas", Color.white);
            DrawDebugRect(m_StatusRect, "status", Color.red);
            DrawDebugRect(m_LayerRect, "layer", Color.cyan);
            DrawDebugRect(m_ToolbarRect, "toolbar", Color.green);
            DrawDebugRect(m_ColorPaletteRect, "palette", Color.gray);
            DrawDebugRect(m_SettingsRect, "settings", Color.blue);
        }

        private void DrawDebugRect(Rect rect, string title, Color c)
        {
            EditorGUI.DrawRect(rect, c);
            GUI.Label(rect, title);
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

            GUILayout.BeginArea(m_LayerRect);
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("Layers", Styles.layerHeader);
                using (new GUILayout.HorizontalScope())
                {
                    // TODO: handle undo
                    if (GUILayout.Button("Cr", Styles.layerToolbarBtn))
                    {
                        UniPixCommands.CreateLayer(m_Session);
                        m_Session.CurrentLayerIndex++;
                    }

                    using (new EditorGUI.DisabledScope(m_Session.CurrentLayerIndex == m_Session.CurrentFrame.Layers.Count - 1))
                        GUILayout.Button("Up", Styles.layerToolbarBtn);

                    using (new EditorGUI.DisabledScope(m_Session.CurrentLayerIndex == 0))
                    {
                        GUILayout.Button("Dw", Styles.layerToolbarBtn);
                        GUILayout.Button("Mer", Styles.layerToolbarBtn);
                    }

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

            var primaryColorRect = new Rect(10, position.height - Styles.kStatusbarHeight - (2*Styles.kColorSwatchSize), Styles.kColorSwatchSize, Styles.kColorSwatchSize);
            var secondaryColorRect = new Rect(primaryColorRect.xMax - 15, primaryColorRect.yMax - 15, Styles.kColorSwatchSize, Styles.kColorSwatchSize);

            // TODO: check if the color is in the current palette or not?

            m_Session.SecondaryColor = EditorGUI.ColorField(secondaryColorRect, new GUIContent(""), m_Session.SecondaryColor, false, false, false);
            m_Session.CurrentColor = EditorGUI.ColorField(primaryColorRect, new GUIContent(""), m_Session.CurrentColor, false, false, false);
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

            if (Event.current != null)
            {
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (!m_CanvasRect.Contains(Event.current.mousePosition))
                            break;

                        if (DragAndDrop.objectReferences.Length == 1 &&
                            DragAndDrop.objectReferences[0])
                        {
                            var objRef = DragAndDrop.objectReferences[0];
                            var path = AssetDatabase.GetAssetPath(objRef);
                            if (!string.IsNullOrEmpty(path) &&
                                (objRef is UniPix.Image || objRef is Texture2D))
                            {
                                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                                if (Event.current.type == EventType.DragPerform)
                                {
                                    DragAndDrop.AcceptDrag();
                                    Event.current.Use();

                                    UniPixCommands.LoadPix(m_Session, path);

                                    EditorGUIUtility.ExitGUI();
                                }
                            }
                        }
                        break;
                }
            }
            


            var xScale = m_Session.Image.Width * m_Session.ZoomLevel;
            var yScale = m_Session.Image.Height * m_Session.ZoomLevel;
            m_Session.ScaledImgRect = new Rect((m_CanvasRect.width / 2 - xScale / 2) + m_imageOffsetX, m_imageOffsetY, xScale, yScale);

            EditorGUI.DrawRect(m_CanvasRect, new Color(0.4f, 0.4f, 0.4f));
            GUILayout.BeginArea(m_CanvasRect);
            {
                EditorGUI.DrawTextureTransparent(m_Session.ScaledImgRect, m_TransparentTex);
                var tex = UniPixUtils.CreateTextureFromImg(m_Session.Image, m_Session.CurrentFrameIndex);
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

                if (m_ShowGrid && m_Session.ZoomLevel > 2)
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
            for (int x = 0; x <= m_Session.Image.Width; x += m_GridSize)
            {
                float posX = m_Session.ScaledImgRect.xMin + m_Session.ZoomLevel * x/* - 0.2f*/;
                EditorGUI.DrawRect(new Rect(posX, m_Session.ScaledImgRect.yMin, 1, m_Session.ScaledImgRect.height), m_GridColor);
            }
            // Then x axis
            for (int y = 0; y <= m_Session.Image.Height; y += m_GridSize)
            {
                float posY = m_Session.ScaledImgRect.yMin + m_Session.ZoomLevel * y/* - 0.2f*/;
                EditorGUI.DrawRect(new Rect(m_Session.ScaledImgRect.xMin, posY, m_Session.ScaledImgRect.width, 1), m_GridColor);
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

        private void DrawColorPalette()
        {
            // Draw Tiles with each different colors in image
            // allow save + load of a palette
            GUILayout.BeginArea(m_ColorPaletteRect);
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("Palette", Styles.layerHeader);

                const int nbItemPerRow = 5;
                var nbRows = (m_Session.palette.Colors.Count / nbItemPerRow) + 1;
                var colorItemIndex = 0;
                for(var rowIndex = 0; rowIndex < nbRows; ++rowIndex)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        for (var itemIndexInRow = 0; itemIndexInRow < nbItemPerRow; ++itemIndexInRow, ++colorItemIndex)
                        {
                            // TODO use a GUIStyle with margin!
                            var colorRect = GUILayoutUtility.GetRect(Styles.kPaletteItemSize + 2*Styles.kMargin, Styles.kPaletteItemSize + 2*Styles.kMargin);

                            if (colorItemIndex < m_Session.palette.Colors.Count)
                            {
                                var contentRect = new Rect(colorRect.x + Styles.kMargin, colorRect.y + Styles.kMargin, colorRect.width - 2 * Styles.kMargin, colorRect.height - 2 * Styles.kMargin);
                                if (m_Session.palette.Colors[colorItemIndex].a == 0f)
                                {
                                    EditorGUI.DrawTextureTransparent(contentRect, m_TransparentTex);
                                }
                                else
                                {
                                    EditorGUI.DrawRect(contentRect, m_Session.palette.Colors[colorItemIndex]);
                                }

                                if (Event.current.isMouse && Event.current.type == EventType.MouseDown && contentRect.Contains(Event.current.mousePosition))
                                {
                                    m_Session.CurrentColor = m_Session.palette.Colors[colorItemIndex];
                                }
                            }
                        }
                    }
                }
            }

            GUILayout.EndArea();
        }

        private void DrawToolbar()
        {
            // settings
            // Save
            // Export
            // New
            // Import
            // Duplicate
            
            GUILayout.BeginArea(m_ToolbarRect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("New", EditorStyles.toolbarButton))
            {
                UniPixCommands.CreatePix(m_Session, 32, 32);
                Repaint();
            }

            if (GUILayout.Button("Load", EditorStyles.toolbarButton))
            {
                UniPixCommands.LoadPix(m_Session);
                Repaint();
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                UniPixCommands.SavePix(m_Session);
            }

            var imgPath = AssetDatabase.GetAssetPath(m_Session.Image);
            if (string.IsNullOrEmpty(imgPath))
            {
                imgPath = "*Untitled*";
            }
            GUILayout.Label(imgPath, EditorStyles.toolbarTextField);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawSettings()
        {
            GUILayout.BeginArea(m_SettingsRect);

            GUILayout.Label("Show", Styles.layerHeader);
            m_ShowGrid = GUILayout.Toggle(m_ShowGrid, "Grid");
            m_GridSize = Mathf.Clamp(EditorGUILayout.IntField("Size", m_GridSize), 1, 5);
            m_GridColor = EditorGUILayout.ColorField("Color", m_GridColor);

            GUILayout.EndArea();
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
