using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(GUIColorAttribute))]
    public class GUIColorDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            GUIColorAttribute customCurve = attribute as GUIColorAttribute;
            Color32 GUIColor = new Color32(customCurve.R, customCurve.G, customCurve.B, customCurve.A);
            
            // draw property with altered GUI color
            if (customCurve.target == GUIColorAttribute.Target.Background)
            {
                Color32 defaultBackgroundColour = GUI.backgroundColor;
                GUI.backgroundColor = GUIColor;
                EditorGUI.PropertyField(position, property, label);
                GUI.backgroundColor = defaultBackgroundColour;
            }
            else if (customCurve.target == GUIColorAttribute.Target.Content)
            {
                Color32 defaultContentColour = GUI.contentColor;
                GUI.contentColor = GUIColor;
                EditorGUI.PropertyField(position, property, label);
                GUI.contentColor = defaultContentColour;
            }
            else
            {
                Color32 defaultGUIColour = GUI.color;
                GUI.color = GUIColor;
                EditorGUI.PropertyField(position, property, label);
                GUI.color = defaultGUIColour;
            }

            EditorGUI.EndProperty();
        }

    } // class end
}
