/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/21/2026
 * Last Modified:   08/21/2026 (Ryan)
 * Notes:           Shows only the fields relevant to RoomEntry's PlacementType - Fixed shows
 *                  SpawnPosition, Constrained shows Bounds, Free shows neither. Prefab and
 *                  PlacementType itself are always shown.
*/
#if UNITY_EDITOR
using RyansLibrary.Labyrinth;
using UnityEditor;
using UnityEngine;

namespace RyansLibrary.UnityEditor
{
    [CustomPropertyDrawer(typeof(RoomEntry))]
    public class RoomEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty prefabProp = property.FindPropertyRelative("Prefab");
            SerializedProperty placementTypeProp = property.FindPropertyRelative("PlacementType");
            SerializedProperty spawnPositionProp = property.FindPropertyRelative("SpawnPosition");
            SerializedProperty boundsProp = property.FindPropertyRelative("Bounds");

            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect line = new Rect(position.x, position.y, position.width, 0f);

            // Prefab and PlacementType each carry a [Header] decorator, drawn as part of PropertyField -
            // reserve each field's real height (decorator included) rather than assuming a single line,
            // or the decorator text overlaps the field below it.
            line.height = FieldHeight(prefabProp);
            EditorGUI.PropertyField(line, prefabProp, true);
            line.y += line.height + spacing;

            line.height = FieldHeight(placementTypeProp);
            EditorGUI.PropertyField(line, placementTypeProp, true);
            line.y += line.height + spacing;

            RoomPlacementType placementType = (RoomPlacementType)placementTypeProp.enumValueIndex;
            switch (placementType)
            {
                case RoomPlacementType.Fixed:
                    line.height = FieldHeight(spawnPositionProp);
                    EditorGUI.PropertyField(line, spawnPositionProp, true);
                    break;
                case RoomPlacementType.Constrained:
                    line.height = FieldHeight(boundsProp);
                    EditorGUI.PropertyField(line, boundsProp, true);
                    break;
                case RoomPlacementType.Free:
                    break;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            SerializedProperty prefabProp = property.FindPropertyRelative("Prefab");
            SerializedProperty placementTypeProp = property.FindPropertyRelative("PlacementType");

            // Prefab + PlacementType, always shown
            float height = FieldHeight(prefabProp) + spacing + FieldHeight(placementTypeProp);

            RoomPlacementType placementType = (RoomPlacementType)placementTypeProp.enumValueIndex;
            switch (placementType)
            {
                case RoomPlacementType.Fixed:
                    SerializedProperty spawnPositionProp = property.FindPropertyRelative("SpawnPosition");
                    height += spacing + FieldHeight(spawnPositionProp);
                    break;
                case RoomPlacementType.Constrained:
                    SerializedProperty boundsProp = property.FindPropertyRelative("Bounds");
                    height += spacing + FieldHeight(boundsProp);
                    break;
                case RoomPlacementType.Free:
                    break;
            }

            return height;
        }

        private static float FieldHeight(SerializedProperty property) => EditorGUI.GetPropertyHeight(property, true);
    }
}
#endif
