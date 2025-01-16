#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code updated by Gaskellgames
/// </summary>

namespace Gaskellgames.FolderSystem
{
    [CustomEditor(typeof(HierarchyFolders))][CanEditMultipleObjects]
    public class HierarchyFoldersEditor : GGEditor
    {
        #region Serialized Properties / OnEnable

        SerializedProperty customText;
        SerializedProperty customIcon;
        SerializedProperty customHighlight;
        
        SerializedProperty textColor;
        SerializedProperty iconColor;
        SerializedProperty highlightColor;
        SerializedProperty textStyle;
        SerializedProperty textAlignment;

        private void OnEnable()
        {
            customText = serializedObject.FindProperty("customText");
            customIcon = serializedObject.FindProperty("customIcon");
            customHighlight = serializedObject.FindProperty("customHighlight");
            
            textColor = serializedObject.FindProperty("textColor");
            iconColor = serializedObject.FindProperty("iconColor");
            highlightColor = serializedObject.FindProperty("highlightColor");
            textStyle = serializedObject.FindProperty("textStyle");
            textAlignment = serializedObject.FindProperty("textAlignment");
        }

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region OnInspectorGUI

        public override void OnInspectorGUI()
        {
            // get & update references
            HierarchyFolders hierarchyFolders = (HierarchyFolders)target;
            serializedObject.Update();

            // draw banner if turned on in Gaskellgames settings
            EditorWindowUtility.DrawInspectorBanner("Assets/Gaskellgames/Folder System/Editor/Icons/InspectorBanner_FolderSystem.png");

            // custom inspector
            EditorGUILayout.PropertyField(textStyle);
            EditorGUILayout.PropertyField(textAlignment);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Custom Colors");
            GUILayout.Space(2);
            GUIContent textLabel = new GUIContent("Text", "Custom text color");
            GUIContent iconLabel = new GUIContent("Icon", "Custom icon color");
            GUIContent backgroundLabel = new GUIContent("Highlight", "Custom highlight color");
            customText.boolValue = EditorGUILayout.ToggleLeft(textLabel, customText.boolValue, GUILayout.Width(55), GUILayout.ExpandWidth(false));
            customIcon.boolValue = EditorGUILayout.ToggleLeft(iconLabel, customIcon.boolValue, GUILayout.Width(55), GUILayout.ExpandWidth(false));
            customHighlight.boolValue = EditorGUILayout.ToggleLeft(backgroundLabel, customHighlight.boolValue, GUILayout.Width(100), GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
            if (customText.boolValue)
            {
                EditorGUILayout.PropertyField(textColor);
            }
            if (customIcon.boolValue)
            {
                EditorGUILayout.PropertyField(iconColor);
            }
            if (customHighlight.boolValue)
            {
                EditorGUILayout.PropertyField(highlightColor);
            }

            // apply reference changes
            serializedObject.ApplyModifiedProperties();
        }

    #endregion

    } // class end
}
        
#endif