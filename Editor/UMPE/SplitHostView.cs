using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    internal class SplitHostView : HostView
    {
        protected override void OnEnable()
        {
            base.OnEnable();

            if (imguiContainer != null)
            {
                imguiContainer.name = "KidsHostView";
                imguiContainer.tabIndex = -1;
            }
        }

        protected override void OldOnGUI()
        {
            ClearBackground();
            EditorGUIUtility.ResetGUIState();

            HandleSplitView();
            InvokeOnGUI(position, new Rect(0, 0, position.width, position.height));
        }

        private void HandleSplitView()
        {
            SplitView sp = parent as SplitView;
            if (Event.current.type == EventType.Repaint && sp)
            {
                View view = this;
                while (sp)
                {
                    int id = sp.controlID;

                    if (id == GUIUtility.hotControl || GUIUtility.hotControl == 0)
                    {
                        int idx = sp.IndexOfChild(view);
                        if (sp.vertical)
                        {
                            if (idx != 0)
                                EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, SplitView.kGrabDist), MouseCursor.SplitResizeUpDown, id);
                            else if (idx != sp.children.Length - 1)
                                EditorGUIUtility.AddCursorRect(new Rect(0, position.height - SplitView.kGrabDist, position.width, SplitView.kGrabDist), MouseCursor.SplitResizeUpDown, id);
                        }
                        else // horizontal
                        {
                            if (idx != 0)
                                EditorGUIUtility.AddCursorRect(new Rect(0, 0, SplitView.kGrabDist, position.height), MouseCursor.SplitResizeLeftRight, id);
                            else if (idx != sp.children.Length - 1)
                                EditorGUIUtility.AddCursorRect(new Rect(position.width - SplitView.kGrabDist, 0, SplitView.kGrabDist, position.height), MouseCursor.SplitResizeLeftRight, id);
                        }
                    }

                    view = sp;
                    sp = sp.parent as SplitView;
                }

                sp = (SplitView)parent;
            }

            if (sp)
            {
                Event e = new Event(Event.current);
                e.mousePosition += new Vector2(position.x, position.y);
                sp.SplitGUI(e);
                if (e.type == EventType.Used)
                    Event.current.Use();
            }
        }
    }
}
