/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/18/2026
 * Last Modified:   08/18/2026 (Ryan)
 * Notes:           Compact toolbar-style drawer for RoomCell.Walls - shows the 6 fixed
 *                  wall faces as toggle buttons instead of a foldout list, expanding
 *                  the selected face's Transform/bool fields below the toolbar.
*/
#if UNITY_EDITOR
using RyansLibrary.Labyrinth;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RyansLibrary.UnityEditor
{
    [CustomPropertyDrawer(typeof(RoomCell))]
    public class RoomCellDrawer : PropertyDrawer
    {
        // Matches the EntryPointFlags face order used throughout the generator (README "Room Entry Points"):
        // +X, -X, +Z, -Z, +Y, -Y
        private static readonly string[] FaceNames = { "W0", "W1", "W2", "W3", "W4", "W5" };
        private const int k_wallCount = 6;

        // Which wall face is expanded, keyed by propertyPath so each RoomCell in a list toggles independently.
        private static readonly Dictionary<string, int> _selectedFace = new Dictionary<string, int>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            // Init properties
            SerializedProperty positionProp = property.FindPropertyRelative("Position");
            SerializedProperty availableProp = property.FindPropertyRelative("IsAvilable");
            SerializedProperty wallsProp = property.FindPropertyRelative("Walls");

            // Safety check
            EnsureSixWalls(wallsProp);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect line = new Rect(position.x, position.y, position.width, lineHeight);

            // Position property
            EditorGUI.PropertyField(line, positionProp);
            line.y += lineHeight + spacing;

            // IsAvailable property
            EditorGUI.PropertyField(line, availableProp);
            line.y += lineHeight + spacing;

            // Walls property
            EditorGUI.LabelField(line, "Walls", EditorStyles.boldLabel);
            line.y += lineHeight + spacing;

            // Flexable buttons
            float buttonWidth = position.width / k_wallCount;
            Rect buttonRect = new Rect(position.x, line.y, buttonWidth, lineHeight);

            // Button Logic
            string key = property.propertyPath;
            if (!_selectedFace.TryGetValue(key, out int selected))
                selected = -1;

            for (int i = 0; i < k_wallCount; i++)
            {
                bool isSelected = selected == i;
                bool hasTransform = wallsProp.arraySize > i && wallsProp.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("WallTransform").objectReferenceValue != null;
                bool isExempt = wallsProp.arraySize > i && wallsProp.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("IsExemptFromMutation").boolValue;

                GUI.backgroundColor = isSelected ? Color.cornflowerBlue : (hasTransform ? Color.lightGray : Color.red);

                if (GUI.Button(buttonRect, FaceNames[i]))
                {
                    selected = isSelected ? -1 : i;
                    _selectedFace[key] = selected;
                }

                if (isExempt)
                {
                    const float markSize = 6f;
                    Rect markRect = new Rect(buttonRect.xMax - markSize - 3, buttonRect.y + 3, markSize, markSize);
                    EditorGUI.DrawRect(markRect, Color.yellow);
                }

                buttonRect.x += buttonWidth;
            }
            GUI.backgroundColor = Color.white;

            line.y += lineHeight + spacing;

            if (selected >= 0 && selected < wallsProp.arraySize)
            {
                SerializedProperty wallProp = wallsProp.GetArrayElementAtIndex(selected);
                SerializedProperty transformProp = wallProp.FindPropertyRelative("WallTransform");
                SerializedProperty exemptProp = wallProp.FindPropertyRelative("IsExemptFromMutation");

                EditorGUI.indentLevel++;

                EditorGUI.PropertyField(line, transformProp, new GUIContent($"{FaceNames[selected]} Transform"));
                line.y += lineHeight + spacing;

                EditorGUI.PropertyField(line, exemptProp, new GUIContent("Exempt From Mutation"));

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Position + IsAvilable + "Walls" header + face button row
            float height = lineHeight * 4;

            if (_selectedFace.TryGetValue(property.propertyPath, out int selected) && selected >= 0)
                height += lineHeight * 2; // expanded wall's Transform + bool rows

            return height;
        }

        // RoomCell is defined to always have exactly 6 walls (one per face); pad/truncate here so the
        // toolbar always has a fixed slot to write into, regardless of how the struct was constructed.
        private void EnsureSixWalls(SerializedProperty wallsProp)
        {
            if (wallsProp.arraySize != k_wallCount)
                wallsProp.arraySize = k_wallCount;
        }
    }
}
#endif
