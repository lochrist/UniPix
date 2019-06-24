using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
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
        public Vector2 RightPanelScroll = new Vector2(0, 0);
        public bool IsDebugDraw;

        public bool ShowGrid = true;
        public int GridSize = 1;
        public Color GridColor = Color.black;
    }

    public class PixEditor : EditorWindow
    {
        public static string packageName = "com.unity.unipix";
        public static string packageFolderName = $"Packages/{packageName}";

        Rect m_CanvasRect;
        Rect m_StatusRect;
        Rect m_ViewportRect;
        Rect m_LayerRect;
        Rect m_PaletteRect;
        Rect m_AnimPreviewRect;
        Rect m_ToolbarRect;
        Rect m_ColorPaletteRect;
        Rect m_ToolsPaletteRect;
        Rect m_FramePreviewRect;
        Rect m_DebugAreaRect;
        Rect m_RightPanelRect;
        Texture2D m_TransparentTex;
        System.Diagnostics.Stopwatch m_Timer = new System.Diagnostics.Stopwatch();

        public static class Prefs
        {
            public static string kPrefix = "unixpix.";
            public static string kCurrentImg = $"{kPrefix}currentImg";
        }

        internal static class Styles
        {
            public const float scrollbarWidth = 13f;
            public const float kToolPaletteWidth = 100;
            public const float kFramePreviewWidth = 100;
            public const float kLeftPanelWidth = kToolPaletteWidth + kFramePreviewWidth;
            public const float kRightPanelWidth = 200;
            public const float kToolbarHeight = 25;
            public const float kStatusbarHeight = 35;
            public const float kColorSwatchSize = 40;
            public const float kPaletteItemSize = 25;
            public const float kLayerHeight = 25;
            public const float kLayerRectHeight = 6 * kLayerHeight;
            public const float kMargin = 2;
            public const float kToolSize = 45;
            public const float kFramePreviewBtn = 25;
            public const int kNbToolsPerRow = (int)kToolPaletteWidth / (int)kToolSize;
            public const int kFramePreviewSize = (int)(kFramePreviewWidth - 2 * kMargin - scrollbarWidth);

            public static GUIContent newLayer = new GUIContent(Icons.plus, "Create new layer");
            public static GUIContent cloneLayer = new GUIContent(Icons.duplicateLayer, "Duplicate layer");
            public static GUIContent moveLayerUp = new GUIContent(Icons.arrowUp, "Move layer up");
            public static GUIContent moveLayerDown = new GUIContent(Icons.arrowDown, "Move layer down");
            public static GUIContent mergeLayer = new GUIContent(Icons.mergeLayer, "Merge layer");
            public static GUIContent deleteLayer = new GUIContent(Icons.x, "Delete layer");

            public static GUIContent cloneFrame = new GUIContent(Icons.duplicateLayer, "Clone frame");
            public static GUIContent deleteFrame = new GUIContent(Icons.x, "Delete frame");

            public static GUIContent brushTool = new GUIContent(Icons.pencil, "Brush");
            public static GUIContent eraserTool = new GUIContent(Icons.eraser, "Eraser");

            public static GUIContent gridSettingsContent = new GUIContent("Grid...");

            // public static GUIContent layerVisible = new GUIContent(Icons.eye, "Layer Visibility");
            // public static GUIContent layerNotVisible = new GUIContent(Icons.eyeClosed, "Layer Visibility");

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
                // name = "layerToolbarBtn",
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = 25,
                fixedHeight = 25
            };

            public static GUIStyle frameBtn = new GUIStyle(EditorStyles.miniButton)
            {
                // name = "fameBtn",
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(2, 2, 2, 2)
            };

            public static GUIStyle statusLabel = new GUIStyle(EditorStyles.label)
            {
                richText = true
            };

            public static GUIStyle pixBox = new GUIStyle()
            {
                name = "pixbox",
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(2, 2, 2, 2)
            };

            public static GUIStyle selectedPixBox = new GUIStyle()
            {
                name = "selected-pixbox",
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(2, 2, 2, 2)
            };

            static Styles()
            {
                currentLayerName.normal.textColor = Color.yellow;
            }
        }

        float m_ImageOffsetX;
        float m_ImageOffsetY;
        bool m_IsPanning;
        Vector2 m_PanStart;

        public SessionData Session;

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

            Session = new SessionData();

            UniPixCommands.LoadPix(Session, EditorPrefs.GetString(Prefs.kCurrentImg, null));

            Session.ZoomLevel = 10f;

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
            if (Session.IsDebugDraw)
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

                GUILayout.BeginArea(m_RightPanelRect);
                Session.RightPanelScroll = GUILayout.BeginScrollView(Session.RightPanelScroll);
                DrawLayers();
                DrawColorPalette();
                // DrawDebugStuff();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                DrawStatus();
            }
        }

        private void ComputeLayout()
        {
            var verticalMaxHeight = position.height - Styles.kToolbarHeight - Styles.kStatusbarHeight;

            m_ToolbarRect = new Rect(0, 0, position.width, Styles.kToolbarHeight);
            { // Column 1
                m_AnimPreviewRect = new Rect(0, m_ToolbarRect.yMax, Styles.kFramePreviewWidth, Styles.kFramePreviewWidth);
                const int kToolsSpacing = 10;
                m_ToolsPaletteRect = new Rect(0, m_AnimPreviewRect.yMax + kToolsSpacing, Styles.kToolPaletteWidth, Styles.kLayerRectHeight);
            }

            { // Column 2
                m_FramePreviewRect = new Rect(
                    m_ToolsPaletteRect.xMax,
                    m_ToolbarRect.yMax,
                    Styles.kFramePreviewWidth,
                    verticalMaxHeight);
            }

            { // Column 3
                m_CanvasRect = new Rect(m_FramePreviewRect.xMax + Styles.kMargin,
                    m_ToolbarRect.yMax + Styles.kMargin,
                    position.width - Styles.kLeftPanelWidth - Styles.kRightPanelWidth - 2*Styles.kMargin,
                    verticalMaxHeight);

                m_StatusRect = new Rect(m_CanvasRect.x, m_CanvasRect.yMax, m_CanvasRect.width, Styles.kStatusbarHeight);
            }

            { // Column 4
                m_RightPanelRect = new Rect(m_CanvasRect.xMax + Styles.kMargin, m_CanvasRect.y, Styles.kRightPanelWidth, verticalMaxHeight);

                m_LayerRect = new Rect(m_RightPanelRect.x, m_RightPanelRect.y, m_RightPanelRect.width, Styles.kLayerRectHeight);

                m_ColorPaletteRect = new Rect(m_RightPanelRect.x, m_LayerRect.yMax, m_RightPanelRect.width, Styles.kLayerRectHeight);
            }
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

            DrawDebugRect(m_DebugAreaRect, "debug", Color.green);
        }

        private static void DrawDebugRect(Rect rect, string title, Color c)
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
                    m_ImageOffsetY -= 20f;
                }
                if (e.keyCode == KeyCode.S)
                {
                    m_ImageOffsetY += 20f;
                }
                if (e.keyCode == KeyCode.A)
                {
                    m_ImageOffsetX -= 20f;
                }
                if (e.keyCode == KeyCode.D)
                {
                    m_ImageOffsetX += 20f;
                }
                if (e.keyCode == KeyCode.UpArrow)
                {
                    Session.ZoomLevel += 2f;
                }
                if (e.keyCode == KeyCode.DownArrow)
                {
                    Session.ZoomLevel -= 2f;
                    Session.ZoomLevel = Mathf.Max(1, Session.ZoomLevel);
                }
                Repaint();
            }
            else if (e.type == EventType.ScrollWheel && m_CanvasRect.Contains(e.mousePosition))
            {
                Session.ZoomLevel -= e.delta.y > 0 ? 1 : -1;
                Session.ZoomLevel = Mathf.Max(1, Session.ZoomLevel);
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
            using (new GUILayout.VerticalScope(Styles.pixBox))
            {
                GUILayout.Label("Layers", Styles.layerHeader);
                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(Styles.newLayer, Styles.layerToolbarBtn))
                    {
                        UniPixCommands.CreateLayer(Session);
                        Repaint();
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button(Styles.cloneLayer, Styles.layerToolbarBtn))
                    {
                        UniPixCommands.CloneLayer(Session);
                        Repaint();
                        GUIUtility.ExitGUI();
                    }

                    using (new EditorGUI.DisabledScope(Session.CurrentLayerIndex == Session.CurrentFrame.Layers.Count - 1))
                    {
                        if (GUILayout.Button(Styles.moveLayerUp, Styles.layerToolbarBtn))
                        {
                            UniPixCommands.SwapLayers(Session, Session.CurrentLayerIndex, Session.CurrentLayerIndex + 1);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }
                    }

                    using (new EditorGUI.DisabledScope(Session.CurrentLayerIndex == 0))
                    {
                        if (GUILayout.Button(Styles.moveLayerDown, Styles.layerToolbarBtn))
                        {
                            UniPixCommands.SwapLayers(Session, Session.CurrentLayerIndex, Session.CurrentLayerIndex - 1);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }

                        if (GUILayout.Button(Styles.mergeLayer, Styles.layerToolbarBtn))
                        {
                            UniPixCommands.MergeLayers(Session, Session.CurrentLayerIndex, Session.CurrentLayerIndex - 1);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }
                    }

                    if (GUILayout.Button(Styles.deleteLayer, Styles.layerToolbarBtn))
                    {
                        UniPixCommands.DeleteLayer(Session);
                        Repaint();
                        GUIUtility.ExitGUI();
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label($"Opacity {(int)(Session.CurrentLayer.Opacity * 100)}%", GUILayout.ExpandWidth(false));
                    EditorGUI.BeginChangeCheck();
                    var opacity = GUILayout.HorizontalSlider(Session.CurrentLayer.Opacity, 0, 1, GUILayout.ExpandWidth(true));
                    if (EditorGUI.EndChangeCheck())
                    {
                        UniPixCommands.SetLayerOpacity(Session, Session.CurrentLayerIndex, opacity);
                        Repaint();
                    }
                }

                for (var i = Session.CurrentFrame.Layers.Count - 1; i >= 0; i--)
                {
                    var layer = Session.CurrentFrame.Layers[i];
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button(layer.Name, i == Session.CurrentLayerIndex ? Styles.currentLayerName : Styles.layerName, GUILayout.ExpandWidth(true)))
                    {
                        UniPixCommands.SetCurrentLayer(Session, i);
                        Repaint();
                    }

                    EditorGUI.BeginChangeCheck();
                    var isLayerVisible = GUILayout.Toggle(layer.Visible, "", Styles.layerVisible);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UniPixCommands.SetLayerVisibility(Session, i, isLayerVisible);
                        Repaint();
                    }

                    // TODO Handle layer lock

                    GUILayout.EndHorizontal();
                }
            }
        }

        private void DrawToolPalette()
        {
            GUILayout.BeginArea(m_ToolsPaletteRect);

            GUILayout.Label("Brush Size", Styles.layerHeader);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{Session.BrushSize}", GUILayout.ExpandWidth(false));
            Session.BrushSize = (int)GUILayout.HorizontalSlider(Session.BrushSize, 1, 6, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.Label("Tools", Styles.layerHeader);

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
                    if (GUI.Toggle(toolRect, tool == m_CurrentTool, tool.Content, GUI.skin.button))
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
            GUILayout.BeginArea(m_FramePreviewRect);
            Session.FrameScroll = GUILayout.BeginScrollView(Session.FrameScroll);
            var frameIndex = 0;
            var eventUsed = false;
            foreach (var frame in Session.Image.Frames)
            {
                var tex = frame.Texture;
                var frameRect = GUILayoutUtility.GetRect(Styles.kFramePreviewSize, Styles.kFramePreviewWidth, Styles.pixBox, GUILayout.Height(Styles.kFramePreviewSize));
                GUI.Box(frameRect, "", frameIndex == Session.CurrentFrameIndex ? Styles.selectedPixBox : Styles.pixBox);
                GUI.DrawTexture(frameRect, tex, ScaleMode.ScaleToFit);

                if (frameRect.Contains(Event.current.mousePosition))
                {
                    if (GUI.Button(new Rect(frameRect.x + Styles.kMargin, frameRect.y + Styles.kMargin, Styles.kFramePreviewBtn, 
                        Styles.kFramePreviewBtn), Styles.cloneFrame, Styles.frameBtn))
                    {
                        UniPixCommands.CloneFrame(Session, frameIndex);
                        Repaint();
                        GUIUtility.ExitGUI();
                        eventUsed = true;
                    }

                    if (GUI.Button(new Rect(frameRect.xMax - Styles.kFramePreviewBtn - Styles.kMargin, frameRect.y + Styles.kMargin, 
                        Styles.kFramePreviewBtn, Styles.kFramePreviewBtn), Styles.deleteFrame, Styles.frameBtn))
                    {
                        UniPixCommands.DeleteFrame(Session, frameIndex);
                        Repaint();
                        GUIUtility.ExitGUI();
                        eventUsed = true;
                    }
                }

                if (!eventUsed && frameRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
                {
                    UniPixCommands.SetCurrentFrame(Session, frameIndex);
                    Repaint();
                    GUIUtility.ExitGUI();
                }

                ++frameIndex;
            }

            if (GUILayout.Button("New Frame"))
            {
                UniPixCommands.NewFrame(Session);
                Repaint();
                GUIUtility.ExitGUI();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawColorSwitcher()
        {
            var primaryColorRect = new Rect(10, position.height - Styles.kStatusbarHeight - (2*Styles.kColorSwatchSize), Styles.kColorSwatchSize, Styles.kColorSwatchSize);
            var secondaryColorRect = new Rect(primaryColorRect.xMax - 15, primaryColorRect.yMax - 15, Styles.kColorSwatchSize, Styles.kColorSwatchSize);

            Session.SecondaryColor = EditorGUI.ColorField(secondaryColorRect, new GUIContent(""), Session.SecondaryColor, false, false, false);
            GUI.Box(secondaryColorRect, "", Styles.pixBox);

            Session.CurrentColor = EditorGUI.ColorField(primaryColorRect, new GUIContent(""), Session.CurrentColor, false, false, false);
            GUI.Box(primaryColorRect, "", Styles.pixBox);
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
                                UniPixCommands.LoadPix(Session, paths);
                                GUIUtility.ExitGUI();
                            }
                        }
                        break;
                }
            }

            var xScale = Session.Image.Width * Session.ZoomLevel;
            var yScale = Session.Image.Height * Session.ZoomLevel;
            Session.ScaledImgRect = new Rect((m_CanvasRect.width / 2 - xScale / 2) + m_ImageOffsetX, m_ImageOffsetY, xScale, yScale);

            EditorGUI.DrawRect(m_CanvasRect, new Color(0.4f, 0.4f, 0.4f));
            GUILayout.BeginArea(m_CanvasRect);
            {
                EditorGUI.DrawTextureTransparent(Session.ScaledImgRect, m_TransparentTex);
                var tex = Session.CurrentFrame.Texture;
                GUI.DrawTexture(Session.ScaledImgRect, tex);

                if (Session.ScaledImgRect.Contains(Event.current.mousePosition))
                {
                    Session.CursorPos = Event.current.mousePosition - Session.ScaledImgRect.position;
                    Session.CursorImgCoord = new Vector2Int((int)(Session.CursorPos.x / Session.ZoomLevel), (int)(Session.CursorPos.y / Session.ZoomLevel));
                    if (m_CurrentTool.OnEvent(Event.current, Session))
                    {
                        Repaint();
                    }
                    else if (Event.current.isMouse &&
                            Event.current.button == 2)
                    {
                        Pan();
                    }
                }

                if (Session.ShowGrid && Session.ZoomLevel > 2)
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
                m_ImageOffsetX += panningDistance.x;
                m_ImageOffsetY += panningDistance.y;
                m_PanStart = Event.current.mousePosition;
            }
        }

        private void DrawGrid()
        {
            // TODO: is it possible to have a transparent texture that maps pixel perfect to the grid with the grid.

            for (int x = 0; x <= Session.Image.Width; x += Session.GridSize)
            {
                float posX = Session.ScaledImgRect.xMin + Session.ZoomLevel * x;
                EditorGUI.DrawRect(new Rect(posX, Session.ScaledImgRect.yMin, 1, Session.ScaledImgRect.height), Session.GridColor);
            }
            // Then x axis
            for (int y = 0; y <= Session.Image.Height; y += Session.GridSize)
            {
                float posY = Session.ScaledImgRect.yMin + Session.ZoomLevel * y;
                EditorGUI.DrawRect(new Rect(Session.ScaledImgRect.xMin, posY, Session.ScaledImgRect.width, 1), Session.GridColor);
            }
        }

        private void DrawAnimationPreview()
        {
            if (Session.Image.Frames.Count == 0)
                return;

            var frameRect = new Rect(
                m_AnimPreviewRect.x + Styles.kMargin,
                m_AnimPreviewRect.y + Styles.kMargin,
                Styles.kFramePreviewSize,
                Styles.kFramePreviewSize);

            var tex = Session.Image.Frames[Session.PreviewFrameIndex].Texture;
            if (Session.IsDebugDraw)
            {
                DrawDebugRect(frameRect, "frame", new Color(0, 1, 0));
            }
            else
            {
                GUI.Box(frameRect, "", Styles.pixBox);
                GUI.DrawTexture(frameRect, tex, ScaleMode.ScaleToFit);
                if (Event.current.type == EventType.MouseDown && frameRect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    Session.IsPreviewPlaying = !Session.IsPreviewPlaying;
                    Session.PreviewTimer = 0;
                }
            }

            var labelRect = new Rect(frameRect.x, frameRect.yMax, 35, 15);
            GUI.Label(labelRect, $"{Session.PreviewFps}fps");
            Session.PreviewFps = (int)GUI.HorizontalSlider(new Rect(labelRect.xMax, labelRect.y,
                    frameRect.width - labelRect.width - Styles.kMargin, labelRect.height), Session.PreviewFps, 0, 24);
        }

        private void DrawColorPalette()
        {
            // Draw Tiles with each different colors in image
            // allow save + load of a palette
            using (new GUILayout.VerticalScope(Styles.pixBox))
            {
                GUILayout.Label("Palette", Styles.layerHeader);

                const int nbItemPerRow = 5;
                var nbRows = (Session.Palette.Colors.Count / nbItemPerRow) + 1;
                var colorItemIndex = 0;
                for(var rowIndex = 0; rowIndex < nbRows; ++rowIndex)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        for (var itemIndexInRow = 0; itemIndexInRow < nbItemPerRow; ++itemIndexInRow, ++colorItemIndex)
                        {
                            // TODO use a GUIStyle with margin!
                            var colorRect = GUILayoutUtility.GetRect(Styles.kPaletteItemSize + 2*Styles.kMargin, Styles.kPaletteItemSize + 2*Styles.kMargin);

                            if (colorItemIndex < Session.Palette.Colors.Count)
                            {
                                var contentRect = new Rect(colorRect.x + Styles.kMargin, colorRect.y + Styles.kMargin, colorRect.width - 2 * Styles.kMargin, colorRect.height - 2 * Styles.kMargin);
                                if (Session.Palette.Colors[colorItemIndex].a == 0f)
                                {
                                    EditorGUI.DrawTextureTransparent(contentRect, m_TransparentTex);
                                }
                                else
                                {
                                    EditorGUI.DrawRect(contentRect, Session.Palette.Colors[colorItemIndex]);
                                }

                                if (Event.current.isMouse && Event.current.type == EventType.MouseDown && contentRect.Contains(Event.current.mousePosition))
                                {
                                    Session.CurrentColor = Session.Palette.Colors[colorItemIndex];
                                }
                            }
                        }
                    }
                }
            }
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
                UniPixCommands.CreatePix(Session, 32, 32);
                Repaint();
            }

            if (GUILayout.Button("Load", EditorStyles.toolbarButton))
            {
                UniPixCommands.LoadPix(Session);
                Repaint();
            }

            if (GUILayout.Button("Save", EditorStyles.toolbarButton))
            {
                UniPixCommands.SavePix(Session);
            }

            GUILayout.Label(Session.ImageTitle, EditorStyles.toolbarTextField);

            var settingsRect = GUILayoutUtility.GetRect(Styles.gridSettingsContent, EditorStyles.toolbarButton);
            if (EditorGUI.DropdownButton(settingsRect, Styles.gridSettingsContent, FocusType.Passive, EditorStyles.toolbarButton) && GridSettingsWindow.canShow)
            {
                if (GridSettingsWindow.ShowAtPosition(this, settingsRect))
                    GUIUtility.ExitGUI();
            }

            GUILayout.FlexibleSpace();

            Session.IsDebugDraw = GUILayout.Toggle(Session.IsDebugDraw, "Debug");

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawStatus()
        {
            var status = $"<b>Zoom:</b>{Session.ZoomLevel}  ";
            status += $"<b>Size:</b>[{Session.Image.Width}x{Session.Image.Height}]  ";
            if (Session.CurrentFrameIndex != -1)
                status += $"<b>Frame:</b> {Session.CurrentFrameIndex + 1} / {Session.Image.Frames.Count}  ";
            if (Session.CurrentLayerIndex != -1)
                status += $"<b>Layer:</b> {Session.CurrentLayer.Name}  ";
            status += $"<b>Mouse:</b>[{Session.CursorImgCoord.x}, {Session.CursorImgCoord.y}]  ";
            m_Timer.Stop();
            status += $"<b>Fps:</b>{m_Timer.ElapsedMilliseconds} ";
            GUI.Label(m_StatusRect, status, Styles.statusLabel);
        }

        private void Update()
        {
            if (Session.IsPreviewPlaying)
            {
                if (Session.PreviewFps > 0 && Time.realtimeSinceStartup - Session.PreviewTimer >= (1f / Session.PreviewFps))
                {
                    Session.PreviewFrameIndex = (Session.PreviewFrameIndex + 1) % Session.Image.Frames.Count;
                    Session.PreviewTimer = Time.realtimeSinceStartup;
                    Repaint();
                }
            }
        }

        [MenuItem("Window/UniPix")]
        static void ShowUniPix()
        {
            GetWindow<PixEditor>();
        }

        [MenuItem("Tools/Refresh Styles &r")]
        internal static void RefreshStyles()
        {
            Unsupported.ClearSkinCache();
            InternalEditorUtility.RequestScriptReload();
            InternalEditorUtility.RepaintAllViews();
            Debug.Log("Style refreshed");
        }
    }
}
