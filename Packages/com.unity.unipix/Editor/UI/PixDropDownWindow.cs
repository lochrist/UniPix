using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace UniPix
{
    public class PixDropDownWindow : EditorWindow
    {
        static PixEditor s_Editor;
        static object s_Context;
        static Action<PixDropDownWindow, PixEditor, object> s_OnGUI;
        static double s_CloseTime;

        void OnEnable() { }

        [UsedImplicitly]
        internal void OnDestroy()
        {
            s_CloseTime = EditorApplication.timeSinceStartup;
        }

        internal static bool canShow
        {
            get
            {
                if (EditorApplication.timeSinceStartup - s_CloseTime < 0.250)
                    return false;
                return true;
            }
        }

        void OnGUI()
        {
            s_OnGUI(this, s_Editor, s_Context);
        }

        public static bool ShowAtPosition(Rect rect, Vector2 winSize, Action<PixDropDownWindow, PixEditor, object> onGui, PixEditor editor, object context = null)
        {
            var screenPos = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
            var screenRect = new Rect(screenPos, rect.size);
            var window = ScriptableObject.CreateInstance<PixDropDownWindow>();
            s_Editor = editor;
            s_Context = context;
            s_OnGUI = onGui;
            window.ShowAsDropDown(screenRect, winSize);
            return true;
        }

        public static bool DropDownButton(Rect buttonRect, GUIContent content, GUIStyle style)
        {
            return EditorGUI.DropdownButton(buttonRect, content, FocusType.Passive, style) && PixDropDownWindow.canShow;
        }

        public static bool DropDownButton(Rect buttonRect, GUIContent content, GUIStyle style, Vector2 winSize, Action<PixDropDownWindow, PixEditor, object> onGui, PixEditor editor, object context = null)
        {
            if (EditorGUI.DropdownButton(buttonRect, content, FocusType.Passive, style) && PixDropDownWindow.canShow)
            {
                ShowAtPosition(buttonRect, winSize, onGui, editor, context);
                GUIUtility.ExitGUI();
                return true;
            }

            return false;
        }
    }
}