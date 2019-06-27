﻿using UnityEngine;
using UnityEditor;

namespace UniPix
{
    public class PixTool
    {
        readonly Color kCursorColor = new Color(1, 1, 1, 0.5f);
        public string Name;
        public Texture2D Icon;
        public GUIContent Content;

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
        public BrushTool()
        {
            Name = "Brush";
            Content = new GUIContent(Icons.pencil, "Brush tool");
        }

        public override bool OnEvent(Event current, PixSession session)
        {
            DrawCursor(session);
            if (IsBrushStroke() &&
                (Event.current.button == 0 || Event.current.button == 1))
            {
                var strokeColor = Event.current.button == 0 ? session.CurrentColor : session.SecondaryColor;
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
        public EraseTool()
        {
            Name = "Erase";
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
        public RectangleTool()
        {
            Name = "Rectangle";
            Content = new GUIContent(Icons.rectangle, "Rectangle");
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

    public class LineTool : PixTool
    {
        public LineTool()
        {
            Name = "Line";
            Content = new GUIContent(Icons.stroke, "Line");
        }

        public override bool OnEvent(Event current, PixSession session)
        {
            DrawCursor(session);
            if (Event.current.isMouse &&
                Event.current.button == 0)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    Debug.Log("It is down");
                    session.SetOverlay(session.CursorImgCoord.x, session.CursorImgCoord.y, Color.blue);
                }
                else if (Event.current.type == EventType.MouseDrag)
                {
                    Debug.Log("Dragging: " + Event.current.mousePosition);
                    session.SetOverlay(session.CursorImgCoord.x, session.CursorImgCoord.y, Color.blue);
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    Debug.Log("It isup!");
                    session.ClearOverlay();
                }
                return true;
            }
            return false;
        }
    }

    public class BucketTool : PixTool
    {
        public BucketTool()
        {
            Name = "Bucket";
            Content = new GUIContent(Icons.bucket, "Bucket");
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

    public class BucketFullTool : PixTool
    {
        public BucketFullTool()
        {
            Name = "BucketFull";
            Content = new GUIContent(Icons.bucketFull, "Full Frame Bucket");
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

    public class DitheringTool : PixTool
    {
        public DitheringTool()
        {
            Name = "Dithering";
            Content = new GUIContent(Icons.dithering, "Dithering");
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
}