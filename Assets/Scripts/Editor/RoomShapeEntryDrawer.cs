/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/19/2026
 * Last Modified:   08/19/2026 (Ryan)
 * Notes:           Boilerplate - draws a small shape preview thumbnail next to RoomShape/SpawnChance,
 *                  with the Rooms list unrolled below. Thumbnail uses AssetPreview, which only shows
 *                  real cube geometry once ShapeDataEditor implements RenderStaticPreview; until then
 *                  it falls back to the generic asset icon.
*/
#if UNITY_EDITOR
using RyansLibrary.Labyrinth;
using UnityEditor;
using UnityEngine;

namespace RyansLibrary.UnityEditor
{
    [CustomPropertyDrawer(typeof(RoomShapeEntry))]
    public class RoomShapeEntryDrawer : PropertyDrawer
    {
        private const float k_previewSize = 64f;
        private const float k_previewPadding = 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty roomShapeProp = property.FindPropertyRelative("RoomShape");
            SerializedProperty spawnChanceProp = property.FindPropertyRelative("SpawnChance");
            SerializedProperty roomsProp = property.FindPropertyRelative("Rooms");

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float headerHeight = Mathf.Max((lineHeight * 2) + spacing, k_previewSize);

            Rect previewRect = new Rect(position.x, position.y, k_previewSize, k_previewSize);
            Rect fieldsRect = new Rect(position.x + k_previewSize + k_previewPadding, position.y,
                position.width - k_previewSize - k_previewPadding, headerHeight);

            DrawPreview(previewRect, roomShapeProp.objectReferenceValue as ShapeData);

            Rect line = new Rect(fieldsRect.x, fieldsRect.y, fieldsRect.width, lineHeight);
            EditorGUI.PropertyField(line, roomShapeProp);
            line.y += lineHeight + spacing;
            EditorGUI.PropertyField(line, spawnChanceProp);

            float bodyY = position.y + headerHeight + spacing;
            Rect roomsRect = new Rect(position.x, bodyY, position.width, position.yMax - bodyY);
            EditorGUI.PropertyField(roomsRect, roomsProp, true);

            EditorGUI.EndProperty();
        }

        private static void DrawPreview(Rect rect, ShapeData shapeData)
        {
            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.15f));

            if (shapeData == null)
            {
                GUI.Label(rect, "No\nShape", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            // Turn asset into texture and render
            Texture2D preview = AssetPreview.GetAssetPreview(shapeData);
            if (preview == null)
                preview = AssetPreview.GetMiniThumbnail(shapeData);

            if (preview != null)
                GUI.DrawTexture(rect, preview, ScaleMode.ScaleToFit);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float headerHeight = Mathf.Max((lineHeight * 2) + spacing, k_previewSize);

            SerializedProperty roomsProp = property.FindPropertyRelative("Rooms");
            float roomsHeight = EditorGUI.GetPropertyHeight(roomsProp, true);

            return headerHeight + spacing + roomsHeight;
        }
    }
}
#endif
