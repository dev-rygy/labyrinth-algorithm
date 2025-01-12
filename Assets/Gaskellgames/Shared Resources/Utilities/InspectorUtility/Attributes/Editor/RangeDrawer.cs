#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(RangeAttribute))]
    public class RangeDrawer : PropertyDrawer
    {
        private Color32 textColor = new Color32(128, 128, 128, 255);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            RangeAttribute range = attribute as RangeAttribute;
            int floatWidth = 50;
            int gap = 4;

            // calculate extra height for labels
            float height = 0;
            if (range.subLabels)
            {
                height = EditorGUIUtility.singleLineHeight * 0.4f;
            }
            Rect labelPosition = EditorGUILayout.GetControlRect(false, height);

            // draw label, slider and float field
            using (EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope())
            {
                // draw slider
                if (fieldInfo.FieldType == typeof(int))
                {
                    property.intValue = (int)EditorGUI.Slider(position, label, property.intValue, range.min, range.max);
                }
                else if (fieldInfo.FieldType == typeof(float))
                {
                    property.floatValue = EditorGUI.Slider(position, label, property.floatValue, range.min, range.max);
                }
                else
                {
                    EditorGUI.PropertyField(position, property, label);
                }
            }

            // draw sub labels
            if (range.subLabels)
            {
                labelPosition.height = EditorGUIUtility.singleLineHeight;
                labelPosition.y = position.y + (labelPosition.height * 0.75f);
                labelPosition.x += EditorGUIUtility.labelWidth;
                labelPosition.width -= EditorGUIUtility.labelWidth + floatWidth + gap;
                GUIStyle subLabelStyle = new GUIStyle();
                subLabelStyle.normal.textColor = textColor;
                subLabelStyle.fontSize = 10;
                subLabelStyle.alignment = TextAnchor.UpperLeft;
                EditorGUI.LabelField(labelPosition, range.minLabel, subLabelStyle);
                subLabelStyle.alignment = TextAnchor.UpperRight;
                EditorGUI.LabelField(labelPosition, range.maxLabel, subLabelStyle);
            }

            EditorGUI.EndProperty();
        }
    } // class end
}
#endif