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
        public string ImagePath;
        public string ImageTitle;
        public bool IsImageDirty;

        public int CurrentLayerIndex = 0;
        public int CurrentFrameIndex = 0;

        public Frame CurrentFrame => Image.Frames[CurrentFrameIndex];
        public Layer CurrentLayer => CurrentFrame.Layers[CurrentLayerIndex];

        public Color CurrentColor = new Color(1, 0, 0);
        public Color SecondaryColor = Color.black;

        public Rect ScaledImgRect;
        public Vector2Int CursorImgCoord;
        public Vector2 CursorPos;
        public int CursorPixelIndex => ImgCoordToPixelIndex(CursorImgCoord.x, CursorImgCoord.y);
        public RectInt BrushRect {
            get
            {
                var halfBrush = BrushSize / 2;
                var reminder = BrushSize % 2;
                var cursorCoordX = Mathf.Max(CursorImgCoord.x - halfBrush, 0);
                var cursorCoordY = Mathf.Max(CursorImgCoord.y - halfBrush, 0);
                var cursorSize = BrushSize;
                var brushRect = new RectInt(
                    cursorCoordX,
                    cursorCoordY,
                    cursorSize, cursorSize);
                brushRect.xMax = Mathf.Min(brushRect.xMax, Image.Width);
                brushRect.yMax = Mathf.Min(brushRect.yMax, Image.Height);
                return brushRect;
            }
        }
        public int BrushSize = 5;

        public Palette Palette;

        public int ImgCoordToPixelIndex(int imgCoordX, int imgCoordY)
        {
            return imgCoordX + (Image.Height - imgCoordY - 1) * Image.Height;
        }

        public int PreviewFps = 4;
        public int PreviewFrameIndex = 0;
        public bool IsPreviewPlaying;
        public float PreviewTimer;

        public Vector2 FrameScroll = new Vector2(0, 0);
        public bool IsDebugDraw;
    }

    public class PixEditor : EditorWindow
    {
        Rect m_CanvasRect;
        Rect m_StatusRect;
        Rect m_ViewportRect;
        Rect m_LayerRect;
        Rect m_PaletteRect;
        Rect m_AnimPreviewRect;
        Rect m_ToolbarRect;
        Rect m_ColorPaletteRect;
        Rect m_SettingsRect;
        Rect m_ToolsPaletteRect;
        Rect m_FramePreviewRect;
        Texture2D m_TransparentTex;
        System.Diagnostics.Stopwatch m_Timer = new System.Diagnostics.Stopwatch();

        public static class Prefs
        {
            public static string kPrefix = "unixpix.";
            public static string kCurrentImg = $"{kPrefix}currentImg";
        }

        static class Styles
        {
            public const float scrollbarWidth = 13f;
            public const float kToolPaletteWidth = 100;
            public const float kFramePreviewWidth = 100;
            public const float kLeftPanelWidth = kToolPaletteWidth + kFramePreviewWidth;
            public const float kLayerWidth = 200;
            public const float kToolbarHeight = 25;
            public const float kStatusbarHeight = 35;
            public const float kColorSwatchSize = 40;
            public const float kPaletteItemSize = 25;
            public const float kLayerHeight = 25;
            public const float kLayerRectHeight = 6 * kLayerHeight;
            public const float kSettingsHeight = 75;
            public const float kMargin = 2;
            public const float kToolSize = 45;
            public const float kFramePreviewBtn = 25;
            public const int kNbToolsPerRow = (int)kToolPaletteWidth / (int)kToolSize;
            public const int kFramePreviewSize = (int)(kFramePreviewWidth - 2 * kMargin - scrollbarWidth);

            public static GUIStyle brushSizeStyle = new GUIStyle(EditorStyles.numberField)
            {
                fixedWidth = 50
            };
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

            public static GUIStyle statusLabel = new GUIStyle(EditorStyles.label)
            {
                richText = true
            };

            static Styles()
            {
                currentLayerName.normal.textColor = Color.yellow;
            }
        }

        float m_imageOffsetX;
        float m_imageOffsetY;
        bool m_IsPanning;
        Vector2 m_PanStart;

        SessionData m_Session;

        bool m_ShowGrid = true;
        int m_GridSize = 1;
        Color m_GridColor = Color.black;

        PixTool m_CurrentTool;
        PixTool[] m_Tools;

        private void OnEnable()
        {
            titleContent = new GUIContent("UniPix");
            m_Tools = new PixTool[] {
                new BrushTool(),
                new EraseTool()
            };
            m_CurrentTool = m_Tools[0];

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
            m_Timer.Restart();
            ProcessEvents();
            ComputeLayout();
            DrawToolbar();
            if (m_Session.IsDebugDraw)
            {
                DrawDebugArea();
            }
            // else
            {
                DrawToolPalette();
                DrawAnimationPreview();
                DrawFrames();
                DrawColorSwitcher();
                DrawPixEditor();
                DrawLayers();
                DrawColorPalette();
                DrawSettings();
                DrawStatus();
            }
        }

        private void ComputeLayout()
        {
            m_ToolbarRect = new Rect(Styles.kMargin, Styles.kMargin, position.width - 2*Styles.kMargin, Styles.kToolbarHeight);
            m_AnimPreviewRect = new Rect(Styles.kMargin, m_ToolbarRect.yMax + Styles.kMargin, Styles.kFramePreviewWidth, Styles.kFramePreviewWidth);
            m_ToolsPaletteRect = new Rect(Styles.kMargin, m_AnimPreviewRect.yMax + 10 + Styles.kMargin, Styles.kToolPaletteWidth, Styles.kLayerRectHeight);
            m_FramePreviewRect = new Rect(
                m_ToolsPaletteRect.xMax + Styles.kMargin, 
                m_ToolbarRect.yMax + Styles.kMargin, 
                Styles.kFramePreviewWidth, 
                position.height - Styles.kToolbarHeight - Styles.kStatusbarHeight - 2 * Styles.kMargin);
            m_CanvasRect = new Rect(m_FramePreviewRect.xMax + Styles.kMargin, 
                m_ToolbarRect.yMax + Styles.kMargin,
                position.width - Styles.kLeftPanelWidth - Styles.kLayerWidth, 
                position.height - Styles.kToolbarHeight - Styles.kStatusbarHeight - 2 * Styles.kMargin);
            m_StatusRect = new Rect(m_CanvasRect.x, m_CanvasRect.yMax + Styles.kMargin, m_CanvasRect.width, Styles.kStatusbarHeight);

            const float kRightPanelWidth = Styles.kLayerWidth - 2 * Styles.kMargin;
            m_LayerRect = new Rect(m_CanvasRect.xMax + Styles.kMargin, m_CanvasRect.y, kRightPanelWidth - Styles.kMargin, Styles.kLayerRectHeight);

            m_ColorPaletteRect = new Rect(m_CanvasRect.xMax + Styles.kMargin, m_LayerRect.yMax + Styles.kMargin, kRightPanelWidth - Styles.kMargin, Styles.kLayerRectHeight);

            m_SettingsRect = new Rect(m_CanvasRect.xMax + Styles.kMargin, m_ColorPaletteRect.yMax + Styles.kMargin, kRightPanelWidth - Styles.kMargin, Styles.kSettingsHeight);
        }

        private void DrawDebugArea()
        {
            // DrawDebugRect(m_ToolbarRect, "toolbar", Color.green);
            DrawDebugRect(m_AnimPreviewRect, "animPreview", new Color(1, 0, 0.5f));
            DrawDebugRect(m_CanvasRect, "canvas", Color.white);
            DrawDebugRect(m_ToolsPaletteRect, "tools", Color.magenta);
            DrawDebugRect(m_FramePreviewRect, "frames", Color.yellow);
            DrawDebugRect(m_StatusRect, "status", Color.red);
            DrawDebugRect(m_LayerRect, "layer", Color.cyan);
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
            else if (e.type == EventType.ScrollWheel && m_CanvasRect.Contains(e.mousePosition))
            {
                m_Session.ZoomLevel -= e.delta.y > 0 ? 1 : -1;
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
            GUILayout.BeginArea(m_LayerRect);
            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label("Layers", Styles.layerHeader);
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Cr", Styles.layerToolbarBtn))
                    {
                        UniPixCommands.CreateLayer(m_Session);
                        Repaint();
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button("Cl", Styles.layerToolbarBtn))
                    {
                        UniPixCommands.CloneLayer(m_Session);
                        Repaint();
                        GUIUtility.ExitGUI();
                    }

                    using (new EditorGUI.DisabledScope(m_Session.CurrentLayerIndex == m_Session.CurrentFrame.Layers.Count - 1))
                    {
                        if (GUILayout.Button("Up", Styles.layerToolbarBtn))
                        {
                            UniPixCommands.SwapLayers(m_Session, m_Session.CurrentLayerIndex, m_Session.CurrentLayerIndex + 1);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }
                    }

                    using (new EditorGUI.DisabledScope(m_Session.CurrentLayerIndex == 0))
                    {
                        if (GUILayout.Button("Dw", Styles.layerToolbarBtn))
                        {
                            UniPixCommands.SwapLayers(m_Session, m_Session.CurrentLayerIndex, m_Session.CurrentLayerIndex - 1);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }

                        if (GUILayout.Button("Mer", Styles.layerToolbarBtn))
                        {
                            UniPixCommands.MergeLayers(m_Session, m_Session.CurrentLayerIndex, m_Session.CurrentLayerIndex - 1);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }
                    }

                    if (GUILayout.Button("Del", Styles.layerToolbarBtn))
                    {
                        UniPixCommands.DeleteLayer(m_Session);
                        Repaint();
                        GUIUtility.ExitGUI();
                    }
                }

                for (var i = m_Session.CurrentFrame.Layers.Count - 1; i >= 0; i--)
                {
                    var layer = m_Session.CurrentFrame.Layers[i];
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button(layer.Name, i == m_Session.CurrentLayerIndex ? Styles.currentLayerName : Styles.layerName))
                    {
                        UniPixCommands.SetCurrentLayer(m_Session, i);
                        Repaint();
                    }

                    EditorGUI.BeginChangeCheck();
                    var opacity = Mathf.Clamp(EditorGUILayout.FloatField(layer.Opacity, Styles.layerOpacity), 0f, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UniPixCommands.SetLayerOpacity(m_Session, i, opacity);
                        Repaint();
                    }

                    EditorGUI.BeginChangeCheck();
                    var isLayerVisible = EditorGUILayout.Toggle(layer.Visible, Styles.layerVisible);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UniPixCommands.SetLayerVisibility(m_Session, i, isLayerVisible);
                        Repaint();
                    }

                    // TODO Handle layer lock

                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndArea();
        }

        private void DrawToolPalette()
        {
            GUILayout.BeginArea(m_ToolsPaletteRect);
            GUILayout.Label("Tools", Styles.layerHeader);

            m_Session.BrushSize = Mathf.Clamp(EditorGUILayout.IntField("Brush", m_Session.BrushSize, Styles.brushSizeStyle), 1, 5);
            var toolsRect = GUILayoutUtility.GetRect(m_ToolsPaletteRect.width, 100);
            var nbRows = (m_Tools.Length / Styles.kNbToolsPerRow) + 1;
            var toolIndex = 0;
            for(var rowIndex = 0; rowIndex < nbRows; ++rowIndex)
            {
                var toolY = toolsRect.y + rowIndex * Styles.kToolSize + Styles.kMargin;
                for (var toolColumn = 0; toolColumn < Styles.kNbToolsPerRow; ++toolColumn, ++toolIndex)
                {
                    if (toolIndex >= m_Tools.Length)
                        break;
                    var tool = m_Tools[toolIndex];
                    var toolRect = new Rect(Styles.kMargin + toolColumn * (Styles.kMargin + Styles.kToolSize), toolY, Styles.kToolSize, Styles.kToolSize);
                    if (GUI.Toggle(toolRect, tool == m_CurrentTool, tool.Name.Substring(0, 2), GUI.skin.button))
                    {
                        m_CurrentTool = tool;
                    }
                }
            }

            // TODO : indicate which colors corresponds to which MouseButton
            // TODO: PAlette editing: remove from palette. Add new color??

            GUILayout.EndArea();
        }

        private void DrawFrames()
        {
            // TODO: reorder frames
            var framesRect = new Rect(0, 0, 
                Styles.kFramePreviewWidth - Styles.scrollbarWidth,
                Styles.kMargin + (Styles.kFramePreviewSize + Styles.kMargin) * m_Session.Image.Frames.Count
                );
            
            var addFrameRect = new Rect(Styles.kMargin,
                framesRect.yMax + Styles.kMargin,
                Styles.kFramePreviewSize - Styles.kMargin,
                Styles.kFramePreviewSize / 2 - Styles.kMargin);

            var viewRect = new Rect(0, 0,
                Styles.kFramePreviewWidth - Styles.scrollbarWidth,
                framesRect.height + addFrameRect.height + Styles.kMargin);

            m_Session.FrameScroll = GUI.BeginScrollView(m_FramePreviewRect, m_Session.FrameScroll, viewRect);
            var frameIndex = 0;
            var eventUsed = false;
            foreach (var frame in m_Session.Image.Frames)
            {
                var frameRect = new Rect(Styles.kMargin,
                    Styles.kMargin + (frameIndex * Styles.kFramePreviewSize),
                    Styles.kFramePreviewSize, 
                    Styles.kFramePreviewSize);
                var tex = frame.Texture;
                GUI.DrawTexture(frameRect, tex);

                if (frameRect.Contains(Event.current.mousePosition))
                {
                    if (GUI.Button(new Rect(frameRect.x + Styles.kMargin, frameRect.y + Styles.kMargin, Styles.kFramePreviewBtn, Styles.kFramePreviewBtn), "C", EditorStyles.miniButton))
                    {
                        UniPixCommands.CloneFrame(m_Session, frameIndex);
                        Repaint();
                        GUIUtility.ExitGUI();
                        eventUsed = true;
                    }

                    if (GUI.Button(new Rect(frameRect.xMax - Styles.kFramePreviewBtn - Styles.kMargin, frameRect.y + Styles.kMargin, Styles.kFramePreviewBtn, Styles.kFramePreviewBtn), "D", EditorStyles.miniButton))
                    {
                        UniPixCommands.DeleteFrame(m_Session, frameIndex);
                        Repaint();
                        GUIUtility.ExitGUI();
                        eventUsed = true;
                    }
                }

                if (!eventUsed && frameRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
                {
                    UniPixCommands.SetCurrentFrame(m_Session, frameIndex);
                    Repaint();
                    GUIUtility.ExitGUI();
                }

                ++frameIndex;
            }

            if (GUI.Button(addFrameRect, "New Frame"))
            {
                UniPixCommands.NewFrame(m_Session);
                Repaint();
                GUIUtility.ExitGUI();
            }
            GUI.EndScrollView();
        }

        private void DrawColorSwitcher()
        {
            var primaryColorRect = new Rect(10, position.height - Styles.kStatusbarHeight - (2*Styles.kColorSwatchSize), Styles.kColorSwatchSize, Styles.kColorSwatchSize);
            var secondaryColorRect = new Rect(primaryColorRect.xMax - 15, primaryColorRect.yMax - 15, Styles.kColorSwatchSize, Styles.kColorSwatchSize);

            m_Session.SecondaryColor = EditorGUI.ColorField(secondaryColorRect, new GUIContent(""), m_Session.SecondaryColor, false, false, false);
            m_Session.CurrentColor = EditorGUI.ColorField(primaryColorRect, new GUIContent(""), m_Session.CurrentColor, false, false, false);
        }

        private void DrawPixEditor()
        {
            if (Event.current != null)
            {
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (!m_CanvasRect.Contains(Event.current.mousePosition))
                            break;

                        var paths = 
                            DragAndDrop.objectReferences
                                .Where(obj => obj is Image || obj is Texture2D)
                                .Select(obj => AssetDatabase.GetAssetPath(obj))
                                .Where(path => !string.IsNullOrEmpty(path)).ToArray();
                        if (paths.Length > 0)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            if (Event.current.type == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();
                                Event.current.Use();
                                UniPixCommands.LoadPix(m_Session, paths);
                                GUIUtility.ExitGUI();
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
                var tex = m_Session.CurrentFrame.Texture;
                GUI.DrawTexture(m_Session.ScaledImgRect, tex);

                if (m_Session.ScaledImgRect.Contains(Event.current.mousePosition))
                {
                    m_Session.CursorPos = Event.current.mousePosition - m_Session.ScaledImgRect.position;
                    m_Session.CursorImgCoord = new Vector2Int((int)(m_Session.CursorPos.x / m_Session.ZoomLevel), (int)(m_Session.CursorPos.y / m_Session.ZoomLevel));
                    if (m_CurrentTool.OnEvent(Event.current, m_Session))
                    {
                        Repaint();
                    }
                    else if (Event.current.isMouse &&
                            Event.current.button == 2)
                    {
                        Pan();
                    }
                }

                if (m_ShowGrid && m_Session.ZoomLevel > 2)
                {
                    DrawGrid();
                }
            }
            GUILayout.EndArea();
        }

        private void Pan()
        {
            if (Event.current.type == EventType.MouseDown)
            {
                m_PanStart = Event.current.mousePosition;
                m_IsPanning = true;
            }
            else if (m_IsPanning && Event.current.type == EventType.MouseUp)
            {
                m_IsPanning = false;
            }
            else if (m_IsPanning && Event.current.type == EventType.MouseDrag)
            {
                var panningDistance = Event.current.mousePosition - m_PanStart;
                m_imageOffsetX += panningDistance.x;
                m_imageOffsetY += panningDistance.y;
                m_PanStart = Event.current.mousePosition;
            }
        }

        private void DrawGrid()
        {
            // TODO: is it possible to have a transparent texture that maps pixel perfect to the grid with the grid.

            for (int x = 0; x <= m_Session.Image.Width; x += m_GridSize)
            {
                float posX = m_Session.ScaledImgRect.xMin + m_Session.ZoomLevel * x;
                EditorGUI.DrawRect(new Rect(posX, m_Session.ScaledImgRect.yMin, 1, m_Session.ScaledImgRect.height), m_GridColor);
            }
            // Then x axis
            for (int y = 0; y <= m_Session.Image.Height; y += m_GridSize)
            {
                float posY = m_Session.ScaledImgRect.yMin + m_Session.ZoomLevel * y;
                EditorGUI.DrawRect(new Rect(m_Session.ScaledImgRect.xMin, posY, m_Session.ScaledImgRect.width, 1), m_GridColor);
            }
        }

        private void DrawAnimationPreview()
        {
            if (m_Session.Image.Frames.Count == 0)
                return;

            var frameRect = new Rect(
                m_AnimPreviewRect.x + Styles.kMargin,
                m_AnimPreviewRect.y + Styles.kMargin,
                Styles.kFramePreviewSize,
                Styles.kFramePreviewSize);

            var tex = m_Session.Image.Frames[m_Session.PreviewFrameIndex].Texture;
            if (m_Session.IsDebugDraw)
            {
                DrawDebugRect(frameRect, "frame", new Color(0, 1, 0));
            }
            else
            {
                GUI.DrawTexture(frameRect, tex);
                if (Event.current.type == EventType.MouseDown && frameRect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    m_Session.IsPreviewPlaying = !m_Session.IsPreviewPlaying;
                    m_Session.PreviewTimer = 0;
                }
            }

            var labelRect = new Rect(frameRect.x, frameRect.yMax, 35, 15);
            GUI.Label(labelRect, $"{m_Session.PreviewFps}fps");
            m_Session.PreviewFps = (int)GUI.HorizontalSlider(new Rect(labelRect.xMax, labelRect.y,
                    frameRect.width - labelRect.width - Styles.kMargin, labelRect.height), 
                m_Session.PreviewFps, 0, 24);
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
                var nbRows = (m_Session.Palette.Colors.Count / nbItemPerRow) + 1;
                var colorItemIndex = 0;
                for(var rowIndex = 0; rowIndex < nbRows; ++rowIndex)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        for (var itemIndexInRow = 0; itemIndexInRow < nbItemPerRow; ++itemIndexInRow, ++colorItemIndex)
                        {
                            // TODO use a GUIStyle with margin!
                            var colorRect = GUILayoutUtility.GetRect(Styles.kPaletteItemSize + 2*Styles.kMargin, Styles.kPaletteItemSize + 2*Styles.kMargin);

                            if (colorItemIndex < m_Session.Palette.Colors.Count)
                            {
                                var contentRect = new Rect(colorRect.x + Styles.kMargin, colorRect.y + Styles.kMargin, colorRect.width - 2 * Styles.kMargin, colorRect.height - 2 * Styles.kMargin);
                                if (m_Session.Palette.Colors[colorItemIndex].a == 0f)
                                {
                                    EditorGUI.DrawTextureTransparent(contentRect, m_TransparentTex);
                                }
                                else
                                {
                                    EditorGUI.DrawRect(contentRect, m_Session.Palette.Colors[colorItemIndex]);
                                }

                                if (Event.current.isMouse && Event.current.type == EventType.MouseDown && contentRect.Contains(Event.current.mousePosition))
                                {
                                    m_Session.CurrentColor = m_Session.Palette.Colors[colorItemIndex];
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

            GUILayout.Label(m_Session.ImageTitle, EditorStyles.toolbarTextField);

            GUILayout.FlexibleSpace();

            m_Session.IsDebugDraw = GUILayout.Toggle(m_Session.IsDebugDraw, "Debug");

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
            var status = $"<b>Zoom:</b>{m_Session.ZoomLevel}  ";
            status += $"<b>Size:</b>[{m_Session.Image.Width}x{m_Session.Image.Height}]  ";
            if (m_Session.CurrentFrameIndex != -1)
                status += $"<b>Frame:</b> {m_Session.CurrentFrameIndex + 1} / {m_Session.Image.Frames.Count}  ";
            if (m_Session.CurrentLayerIndex != -1)
                status += $"<b>Layer:</b> {m_Session.CurrentLayer.Name}  ";
            status += $"<b>Mouse:</b>[{m_Session.CursorImgCoord.x}, {m_Session.CursorImgCoord.y}]  ";
            m_Timer.Stop();
            status += $"<b>Fps:</b>{m_Timer.ElapsedMilliseconds} ";
            GUI.Label(m_StatusRect, status, Styles.statusLabel);
        }

        private void Update()
        {
            if (m_Session.IsPreviewPlaying)
            {
                if (m_Session.PreviewFps > 0 && Time.realtimeSinceStartup - m_Session.PreviewTimer >= (1f / m_Session.PreviewFps))
                {
                    m_Session.PreviewFrameIndex = (m_Session.PreviewFrameIndex + 1) % m_Session.Image.Frames.Count;
                    m_Session.PreviewTimer = Time.realtimeSinceStartup;
                    Repaint();
                }
            }
        }

        [MenuItem("Window/UniPix")]
        static void ShowUniPix()
        {
            GetWindow<PixEditor>();
        }
    }

}
