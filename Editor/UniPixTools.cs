using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UniPix
{
    public class PixTool
    {
        public Texture2D Icon;

        public virtual void OnEvent(Event current, Vector2Int cursorImgPos, Vector2 cursorPos, SessionData session)
        {

        }
    }
}