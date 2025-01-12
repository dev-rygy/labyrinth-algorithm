#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(RangeMinMaxAttribute))]
    public class RangeMinMaxDrawer : PropertyDrawer
    {
        private Color32 textColor = new Color32(128, 128, 128, 255);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            RangeMinMaxAttribute rangeMinMax = attribute as RangeMinMaxAttribute;
            Vector2 minMax = property.vector2Value;
            int floatWidth = 50;
            int gap = 4;

            // calculate extra height for labels
            float height = 0;
            if (rangeMinMax.subLabels)
            {
                height = EditorGUIUtility.singleLineHeight * 0.4f;
            }
            Rect labelPosition = EditorGUILayout.GetControlRect(false, height);

            // draw label, min max slider and float fields
            using (EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope())
            {
                // label
                EditorGUI.PrefixLabel(position, label);

                // min max slider
                float positionX = position.x + EditorGUIUtility.labelWidth + 2;
                float sliderWidth = position.width - (EditorGUIUtility.labelWidth + ((gap + floatWidth + gap) * 2));
                Rect minMaxRect = new Rect(positionX, position.y, sliderWidth, position.height);
                EditorGUI.MinMaxSlider(minMaxRect, ref minMax.x, ref minMax.y, rangeMinMax.min, rangeMinMax.max);

                // input fields
                positionX = position.x + EditorGUIUtility.labelWidth + gap + sliderWidth + gap;
                Rect multiFloatRect = new Rect(positionX, position.y, (floatWidth + gap) * 2, position.height);
                float[] values = new float[] { minMax.x, minMax.y };
                GUIContent[] floatRectLabels =  { new GUIContent("", "Min Value"), new GUIContent("", "Max Value") };
                EditorGUI.MultiFloatField(multiFloatRect, floatRectLabels, values);

                // apply values
                minMax.x = values[0];
                minMax.y = values[1];
                property.vector2Value = MathUtility.RoundVector2(minMax, 2);
            }

            // draw sub labels
            if (rangeMinMax.subLabels)
            {
                labelPosition.height = EditorGUIUtility.singleLineHeight;
                labelPosition.y = position.y + (labelPosition.height * 0.75f);
                labelPosition.x += EditorGUIUtility.labelWidth;
                labelPosition.width -= EditorGUIUtility.labelWidth + gap + floatWidth + gap + floatWidth + gap;
                GUIStyle subLabelStyle = new GUIStyle();
                subLabelStyle.normal.textColor = textColor;
                subLabelStyle.fontSize = 10;
                subLabelStyle.alignment = TextAnchor.UpperLeft;
                EditorGUI.LabelField(labelPosition, rangeMinMax.minLabel, subLabelStyle);
                subLabelStyle.alignment = TextAnchor.UpperRight;
                EditorGUI.LabelField(labelPosition, rangeMinMax.maxLabel, subLabelStyle);
            }

            EditorGUI.EndProperty();
        }

    } // class end
}

#endif