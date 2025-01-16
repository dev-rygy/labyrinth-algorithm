#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(MinAttribute))]
    public class MinDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // open property and get reference to attribute instance
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            MinAttribute min = attribute as MinAttribute;

            if (property.propertyType == SerializedPropertyType.Float)
            {
                float limitedValue = Mathf.Max(property.floatValue, min.min);
                property.floatValue = EditorGUI.FloatField(position, label, limitedValue);
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                int limitedValue = Mathf.Max(property.intValue, (int)min.min);
                property.intValue = EditorGUI.IntField(position, label, limitedValue);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }

            // close property
            EditorGUI.EndProperty();
        }

    } // class end
}
#endif