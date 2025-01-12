using UnityEditor;
using UnityEngine;

namespace Utilities.BetterHierarchy
{
    public static class UnityEditorBackgroundColor
    {
        public struct SystemColor
        {
            public Color DarkColor { set; private get; }
            public Color LightColor { set; private get; }
        
            public static explicit operator Color(SystemColor systemColor)
            {
                return EditorGUIUtility.isProSkin ? systemColor.DarkColor : systemColor.LightColor;
            }
        }
        

        private static SystemColor Default => new SystemColor
        {
            DarkColor = new Color(0.2196f, 0.2196f, 0.2196f),
            LightColor = new Color(0.7843f, 0.7843f, 0.7843f)
        };

        private static SystemColor Selected => new SystemColor
        {
            DarkColor = new Color(0.1725f, 0.3647f, 0.5294f),
            LightColor = new Color(0.22745f, 0.447f, 0.6902f)
        };

        private static SystemColor SelectedUnfocused => new SystemColor
        {
            DarkColor = new Color(0.3f, 0.3f, 0.3f),
            LightColor = new Color(0.68f, 0.68f, 0.68f)
        };

        private static SystemColor Hovered => new SystemColor
        {
            DarkColor = new Color(0.2706f, 0.2706f, 0.2706f),
            LightColor = new Color(0.698f, 0.698f, 0.698f)
        };
        
        
        public static Color Get(HierarchyObjectStatus hierarchyObjectStatus, bool isWindowFocused, int selectedAmount = 0)
        {
            return (Color)GetSystemColor(hierarchyObjectStatus, isWindowFocused, selectedAmount);
        }

        
        private static SystemColor GetSystemColor(HierarchyObjectStatus obj, bool isWindowFocused, int selectedObjectsAmount = 0)
        {
            bool isMouseDown = MouseStatus.IsMouseDown;
            
            if (obj.IsSelected)
            {
                if (isMouseDown && !obj.IsDropDownHovered && !obj.IsHovered && selectedObjectsAmount == 1)
                    return Default;

                return isWindowFocused ? Selected : SelectedUnfocused;
            }
            else if (obj.IsHovered)
                return isMouseDown && !obj.IsDropDownHovered ? Selected : Hovered;
            else
                return Default;
        }
    }
}