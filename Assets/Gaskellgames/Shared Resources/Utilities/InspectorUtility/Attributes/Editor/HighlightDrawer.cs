using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(HighlightAttribute))]
    public class HighlightDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            HighlightAttribute highlight = attribute as HighlightAttribute;
            
            EditorGUI.PropertyField(position, property, label);
            
            Color32 outlineColor = new Color32(highlight.R, highlight.G, highlight.B, highlight.A);
            Rect topBorder = new Rect(position.xMin - 1, position.yMin - 1, position.width + 2, 1);
            EditorGUI.DrawRect(topBorder, outlineColor);
            Rect bottomBorder = new Rect(position.xMin - 1, position.yMax, position.width + 2, 1);
            EditorGUI.DrawRect(bottomBorder, outlineColor);
            Rect leftBorder = new Rect(position.xMin - 1, position.yMin - 1, 1, position.height + 2);
            EditorGUI.DrawRect(leftBorder, outlineColor);
            Rect rightBorder = new Rect(position.xMax, position.yMin - 1, 1, position.height + 2);
            EditorGUI.DrawRect(rightBorder, outlineColor);
            
            EditorGUI.EndProperty();
        }
        
    } // class end
}
