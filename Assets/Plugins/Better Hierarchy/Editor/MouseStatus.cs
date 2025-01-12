using UnityEngine;

namespace Utilities.BetterHierarchy
{
    public static class MouseStatus
    {
        public static bool IsMouseDown
        {
            get
            {
                UpdateMouseEventState();
                return isMouseDown;
            }
        }

        public static bool IsMouseDragged
        {
            get
            {
                UpdateMouseEventState();
                return isMouseDragged;
            }
        }

        private static bool isMouseDown;
        private static bool isMouseDragged;

        public static void UpdateMouseEventState()
        {
            if (Event.current == null)
                return;

            var mouseEvent = Event.current.type;

            if (mouseEvent == EventType.MouseDrag)
                isMouseDragged = true;
            if (mouseEvent == EventType.DragExited)
            {
                isMouseDragged = false;
                isMouseDown = false;
            }

            if (mouseEvent == EventType.MouseDown)
                isMouseDown = true;
            if (mouseEvent == EventType.MouseUp)
                isMouseDown = false;
        }
    }
}