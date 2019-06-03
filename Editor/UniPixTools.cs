using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UniPix
{
    public class PixTool
    {
        // TODO: cursor color uses color swapper
        readonly Color kCursorColor = new Color(1, 1, 1, 0.5f);

        public string Name;
        public Texture2D Icon;

        public static bool IsBrushStroke()
        {
            return Event.current.isMouse &&
                (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag);
        }

        public virtual void DrawCursor(SessionData session)
        {
            var brushRect = session.BrushRect;
            var cursorPosInImg = new Vector2(brushRect.x * session.ZoomLevel, brushRect.y * session.ZoomLevel) + session.ScaledImgRect.position;
            EditorGUI.DrawRect(new Rect(cursorPosInImg, new Vector2(brushRect.width * session.ZoomLevel, brushRect.height * session.ZoomLevel)), kCursorColor);
        }

        public virtual bool OnEvent(Event current, SessionData session)
        {
            return false;
        }
    }

    public class BrushTool : PixTool
    {
        public BrushTool()
        {
            Name = "Brush";
        }

        public override bool OnEvent(Event current, SessionData session)
        {
            // TODO: check if the color is in the current palette or not?

            DrawCursor(session);
            if (IsBrushStroke() &&
                (Event.current.button == 0 || Event.current.button == 1))
            {
                UniPixCommands.SetPixelsUnderBrush(session, Event.current.button == 0 ? session.CurrentColor : session.SecondaryColor);
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
        }

        public override bool OnEvent(Event current, SessionData session)
        {
            DrawCursor(session);
            if (IsBrushStroke() &&
                Event.current.button == 0)
            {
                UniPixCommands.SetPixelsUnderBrush(session, Color.clear);
                return true;
            }
            return false;
        }
    }
}