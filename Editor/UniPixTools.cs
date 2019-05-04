using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UniPix
{
    public class PixTool
    {
        // TODO: cursor position uses color swapper
        readonly Color kCursorColor = new Color(1, 1, 1, 0.5f);

        public Texture2D Icon;

        public virtual void DrawCursor(SessionData session)
        {
            var cursorPosInImg = new Vector2(session.CursorImgCoord.x * session.ZoomLevel, session.CursorImgCoord.y * session.ZoomLevel) + session.ScaledImgRect.position;
            EditorGUI.DrawRect(new Rect(cursorPosInImg, new Vector2(session.ZoomLevel, session.ZoomLevel)), kCursorColor);
        }

        public virtual bool OnEvent(Event current, SessionData session)
        {
            return false;
        }
    }

    public class BrushTool : PixTool
    {
        public override bool OnEvent(Event current, SessionData session)
        {
            DrawCursor(session);
            if (Event.current.isMouse && 
                (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) &&
                (Event.current.button == 0 || Event.current.button == 1)
                )
            {
                // TODO: undo
                var pixelIndex = session.CursorImgCoord.x + (session.Image.Height - session.CursorImgCoord.y - 1) * session.Image.Height;
                session.CurrentLayer.Pixels[pixelIndex] = Event.current.button == 0 ? session.CurrentColor : session.SecondaryColor;
            }
            return true;
        }
    }

    public class EraseTool : PixTool
    {
        public override bool OnEvent(Event current, SessionData session)
        {
            DrawCursor(session);
            if (Event.current.isMouse && (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag))
            {
                // TODO: undo
                var pixelIndex = session.CursorImgCoord.x + (session.Image.Height - session.CursorImgCoord.y - 1) * session.Image.Height;
                session.CurrentLayer.Pixels[pixelIndex] = Color.clear;
            }
            return true;
        }
    }
}