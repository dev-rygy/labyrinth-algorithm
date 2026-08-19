/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/19/2026
 * Last Modified:   08/19/2026 (Ryan)
 * Notes:           Draws ShapeCellDictionary's backing _keys/_values lists as paired
 *                  Position/State rows instead of two separate raw lists.
*/
#if UNITY_EDITOR
using RyansLibrary.Labyrinth;
using UnityEditor;
using UnityEngine;

namespace RyansLibrary.UnityEditor
{
    [CustomPropertyDrawer(typeof(ShapeCellDictionary))]
    public class ShapeCellDictionaryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty keysProp = property.FindPropertyRelative("_keys");
            SerializedProperty valuesProp = property.FindPropertyRelative("_values");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect line = new Rect(position.x, position.y, position.width, lineHeight);

            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);
            line.y += lineHeight + spacing;

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;

            int removeIndex = -1;
            for (int i = 0; i < keysProp.arraySize; i++)
            {
                float removeWidth = 20f;
                float keyWidth = (line.width - removeWidth - 8f) * 0.6f;
                float valueWidth = (line.width - removeWidth - 8f) * 0.4f;

                Rect keyRect = new Rect(line.x, line.y, keyWidth, lineHeight);
                Rect valueRect = new Rect(keyRect.xMax + 4f, line.y, valueWidth, lineHeight);
                Rect removeRect = new Rect(valueRect.xMax + 4f, line.y, removeWidth, lineHeight);

                EditorGUI.PropertyField(keyRect, keysProp.GetArrayElementAtIndex(i), GUIContent.none);
                EditorGUI.PropertyField(valueRect, valuesProp.GetArrayElementAtIndex(i), GUIContent.none);

                if (GUI.Button(removeRect, "-"))
                    removeIndex = i;

                line.y += lineHeight + spacing;
            }

            if (removeIndex >= 0)
            {
                keysProp.DeleteArrayElementAtIndex(removeIndex);
                valuesProp.DeleteArrayElementAtIndex(removeIndex);
            }

            if (GUI.Button(new Rect(line.x, line.y, line.width, lineHeight), "Add Cell"))
            {
                keysProp.arraySize++;
                valuesProp.arraySize++;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            if (!property.isExpanded)
                return lineHeight;

            SerializedProperty keysProp = property.FindPropertyRelative("_keys");
            int rowCount = keysProp.arraySize + 2; // foldout row + one row per cell + add-button row

            return (lineHeight + spacing) * rowCount;
        }
    }
}
#endif
