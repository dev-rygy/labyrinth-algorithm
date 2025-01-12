#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(ProgressBarAttribute))]
    public class ProgressBarDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            ProgressBarAttribute progressBar = attribute as ProgressBarAttribute;

            // set label
            string barLabel = progressBar.label;
            if (barLabel == "")
            {
                barLabel = ObjectNames.NicifyVariableName(label.ToString());
            }
            
            // set color
            Color32 barColor = new Color32(progressBar.R, progressBar.G, progressBar.B, progressBar.A);
            
            // set sizes
            int floatWidth = 50;
            int gap = 4;
            float width = position.width - (floatWidth + gap);
            Rect barBackground = new Rect(position.x, position.yMin, width, position.height);
            Rect floatRect = new Rect(position.xMax - floatWidth, position.yMin, floatWidth, position.height);
            
            // draw bar
            if (fieldInfo.FieldType == typeof(int))
            {
                int value = property.intValue;
                float barValue = value / progressBar.maxValue;
                if (barValue < 0) { barValue = 0; }
                else if (1 < barValue) { barValue = 1; }
                ProgressBar(barBackground, barValue, barLabel, barColor);
                if (progressBar.readOnly) { GUI.enabled = false; }
                property.intValue = (int)EditorGUI.FloatField(floatRect, GUIContent.none, value);
                GUI.enabled = true;
            }
            else if(fieldInfo.FieldType == typeof(float))
            {
                float value = property.floatValue;
                float barValue = value / progressBar.maxValue;
                if (barValue < 0) { barValue = 0; }
                else if (1 < barValue) { barValue = 1; }
                ProgressBar(barBackground, barValue, barLabel, barColor);
                if (progressBar.readOnly) { GUI.enabled = false; }
                property.floatValue = EditorGUI.FloatField(floatRect, GUIContent.none, value);
                GUI.enabled = true;
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }
            
            EditorGUI.EndProperty();
        }
        
        private void ProgressBar(Rect barPosition, float fillPercent, string label, Color barColor)
        {
            // create inner barPosition
            int border = 1;
            Rect barPositionInner = new Rect(barPosition.x + border, barPosition.y + border, barPosition.width - (border * 2), barPosition.height - (border * 2));
            
            // draw background
            EditorGUI.DrawRect(barPosition, new Color32(028, 028, 028, 255));
            EditorGUI.DrawRect(barPositionInner, new Color32(045, 045, 045, 255));
            
            // draw bar
            Rect fillRect = new Rect(barPositionInner.xMin, barPositionInner.yMin, barPositionInner.width * fillPercent, barPositionInner.height);
            EditorGUI.DrawRect(fillRect, barColor);
            fillRect = new Rect(barPositionInner.xMin, barPositionInner.yMin, barPositionInner.width * fillPercent, 2);
            EditorGUI.DrawRect(fillRect, barColor * 1.1f);
            fillRect = new Rect(barPositionInner.xMin, barPositionInner.yMax - 2 , barPositionInner.width * fillPercent, 2);
            EditorGUI.DrawRect(fillRect, barColor * 0.9f);

            // set label alignment
            TextAnchor defaultAlignment = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;

            // draw label
            Rect labelRect = new Rect(barPositionInner.xMin, barPositionInner.yMin, barPositionInner.width, barPositionInner.height);
            GUI.Label(labelRect, label);

            // reset label alignment
            GUI.skin.label.alignment = defaultAlignment;
        }
        
    } // class end
}

#endif