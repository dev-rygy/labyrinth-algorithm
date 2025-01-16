#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public static class HierarchyUtility
    {
        #region Helper Functions

        public static void DrawHierarchyIcon<T>(int instanceID, Rect position, Texture2D icon, int pixelOffset = 0)
        {
            // check for valid draw
            if (Event.current.type != EventType.Repaint) { return; }

            // draw icon
            if (icon != null)
            {
                GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (gameObject != null)
                {
                    if (gameObject.GetComponent<T>() != null)
                    {
                        float hierarchyPixelHeight = 16;
                        float offset = hierarchyPixelHeight + pixelOffset;
                        EditorGUIUtility.SetIconSize(new Vector2(hierarchyPixelHeight, hierarchyPixelHeight));
                        Rect iconPosition = new Rect(position.xMax - offset, position.yMin, position.width, position.height);
                        GUIContent iconGUIContent = new GUIContent(icon);
                        EditorGUI.LabelField(iconPosition, iconGUIContent);
                    }
                }
            }
        }

        public static int CheckForHierarchyIconOffset<T>(int instanceID)
        {
            GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject != null)
            {
                int hierarchyPixelHeight = 16;
                if (gameObject.GetComponent<T>() != null)
                {
                    return hierarchyPixelHeight;
                }
            }
            
            return 0;
        }

        #endregion
        
    } // class end
}

#endif