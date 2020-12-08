using UnityEngine;
using UnityEditor;

namespace UniPix
{
    public class PixTool
    {
        readonly Color kCursorColor = new Color(1, 1, 1, 0.5f);
        public string Name;
        public Texture2D Icon;
        public GUIContent Content;

        public static Color StrokeColor(PixSession session)
        {
            return Event.current.button == 0 ? session.CurrentColor : session.SecondaryColor;
        }

        public static bool IsBrushStroke()
        {
            return Event.current.isMouse &&
                (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag);
        }

        public virtual void DrawCursor(PixSession session)
        {
            var brushRect = session.BrushRect;
            var cursorPosInImg = new Vector2(brushRect.x * session.ZoomLevel, brushRect.y * session.ZoomLevel) + session.ScaledImgRect.position;
            EditorGUI.DrawRect(new Rect(cursorPosInImg, new Vector2(brushRect.width * session.ZoomLevel, brushRect.height * session.ZoomLevel)), kCursorColor);
        }

        public virtual bool OnEvent(Event current, PixSession session)
        {
            return false;
        }
    }

    public class BrushTool : PixTool
    {
        public static string kName = "Brush";
        public BrushTool()
        {
            Name = kName;
            Content = new GUIContent(Icons.pencil, "Brush tool");
        }

        public override bool OnEvent(Event current, PixSession session)
        {
            DrawCursor(session);
            if (IsBrushStroke() &&
                (Event.current.button == 0 || Event.current.button == 1))
            {
                var strokeColor = StrokeColor(session);
                if (!session.Palette.Colors.Contains(strokeColor))
                {
                    PixCommands.AddPaletteColor(session, strokeColor);
                }
                PixCommands.SetPixelsUnderBrush(session, strokeColor);
                return true;
            }
            return false;
        }
    }

    public class EraseTool : PixTool
    {
        public static string kName = "Eraser";
        public EraseTool()
        {
            Name = kName;
            Content = new GUIContent(Icons.eraser, "Eraser");
        }

        public override bool OnEvent(Event current, PixSession session)
        {
            DrawCursor(session);
            if (IsBrushStroke() &&
                Event.current.button == 0)
            {
                PixCommands.SetPixelsUnderBrush(session, Color.clear);
                return true;
            }
            return false;
        }
    }

    public class RectangleTool : PixTool
    {
        public static string kName = "Rectangle";
        bool m_Active;
        Vector2Int m_Start;
        public RectangleTool()
        {
            Name = kName;
            Content = new GUIContent(Icons.rectangle, "Rectangle (hold Ctrl for filled)");
        }

        public override bool OnEvent(Event current, PixSession session)
        {
            if (!m_Active)
                DrawCursor(session);
            if (Event.current.isMouse &&
                (Event.current.button == 0 || Event.current.button == 1))
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    m_Active = true;
                    m_Start = session.CursorImgCoord;
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    session.ClearOverlay();
                    var pixels = session.Overlay.GetPixels();
                    var strokeColor = StrokeColor(session);
                    if (Event.current.control)
                    {
                        PixUtils.DrawFilledRectangle(session.Image, m_Start, session.CursorImgCoord, strokeColor, pixels);
                    }
                    else
                    {
                        PixUtils.DrawRectangle(session.Image, m_Start, session.CursorImgCoord, strokeColor, session.BrushSize, pixels);
                    }
                    session.Overlay.SetPixels(pixels);
                    session.Overlay.Apply();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    session.DestroyOverlay();
                    using (new PixCommands.SessionChangeScope(session, "Rectangle"))
                    {
                        var pixels = session.CurrentLayer.Pixels;
                        var strokeColor = StrokeColor(session);
                        if (Event.current.control)
                        {
                            PixUtils.DrawFilledRectangle(session.Image, m_Start, session.CursorImgCoord, strokeColor, pixels);
                        }
                        else
                        {
                            PixUtils.DrawRectangle(session.Image, m_Start, session.CursorImgCoord, strokeColor, session.BrushSize, pixels);
                        }
                    }

                    m_Active = false;
                }
                return true;
            }
            return false;
        }
    }


    public class LineTool : PixTool
    {
        public static string kName = "Line";
        Vector2Int m_Start;
        public LineTool()
        {
            Name = kName;
            Content = new GUIContent(Icons.stroke, "Line");
        }

        public override bool OnEvent(Event current, PixSession session)
        {
            DrawCursor(session);
            if (Event.current.isMouse &&
                (Event.current.button == 0 || Event.current.button == 1))
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    m_Start = session.CursorImgCoord;
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    session.ClearOverlay();
                    var pixels = session.Overlay.GetPixels();
                    var strokeColor = StrokeColor(session);

                    PixUtils.DrawLine(session.Image, m_Start, session.CursorImgCoord, strokeColor, session.BrushSize, pixels);

                    session.Overlay.SetPixels(pixels);
                    session.Overlay.Apply();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    session.DestroyOverlay();
                    using (new PixCommands.SessionChangeScope(session, "Rectangle"))
                    {
                        var pixels = session.CurrentLayer.Pixels;
                        var strokeColor = StrokeColor(session);
                        PixUtils.DrawLine(session.Image, m_Start, session.CursorImgCoord, strokeColor, session.BrushSize, pixels);
                    }
                }
                return true;
            }
            return false;
        }
    }

    public class RectangleSelection : PixTool
    {
        public static string kName = "Selection";
        bool m_Active;
        Vector2Int m_Start;
        public RectangleSelection()
        {
            Name = kName;
            Content = new GUIContent(Icons.rectangle, "Rectangle Selection");
        }

        public override bool OnEvent(Event current, PixSession session)
        {
            if (!m_Active)
                DrawCursor(session);
            if (Event.current.isMouse &&
                (Event.current.button == 0 || Event.current.button == 1))
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    m_Active = true;
                    m_Start = session.CursorImgCoord;
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    
                    m_Active = false;
                }
                return true;
            }
            return false;
        }
    }

    public class BucketTool : PixTool
    {
        public static string kName = "Bucket";
        public BucketTool()
        {
            Name = kName;
            Content = new GUIContent(Icons.bucket, "Bucket");
        }

        public override bool OnEvent(Event current, PixSession session)
        {
            DrawCursor(session);
            if (Event.current.isMouse &&
                (Event.current.button == 0 || Event.current.button == 1) &&
                Event.current.type == EventType.MouseUp)
            {
                using (new PixCommands.SessionChangeScope(session, "Bucket"))
                {
                    var pixels = session.CurrentLayer.Pixels;
                    var strokeColor = StrokeColor(session);
                    var pixelIndex = session.CursorPixelIndex;
                    var currentCursorColor = pixels[pixelIndex];
                    PixUtils.FloodFill(session.Image, session.CursorImgCoord, currentCursorColor, strokeColor, pixels);
                }
                return true;
            }
            return false;
        }
    }

    public class BucketFullTool : PixTool
    {
        public static string kName = "BucketFull";
        public BucketFullTool()
        {
            Name = kName;
            Content = new GUIContent(Icons.bucketFull, "Full Frame Bucket");
        }

        public override bool OnEvent(Event current, PixSession session)
        {
            DrawCursor(session);
            if (Event.current.isMouse &&
                (Event.current.button == 0 || Event.current.button == 1) &&
                Event.current.type == EventType.MouseUp)
            {
                using (new PixCommands.SessionChangeScope(session, "Bucket"))
                {
                    var pixels = session.CurrentLayer.Pixels;
                    var strokeColor = StrokeColor(session);
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = strokeColor;
                    }
                }
                return true;
            }
            return false;
        }
    }

    public class DitheringTool : PixTool
    {
        public static string kName = "Dithering";
        public DitheringTool()
        {
            Name = kName;
            Content = new GUIContent(Icons.dithering, "Dithering");
        }

        public override bool OnEvent(Event current, PixSession session)
        {
            DrawCursor(session);
            if (IsBrushStroke() &&
                (Event.current.button == 0 || Event.current.button == 1))
            {
                using (new PixCommands.SessionChangeScope(session, "Dithering"))
                {
                    var brushRect = session.BrushRect;
                    for (var y = brushRect.y; y < brushRect.yMax; ++y)
                    {
                        for (var x = brushRect.x; x < brushRect.xMax; ++x)
                        {
                            var strokeColor = (x + y) % 2 == 0 ? session.SecondaryColor : session.CurrentColor;
                            var pixelIndex = session.ImgCoordToPixelIndex(x, y);
                            session.CurrentLayer.Pixels[pixelIndex] = strokeColor;
                        }
                    }
                }
                return true;
            }
            return false;
        }
    }
}