#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(CustomCurveAttribute))]
    public class CustomCurveDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            CustomCurveAttribute customCurve = attribute as CustomCurveAttribute;
            Color32 lineColor = new Color32(customCurve.R, customCurve.G, customCurve.B, customCurve.A);

            if (property.propertyType == SerializedPropertyType.AnimationCurve)
            {
                // draw curve
                EditorGUI.CurveField(position, property, lineColor, default, new GUIContent(label));
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }

            EditorGUI.EndProperty();
        }

    } // class end
}

#endif