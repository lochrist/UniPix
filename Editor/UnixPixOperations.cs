using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace UniPix
{
    public static class UnixPixOperations
    {
        public static void CreateLayer(SessionData session)
        {
            session.CurrentFrame.AddLayer(session.Image.Width, session.Image.Height);
        }
    }
}