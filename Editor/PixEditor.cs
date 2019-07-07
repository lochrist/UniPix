using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

namespace UniPix
{
    [System.Serializable]
    public class PixSession
    {
        public static PixSession Create()
        {
            var session = new PixSession();
            session.ImageSessionState = ScriptableObject.CreateInstance<PixImageSessionState>();
            return session;
        }

        public float ZoomLevel = 20f;

        public PixImage Image;

        Texture2D m_Overlay;

        public bool HasOverlay => m_Overlay != null;
        public void ClearOverlay(bool apply = true)
        {
            PixUtils.SetTextureColor(Overlay, Color.clear, apply);
        }
        public void DestroyOverlay()
        {
            if (m_Overlay)
            {
                Object.DestroyImmediate(m_Overlay);
            }
            m_Overlay = null;
        }
        public Texture2D Overlay
        {
            get
            {
                if (m_Overlay == null)
                {
                    m_Overlay = PixUtils.CreateTexture(Image.Width, Image.Height);
                    PixUtils.SetTextureColor(m_Overlay, Color.clear);
                }
                return m_Overlay;
            }
        }

        public void SetOverlay(int imgCoordX, int imgCoordY, Color color, bool apply = true)
        {
            Overlay.SetPixel(imgCoordX, Overlay.height - imgCoordY - 1, color);
            if (apply)
                Overlay.Apply();
        }

        public string ImagePath;
        public string ImageTitle;
        public bool IsImageDirty;

        public int CurrentLayerIndex
        {
            get => ImageSessionState.CurrentLayerIndex;
            set => ImageSessionState.CurrentLayerIndex = value;
        }

        public int CurrentFrameIndex
        {
            get => ImageSessionState.CurrentFrameIndex;
            set => ImageSessionState.CurrentFrameIndex = value;
        }

        public Frame CurrentFrame => Image.Frames[CurrentFrameIndex];
        public Layer CurrentLayer => CurrentFrame.Layers[CurrentLayerIndex];

        public PixImageSessionState ImageSessionState;

        public Color CurrentColor = new Color(1, 0, 0);
        public int CurrentColorPaletteIndex = -1;
        public Color SecondaryColor = Color.black;
        public int SecondaryColorPaletteIndex = -1;

        public int CurrentToolIndex;

        public Vector2 CanvasSize;

        public float ImageOffsetX;
        public float ImageOffsetY;
        public Rect ScaledImgRect;
        public Vector2Int CursorImgCoord;
        public Vector2 CursorPos;
        public int CursorPixelIndex => ImgCoordToPixelIndex(CursorImgCoord.x, CursorImgCoord.y);
        public RectInt BrushRect {
            get
            {
                var halfBrush = BrushSize / 2;
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

        public int BrushSize = 1;
        public Palette Palette;
        public int ImgCoordToPixelIndex(int imgCoordX, int imgCoordY)
        {
            return PixUtils.ImgCoordToPixelIndex(Image, imgCoordX, imgCoordY);
        }
        public int PreviewFps = 4;
        public int PreviewFrameIndex = 0;
        public bool IsPreviewPlaying = true;
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
        public static PixSession s_Session;
        public PixSession Session;
        public static class Prefs
        {
            public static string kPrefix = "unixpix.";
            public static string kCurrentImg = $"{kPrefix}currentImg";
        }
        public static class Styles
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

            public static GUIContent newContent = new GUIContent(Icons.newImage, "New Image");
            public static GUIContent loadContent = new GUIContent(Icons.folder, "Load Image");
            public static GUIContent saveContent = new GUIContent(Icons.diskette, "Save Image");
            public static GUIContent gridSettingsContent = new GUIContent(Icons.cog);
            public static GUIContent exportContent = new GUIContent(Icons.export);
            public static GUIContent syncContent = new GUIContent(Icons.counterClockwiseRotation, "Save and sync Sources");
            public static GUIContent colorSwitcherContent = new GUIContent(Icons.colorSwapAndArrow, "Swap Primary and Secondary");

            public static GUIStyle layerHeader = new GUIStyle(EditorStyles.boldLabel);
            public static GUIStyle layerName = new GUIStyle(EditorStyles.largeLabel);
            public static GUIStyle currentLayerName = new GUIStyle(EditorStyles.largeLabel);
            public static GUIStyle layerOpacitySlider = new GUIStyle(GUI.skin.horizontalSlider)
            {
                margin = new RectOffset(0, 15, 0, 0)
            };
            public static GUIStyle brushSlider = new GUIStyle(GUI.skin.horizontalSlider)
            {
                margin = new RectOffset(0, 17, 0, 0)
            };
            public static GUIStyle layerVisible = new GUIStyle(EditorStyles.toggle)
            {
                margin = new RectOffset(0, 0, 4, 0),
                padding = new RectOffset(0, 0, 4, 0)
            };
            public static GUIStyle layerLocked = new GUIStyle(EditorStyles.toggle);
            public static GUIStyle layerToolbarBtn = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = 30,
                fixedHeight = 30
            };

            public static GUIStyle frameBtn = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(2, 2, 2, 2)
            };

            public static GUIStyle statusLabel = new GUIStyle(EditorStyles.label)
            {
                richText = true
            };
            public static GUIStyle colorSwap = new GUIStyle()
            {
                name = "colorSwap"
            };
            public static GUIStyle pixMain = new GUIStyle()
            {
                name = "pixmain",
                padding = new RectOffset(2, 0, 0, 0)
            };

            public static GUIStyle pixBox = new GUIStyle()
            {
                name = "pixbox",
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(2, 2, 2, 2)
            };

            public static GUIStyle selectedPixBox = new GUIStyle(pixBox)
            {
                name = "selected-pixbox"
            };

            public static GUIStyle primaryColorBox = new GUIStyle(pixBox)
            {
                name = "primary-color-box"
            };

            public static GUIStyle secondaryColorBox = new GUIStyle(pixBox)
            {
                name = "secondary-color-box"
            };

            public static readonly GUIStyle itemBackground1 = new GUIStyle
            {
                name = "pix-item-background1",
            };

            public static readonly GUIStyle itemBackground2 = new GUIStyle(itemBackground1)
            {
                name = "pix-item-background2",
            };

            static Styles()
            {
                currentLayerName.normal.textColor = Color.yellow;
            }
        }

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
        Rect m_RightPanelRect;
        Texture2D m_TransparentTex;
        System.Diagnostics.Stopwatch m_Timer = new System.Diagnostics.Stopwatch();
        bool m_IsPanning;
        Vector2 m_PanStart;

        PixTool CurrentTool => m_Tools[Session.CurrentToolIndex];
        PixTool[] m_Tools;

        public void UpdateCanvasSize()
        {
            Session.CanvasSize = new Vector2(
                Math.Max(position.width, minSize.x) - Styles.kLeftPanelWidth - Styles.kRightPanelWidth - 2 * Styles.kMargin,
                Math.Max(position.height, minSize.y) - Styles.kToolbarHeight - Styles.kStatusbarHeight);
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("UniPix");
            minSize = new Vector2(Styles.kLeftPanelWidth + Styles.kRightPanelWidth + 100, 400);
            Session = PixSession.Create();

            m_Tools = new PixTool[] {
                new BrushTool(),
                new EraseTool(),
                new BucketTool(), 
                new BucketFullTool(), 
                new LineTool(), 
                new RectangleTool(),
                new DitheringTool()
            };
            Session.CurrentToolIndex = 0;
            s_Session = Session;
            UpdateCanvasSize();
            PixCommands.LoadPix(Session, EditorPrefs.GetString(Prefs.kCurrentImg, null));

            m_TransparentTex = PixUtils.CreateTexture(1, 1);
            PixUtils.SetTextureColor(m_TransparentTex, Color.clear);

            wantsMouseMove = true;

            Undo.undoRedoPerformed -= OnUndo;
            Undo.undoRedoPerformed += OnUndo;
        }

        private void OnDisable()
        {
            s_Session = null;
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
                DrawCanvas();

                GUILayout.BeginArea(m_RightPanelRect);
                Session.RightPanelScroll = GUILayout.BeginScrollView(Session.RightPanelScroll);
                DrawLayers();
                DrawColorPalette();
                DrawFrameSource();
                GUILayout.EndScrollView();
                GUILayout.EndArea();
                DrawStatus();
            }
        }

        private void OnUndo()
        {
            Session.CurrentFrame.UpdateFrame();
            Repaint();
        }

        private void ComputeLayout()
        {
            var verticalMaxHeight = position.height - Styles.kToolbarHeight - Styles.kStatusbarHeight;

            m_ToolbarRect = new Rect(Styles.kMargin, 0, position.width, Styles.kToolbarHeight);
            { // Column 1
                m_AnimPreviewRect = new Rect(m_ToolbarRect.x, m_ToolbarRect.yMax, Styles.kFramePreviewWidth, Styles.kFramePreviewWidth);
                const int kToolsSpacing = 10;
                m_ToolsPaletteRect = new Rect(m_ToolbarRect.x, 
                    m_AnimPreviewRect.yMax + kToolsSpacing, 
                    Styles.kToolPaletteWidth, 
                    (m_Tools.Length) * (Styles.kToolSize + Styles.kMargin));
            }

            { // Column 2
                m_FramePreviewRect = new Rect(
                    m_ToolsPaletteRect.xMax,
                    m_ToolbarRect.yMax,
                    Styles.kFramePreviewWidth,
                    verticalMaxHeight);
            }

            { // Column 3
                UpdateCanvasSize();
                m_CanvasRect = new Rect(m_FramePreviewRect.xMax + Styles.kMargin,
                    m_ToolbarRect.yMax + Styles.kMargin,
                    Session.CanvasSize.x, Session.CanvasSize.y);
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
        }

        private static void DrawDebugRect(Rect rect, string title, Color c)
        {
            EditorGUI.DrawRect(rect, c);
            GUI.Label(rect, title);
        }

        private void ProcessEvents()
        {
            var e = Event.current;
            if (ModeService.currentId != "unipix" && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.W)
                {
                    Session.ImageOffsetY -= 20f;
                }
                if (e.keyCode == KeyCode.S)
                {
                    Session.ImageOffsetY += 20f;
                }
                if (e.keyCode == KeyCode.A)
                {
                    Session.ImageOffsetX -= 20f;
                }
                if (e.keyCode == KeyCode.D)
                {
                    Session.ImageOffsetX += 20f;
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
                        PixCommands.CreateLayer(Session);
                        Repaint();
                        GUIUtility.ExitGUI();
                    }

                    if (GUILayout.Button(Styles.cloneLayer, Styles.layerToolbarBtn))
                    {
                        PixCommands.CloneLayer(Session);
                        Repaint();
                        GUIUtility.ExitGUI();
                    }

                    using (new EditorGUI.DisabledScope(Session.CurrentLayerIndex == Session.CurrentFrame.Layers.Count - 1))
                    {
                        if (GUILayout.Button(Styles.moveLayerUp, Styles.layerToolbarBtn))
                        {
                            PixCommands.SwapLayers(Session, Session.CurrentLayerIndex, Session.CurrentLayerIndex + 1);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }
                    }

                    using (new EditorGUI.DisabledScope(Session.CurrentLayerIndex == 0))
                    {
                        if (GUILayout.Button(Styles.moveLayerDown, Styles.layerToolbarBtn))
                        {
                            PixCommands.SwapLayers(Session, Session.CurrentLayerIndex, Session.CurrentLayerIndex - 1);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }

                        if (GUILayout.Button(Styles.mergeLayer, Styles.layerToolbarBtn))
                        {
                            PixCommands.MergeLayers(Session, Session.CurrentLayerIndex, Session.CurrentLayerIndex - 1);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }
                    }

                    if (GUILayout.Button(Styles.deleteLayer, Styles.layerToolbarBtn))
                    {
                        PixCommands.DeleteLayer(Session);
                        Repaint();
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUI.BeginChangeCheck();
                var opacity = PixUtils.Slider($"Opacity {(int)(Session.CurrentLayer.Opacity * 100)}%",
                    Session.CurrentLayer.Opacity, 0f, 1f, Styles.layerOpacitySlider
                    );
                if (EditorGUI.EndChangeCheck())
                {
                    PixCommands.SetLayerOpacity(Session, Session.CurrentLayerIndex, opacity);
                    Repaint();
                }
                GUILayout.Space(5);

                for (var i = Session.CurrentFrame.Layers.Count - 1; i >= 0; i--)
                {
                    var layer = Session.CurrentFrame.Layers[i];
                    var bgStyle = i % 2 == 1 ? Styles.itemBackground1 : Styles.itemBackground2;
                    GUILayout.BeginHorizontal(bgStyle);

                    if (GUILayout.Button(layer.Name, i == Session.CurrentLayerIndex ? Styles.currentLayerName : Styles.layerName, GUILayout.ExpandWidth(true)))
                    {
                        PixCommands.SetCurrentLayer(Session, i);
                        Repaint();
                    }

                    EditorGUI.BeginChangeCheck();
                    var isLayerVisible = GUILayout.Toggle(layer.Visible, "", Styles.layerVisible);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PixCommands.SetLayerVisibility(Session, i, isLayerVisible);
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
            Session.BrushSize = (int)PixUtils.Slider($"{Session.BrushSize}", Session.BrushSize, 1, 6, Styles.brushSlider);

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
                    if (GUI.Toggle(toolRect, toolIndex == Session.CurrentToolIndex, tool.Content, GUI.skin.button))
                    {
                        Session.CurrentToolIndex = toolIndex;
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
                var frameRect = PixUtils.LayoutFrameTile(frame, frameIndex == Session.CurrentFrameIndex);
                if (frameRect.Contains(Event.current.mousePosition))
                {
                    if (GUI.Button(new Rect(frameRect.x + Styles.kMargin, frameRect.y + Styles.kMargin, Styles.kFramePreviewBtn, 
                        Styles.kFramePreviewBtn), Styles.cloneFrame, Styles.frameBtn))
                    {
                        PixCommands.CloneFrame(Session, frameIndex);
                        Repaint();
                        GUIUtility.ExitGUI();
                        eventUsed = true;
                    }

                    if (GUI.Button(new Rect(frameRect.xMax - Styles.kFramePreviewBtn - Styles.kMargin, frameRect.y + Styles.kMargin, 
                        Styles.kFramePreviewBtn, Styles.kFramePreviewBtn), Styles.deleteFrame, Styles.frameBtn))
                    {
                        PixCommands.DeleteFrame(Session, frameIndex);
                        Repaint();
                        GUIUtility.ExitGUI();
                        eventUsed = true;
                    }
                }

                if (!eventUsed && frameRect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
                {
                    PixCommands.SetCurrentFrame(Session, frameIndex);
                    Repaint();
                    GUIUtility.ExitGUI();
                }

                ++frameIndex;
            }

            if (GUILayout.Button("New Frame"))
            {
                PixCommands.NewFrame(Session);
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

            EditorGUI.BeginChangeCheck();
            var color = EditorGUI.ColorField(secondaryColorRect, new GUIContent(""), Session.SecondaryColor, false, false, false);
            if (EditorGUI.EndChangeCheck())
            {
                PixCommands.SetBrushColor(Session, 1, color);
            }
            GUI.Box(secondaryColorRect, "", Styles.secondaryColorBox);

            EditorGUI.BeginChangeCheck();
            color = EditorGUI.ColorField(primaryColorRect, new GUIContent(""), Session.CurrentColor, false, false, false);
            if (EditorGUI.EndChangeCheck())
            {
                PixCommands.SetBrushColor(Session, 0, color);
            }
            GUI.Box(primaryColorRect, "", Styles.primaryColorBox);
            var switcher = new Rect(primaryColorRect.x + 7, primaryColorRect.yMax, 30, 30);
            if (GUI.Button(switcher, Styles.colorSwitcherContent, Styles.colorSwap))
            {
                PixCommands.SwitchColor(Session);
            }
        }

        private void DrawCanvas()
        {
            if (Event.current != null)
            {
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        if (!m_CanvasRect.Contains(Event.current.mousePosition))
                            break;
                        var objs = DragAndDrop.objectReferences
                            .Where(PixUtils.IsValidPixSource).ToArray();

                        if (objs.Length == 0 && DragAndDrop.paths.Length > 0)
                        {
                            objs = DragAndDrop.paths
                                .Select(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>)
                                .Where(PixUtils.IsValidPixSource).ToArray();
                        }

                        if (objs.Length > 0)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            if (Event.current.type == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();
                                Event.current.Use();
                                PixCommands.LoadPix(Session, objs);
                                GUIUtility.ExitGUI();
                            }
                        }
                        break;
                }
            }

            var xScale = Session.Image.Width * Session.ZoomLevel;
            var yScale = Session.Image.Height * Session.ZoomLevel;
            Session.ScaledImgRect = new Rect(Session.ImageOffsetX, Session.ImageOffsetY, xScale, yScale);

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(m_CanvasRect, new Color(0.4f, 0.4f, 0.4f));

            GUILayout.BeginArea(m_CanvasRect);
            {
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawTextureTransparent(Session.ScaledImgRect, m_TransparentTex);
                    var tex = Session.CurrentFrame.Texture;
                    GUI.DrawTexture(Session.ScaledImgRect, tex);
                }
                if (Session.ScaledImgRect.Contains(Event.current.mousePosition))
                {
                    Session.CursorPos = Event.current.mousePosition - Session.ScaledImgRect.position;
                    Session.CursorImgCoord = new Vector2Int((int)(Session.CursorPos.x / Session.ZoomLevel), (int)(Session.CursorPos.y / Session.ZoomLevel));
                    if (CurrentTool.OnEvent(Event.current, Session))
                    {
                        Repaint();
                    }
                    else if (Event.current.isMouse &&
                            Event.current.button == 2)
                    {
                        Pan();
                    }
                }

                if (Event.current.type == EventType.Repaint && Session.ShowGrid && Session.ZoomLevel > 2)
                {
                    DrawGrid();
                }

                if (Event.current.type == EventType.Repaint && Session.HasOverlay)
                {
                    GUI.DrawTexture(Session.ScaledImgRect, Session.Overlay);
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
                Session.ImageOffsetX += panningDistance.x;
                Session.ImageOffsetY += panningDistance.y;
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
                if (Event.current.type == EventType.Repaint)
                {
                    GUI.Box(frameRect, "", Styles.pixBox);
                    GUI.DrawTexture(frameRect, tex, ScaleMode.ScaleToFit);
                }
                
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
                                if (Event.current.type == EventType.Repaint)
                                {
                                    if (Session.Palette.Colors[colorItemIndex].a == 0f)
                                    {
                                        EditorGUI.DrawTextureTransparent(contentRect, m_TransparentTex);
                                    }
                                    else
                                    {
                                        EditorGUI.DrawRect(contentRect, Session.Palette.Colors[colorItemIndex]);
                                        if (Session.CurrentColorPaletteIndex == colorItemIndex)
                                        {
                                            GUI.Box(contentRect, "", Styles.primaryColorBox);
                                        }
                                        else if (Session.SecondaryColorPaletteIndex == colorItemIndex)
                                        {
                                            GUI.Box(contentRect, "", Styles.secondaryColorBox);
                                        }
                                    }
                                }
                                else if (Event.current.isMouse && 
                                    Event.current.type == EventType.MouseDown && 
                                    contentRect.Contains(Event.current.mousePosition) &&
                                    (Event.current.button == 0 || Event.current.button == 1))
                                {
                                    PixCommands.SetBrushColor(Session, Event.current.button, Session.Palette.Colors[colorItemIndex]);
                                    Repaint();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawFrameSource()
        {
            using (new GUILayout.VerticalScope(Styles.pixBox))
            {
                GUILayout.Label("Frame Source", Styles.layerHeader);

                using (new PixUtils.FieldWidthScope(45, 45))
                {
                    EditorGUI.BeginChangeCheck();
                    var sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", Session.CurrentFrame.SourceSprite, typeof(Sprite), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PixCommands.ReplaceSourceSprite(Session, sprite);
                        Repaint();
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUI.DisabledScope(Session.CurrentFrame.SourceSprite == null || !Session.IsImageDirty))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(Styles.syncContent, GUILayout.MaxWidth(32)))
                    {
                        PixCommands.SavePix(Session);
                        PixCommands.UpdateFrameSprite(Session.CurrentFrame);
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void DrawToolbar()
        {
            GUILayout.BeginArea(m_ToolbarRect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            if (ModeService.currentId != "unipix")
            {
                if (GUILayout.Button(Styles.newContent, EditorStyles.toolbarButton))
                {
                    PixCommands.CreatePix(Session, 32, 32);
                    Repaint();
                }

                if (GUILayout.Button(Styles.loadContent, EditorStyles.toolbarButton))
                {
                    PixCommands.LoadPix(Session);
                    Repaint();
                }

                using (new EditorGUI.DisabledScope(!Session.IsImageDirty && !string.IsNullOrEmpty(Session.ImagePath)))
                {
                    if (GUILayout.Button(Styles.saveContent, EditorStyles.toolbarButton))
                    {
                        PixCommands.SavePix(Session);
                    }
                }

                var syncRect = GUILayoutUtility.GetRect(Styles.syncContent, EditorStyles.toolbarButton);
                var areAllSourcesSet = Session.Image.Frames.All(f => f.SourceSprite != null);
                if (!areAllSourcesSet && EditorGUI.DropdownButton(syncRect, Styles.syncContent, FocusType.Passive, EditorStyles.toolbarButton) && SyncWindow.canShow)
                {
                    if (SyncWindow.ShowAtPosition(this, syncRect))
                        GUIUtility.ExitGUI();
                }
                else if (GUI.Button(syncRect, Styles.syncContent, EditorStyles.toolbarButton))
                {
                    PixCommands.SaveImageSources(Session);
                }

                var settingsRect = GUILayoutUtility.GetRect(Styles.gridSettingsContent, EditorStyles.toolbarButton);
                if (EditorGUI.DropdownButton(settingsRect, Styles.gridSettingsContent, FocusType.Passive, EditorStyles.toolbarButton) && GridSettingsWindow.canShow)
                {
                    if (GridSettingsWindow.ShowAtPosition(this, settingsRect))
                        GUIUtility.ExitGUI();
                }

                var exportRect = GUILayoutUtility.GetRect(Styles.exportContent, EditorStyles.toolbarButton);
                if (EditorGUI.DropdownButton(exportRect, Styles.exportContent, FocusType.Passive, EditorStyles.toolbarButton) && ExportWindow.canShow)
                {
                    if (ExportWindow.ShowAtPosition(this, exportRect))
                        GUIUtility.ExitGUI();
                }
            }
            else
            {
                GUILayout.Space(25);
            }

            GUILayout.Label(Session.ImageTitle, EditorStyles.toolbarTextField, GUILayout.MinWidth(250));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Center"))
            {
                PixCommands.FrameImage(Session);
            }

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

        #region Menu
        [MenuItem("Window/UniPix", false, 1100)]
        static void ShowUniPix()
        {
            var pixEditor = GetWindow<PixEditor>();
            pixEditor.UpdateCanvasSize();
            PixCommands.FrameImage(pixEditor.Session);
        }

        [UsedImplicitly, MenuItem("Tools/Refresh Styles &r")]
        static void RefreshStyles()
        {
            Unity.MPE.ChannelService.Start();

            Unsupported.ClearSkinCache();
            InternalEditorUtility.RequestScriptReload();
            InternalEditorUtility.RepaintAllViews();
            Debug.Log("Style refreshed");
        }

        [UsedImplicitly, MenuItem("Assets/Open in UniPix", false, 180000)]
        private static void OpenInPix()
        { 
            if (Selection.objects.Any(PixUtils.IsValidPixSource))
            {
                PixCommands.EditInPix(Selection.objects);
            }
        }

        [UsedImplicitly, MenuItem("Assets/Open in UniPix", true, 180000)]
        private static bool OpenInPixValidate()
        {
            return Selection.objects.Any(PixUtils.IsValidPixSource);
        }

        static bool m_IsTracking;
        [MenuItem("Tools/Track performance _F9")]
        private static void TogglePerformanceTracking()
        {
            if (m_IsTracking)
            {
                Debug.Log("Stop tracking");
                m_IsTracking = false;
                var rawFile = $"D:/work/performancetracking/draw_block_{GUID.Generate()}_deep.raw";
                ProfilerDriver.SaveProfile(rawFile);
                ProfilerDriver.profileEditor = false;
                ProfilerDriver.enabled = false;
                // ProfilerDriver.SaveProfile();

                ProfilerDriver.LoadProfile(rawFile, false);
                var win = GetWindow<ProfilerWindow>();
                win.Show();
            }
            else
            {
                Debug.Log("Start tracking");
                m_IsTracking = true;
                // ProfilerDriver.deepProfiling = true;
                ProfilerDriver.profileEditor = true;
                ProfilerDriver.enabled = true;
            }
        }

        #endregion
    }
}
