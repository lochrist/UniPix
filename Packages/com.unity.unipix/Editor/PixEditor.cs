using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UniPix
{
    public class PixEditor : EditorWindow
    {
        public static string packageName = "com.unity.unipix";
        public static string packageFolderName = $"Packages/{packageName}";
        public PixSession Session;

        public static class Prefs
        {
            public static string kPrefix = PixIO.useProject ? "unixpix." : "unipix.app.";
            public static string kCurrentImg = $"{kPrefix}currentImg";
            public static string kLastSaveImgFolder = $"{kPrefix}lastSaveImg";
            public static string kLastExportImgFolder = $"{kPrefix}lastExportImg";
            public static string kLastOpenImgFolder = $"{kPrefix}lastOpenImg";
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
        Texture2D m_GridTex;
        bool m_IsPanning;
        Vector2 m_PanStart;

        PixTool CurrentTool => Tools[Session.CurrentToolIndex];
        public PixTool[] Tools;

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

            PixMode.s_Session = Session;
            PixMode.s_Editor = this;

            Tools = new PixTool[] {
                new BrushTool(),
                new EraseTool(),
                new BucketTool(), 
                new BucketFullTool(), 
                new LineTool(), 
                new RectangleTool(),
                new DitheringTool(),
                new RectangleSelection()
            };

            SetCurrentTool(0);
            UpdateCanvasSize();

            var initialImage = EditorPrefs.GetString(Prefs.kCurrentImg, null);
            if (string.IsNullOrEmpty(initialImage))
            {
                PixCommands.NewImage(Session, 32, 32);
            }
            else
            {
                PixCommands.OpenImage(Session, initialImage);
            }

            m_TransparentTex = PixCore.CreateTexture(1, 1);
            PixCore.SetTextureColor(m_TransparentTex, Color.clear);

            wantsMouseMove = true;

            Undo.undoRedoPerformed -= OnUndo;
            Undo.undoRedoPerformed += OnUndo;

            EditorApplication.delayCall += () => PixCommands.FrameImage(Session);
        }

        internal void ResetGrid()
        {
            m_GridTex = null;
            UpdateGridTex();
        }

        private void OnDisable()
        {
            PixMode.s_Session = null;
        }

        private void OnGUI()
        {
            // using (new EditorPerformanceTracker("PixMode"))
            {
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
                    if (PixIO.useProject)
                        DrawFrameSource();
                    GUILayout.EndScrollView();
                    GUILayout.EndArea();
                    DrawStatus();
                }
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
                    (Tools.Length) * (Styles.kToolSize + Styles.kMargin));
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
                    PixCommands.IncreaseZoom(Session);
                }
                if (e.keyCode == KeyCode.DownArrow)
                {
                    PixCommands.DecreaseZoom(Session);
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
                        PixCommands.CloneCurrentLayer(Session);
                        Repaint();
                        GUIUtility.ExitGUI();
                    }

                    using (new EditorGUI.DisabledScope(Session.CurrentLayerIndex == Session.CurrentFrame.Layers.Count - 1))
                    {
                        if (GUILayout.Button(Styles.moveLayerUp, Styles.layerToolbarBtn))
                        {
                            PixCommands.MoveCurrentLayerUp(Session);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }
                    }

                    using (new EditorGUI.DisabledScope(Session.CurrentLayerIndex == 0))
                    {
                        if (GUILayout.Button(Styles.moveLayerDown, Styles.layerToolbarBtn))
                        {
                            PixCommands.MoveCurrentLayerDown(Session);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }

                        if (GUILayout.Button(Styles.mergeLayer, Styles.layerToolbarBtn))
                        {
                            PixCommands.MergeLayers(Session);
                            Repaint();
                            GUIUtility.ExitGUI();
                        }
                    }

                    if (GUILayout.Button(Styles.deleteLayer, Styles.layerToolbarBtn))
                    {
                        PixCommands.DeleteCurrentLayer(Session);
                        Repaint();
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUI.BeginChangeCheck();
                var opacity = PixUI.Slider($"Opacity {(int)(Session.CurrentLayer.Opacity * 100)}%",
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
            EditorGUI.BeginChangeCheck();
            var brushSize = (int)PixUI.Slider($"{Session.BrushSize}", Session.BrushSize, 1, 6, Styles.brushSlider);
            if (EditorGUI.EndChangeCheck())
            {
                PixCommands.SetBrushSize(Session, brushSize);
            }

            GUILayout.Label("Tools", Styles.layerHeader);

            var toolsRect = GUILayoutUtility.GetRect(m_ToolsPaletteRect.width, 100);
            var nbRows = (Tools.Length / Styles.kNbToolsPerRow) + 1;
            var toolIndex = 0;
            for(var rowIndex = 0; rowIndex < nbRows; ++rowIndex)
            {
                var toolY = toolsRect.y + rowIndex * Styles.kToolSize + Styles.kMargin;
                for (var toolColumn = 0; toolColumn < Styles.kNbToolsPerRow; ++toolColumn, ++toolIndex)
                {
                    if (toolIndex >= Tools.Length)
                        break;
                    var tool = Tools[toolIndex];
                    var toolRect = new Rect(Styles.kMargin + toolColumn * (Styles.kMargin + Styles.kToolSize), toolY, Styles.kToolSize, Styles.kToolSize);

                    EditorGUI.BeginChangeCheck();
                    GUI.Toggle(toolRect, toolIndex == Session.CurrentToolIndex, tool.Content, GUI.skin.button);
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetCurrentTool(toolIndex);
                    }
                }
            }

            // TODO : indicate which colors corresponds to which MouseButton
            // TODO: Palette editing: remove from palette. Add new color??

            GUILayout.EndArea();
        }

        private void SetCurrentTool(int currentToolIndex)
        {
            if (Session.CurrentToolIndex != -1)
                Tools[Session.CurrentToolIndex].OnDisable(Session);
            Session.CurrentToolIndex = currentToolIndex;
            Tools[Session.CurrentToolIndex].OnEnable(Session);
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
                var frameRect = PixUI.LayoutFrameTile(frame, frameIndex == Session.CurrentFrameIndex);
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

                        var objs = PixIO.GetDragAndDropContent();
                        if (objs.Length > 0)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            if (Event.current.type == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();
                                Event.current.Use();
                                PixCommands.OpenImage(Session, objs);
                                GUIUtility.ExitGUI();
                            }
                        }
                        break;
                }
            }

            var xScale = Session.Image.Width * Session.ZoomLevel;
            var yScale = Session.Image.Height * Session.ZoomLevel;
            Session.ScaledImgRect = new Rect(Session.ImageOffsetX, Session.ImageOffsetY, xScale, yScale);
            if (m_GridTex == null || Session.GridPixelSize != m_GridTex.width || Session.GridPixelSize != m_GridTex.height)
            {
                ResetGrid();
            }

            if (Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(m_CanvasRect, Styles.canvasColor);

            GUILayout.BeginArea(m_CanvasRect);
            {
                if (Event.current.type == EventType.Repaint)
                {
                    var tex = Session.CurrentFrame.Texture;
                    if (Session.ShowCheckerPattern)
                    {
                        GUI.DrawTextureWithTexCoords(
                            Session.ScaledImgRect,
                            PixUI.GetTransparentCheckerTexture(),
                            new Rect(
                                0,
                                0,
                                Session.ScaledImgRect.width / Session.CheckPatternSize / Session.ZoomLevel,
                                Session.ScaledImgRect.height / Session.CheckPatternSize / Session.ZoomLevel),
                            false);
                    }
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
                    // using (new DebugLogTimer("Draw grid"))
                    {
                        DrawGrid();
                    }
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
            if (m_GridTex != null)
            {
                var rect = Session.ScaledImgRect;
                GUI.DrawTextureWithTexCoords(Session.ScaledImgRect, m_GridTex, new Rect(0, 0, Session.ScaledImgRect.width / m_GridTex.width, Session.ScaledImgRect.height / m_GridTex.height));
                EditorGUI.DrawRect(new Rect(Session.ScaledImgRect.xMax, Session.ScaledImgRect.yMin, 1, Session.ScaledImgRect.height), Session.GridColor);
                EditorGUI.DrawRect(new Rect(Session.ScaledImgRect.xMin, Session.ScaledImgRect.yMin, Session.ScaledImgRect.width, 1), Session.GridColor);
            }
            else
            {
                DrawGridFromRect();
            }
        }

        private void UpdateGridTex()
        {
            // using (new DebugLogTimer("Gen grid"))
            {
                var zoom = (int)Session.ZoomLevel;
                var gridSize = zoom * 3 * Session.GridSize;
                m_GridTex = PixCore.CreateTexture(gridSize, gridSize);
                m_GridTex.wrapMode = TextureWrapMode.Repeat;
                var pixels = m_GridTex.GetPixels();
                for (int i = 0; i < pixels.Length; ++i)
                    pixels[i] = Color.clear;
                m_GridTex.SetPixels(pixels);

                var gridStep = Session.GridSize * zoom;
                for (int x = 0; x < gridSize; x += gridStep)
                {
                    for (int y = 0; y < gridSize; ++y)
                    {
                        m_GridTex.SetPixel(x, y, Session.GridColor);
                    }
                }
                // Then x axis
                for (int y = 0; y < gridSize; y += gridStep)
                {
                    for (int x = 0; x < gridSize; ++x)
                    {
                        m_GridTex.SetPixel(x, y, Session.GridColor);
                    }
                }
                m_GridTex.Apply();
                Repaint();
            }
        }

        private void DrawGridFromRect()
        {
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
                    PixUI.DrawFrame(frameRect, tex, false);
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
            EditorGUI.BeginChangeCheck();
            var previewFps = (int)GUI.HorizontalSlider(new Rect(labelRect.xMax, labelRect.y,
                    frameRect.width - labelRect.width - Styles.kMargin, labelRect.height), Session.PreviewFps, PixSession.k_MinPreviewFps, PixSession.k_MaxPreviewFps);
            if (EditorGUI.EndChangeCheck())
            {
                PixCommands.SetPreviewFps(Session, previewFps);
            }
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

                using (new PixUI.FieldWidthScope(45, 45))
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
                        PixCommands.SaveImage(Session);
                        PixIO.UpdateFrameSourceSprite(Session.CurrentFrame);
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void DrawToolbar()
        {
            GUILayout.BeginArea(m_ToolbarRect, EditorStyles.toolbar);
            GUILayout.BeginHorizontal();

            
            if (GUILayout.Button(Styles.newContent, EditorStyles.toolbarButton))
            {
                PixCommands.NewImage(Session, 32, 32);
                Repaint();
            }

            if (GUILayout.Button(Styles.loadContent, EditorStyles.toolbarButton))
            {
                PixCommands.OpenImage(Session);
                Repaint();
            }

            using (new EditorGUI.DisabledScope(!Session.IsImageDirty && !string.IsNullOrEmpty(Session.Image.Path)))
            {
                if (GUILayout.Button(Styles.saveContent, EditorStyles.toolbarButton))
                {
                    PixCommands.SaveImage(Session);
                }
            }

            if (PixIO.useProject)
            {
                var syncRect = GUILayoutUtility.GetRect(Styles.syncContent, EditorStyles.toolbarButton);
                var areAllSourcesSet = Session.Image.Frames.All(f => f.SourceSprite != null);
                if (!areAllSourcesSet && EditorGUI.DropdownButton(syncRect, Styles.syncContent, FocusType.Passive, EditorStyles.toolbarButton) && SyncWindow.canShow)
                {
                    if (SyncWindow.ShowAtPosition(this, syncRect))
                        GUIUtility.ExitGUI();
                }
                else if (GUI.Button(syncRect, Styles.syncContent, EditorStyles.toolbarButton))
                {
                    PixIO.UpdateImageSourceSprites(Session);
                }
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
        #endregion

        #region Debug Menu
        [UsedImplicitly, MenuItem("UniPix Debug/Refresh Styles &r")]
        static void RefreshStyles()
        {
            UnityEditor.MPE.ChannelService.Start();

            Unsupported.ClearSkinCache();
            EditorUtility.RequestScriptReload();
            InternalEditorUtility.RepaintAllViews();
            Debug.Log("Style refreshed");
        }

        [UsedImplicitly, MenuItem("UniPix Debug/Go to PixMode", false, 10000)]
        static void SwitchToPix()
        {
            ModeService.ChangeModeById("unipix");
        }

        [UsedImplicitly, MenuItem("UniPix Debug/Dynamic Pix Layout", false, 10000)]
        static void DynPixLayout()
        {
            PixMode.LayoutDynPix(null);
        }

        [UsedImplicitly, MenuItem("UniPix Debug/Dynamic Pix Browse Layout", false, 10000)]
        static void DynPixBrowseLayout()
        {
            PixMode.LayoutDynPixBrowse(null);
        }

        [UsedImplicitly, MenuItem("UniPix Debug/Dynamic Pix Debug Layout", false, 10000)]
        static void DynPixDebugLayout()
        {
            PixMode.LayoutDynPixDebug(null);
        }
        #endregion
    }
}