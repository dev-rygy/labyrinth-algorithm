#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gaskellgames.SceneManagement
{
    [CustomPropertyDrawer(typeof(SceneData))]
    public class SceneDataDrawer : PropertyDrawer
    {
        #region Variables

        SerializedProperty sceneAsset;
        SerializedProperty guid;
        SerializedProperty buildIndex;
        SerializedProperty sceneName;
        SerializedProperty sceneFilePath;

        private string tooltipSuffix = "\n\nEditorOnly References:\n- SceneAsset\n- GUID\n\nRuntime references:\n- Scene (no data to show)\n- buildIndex\n- sceneName\n- sceneFilePath";
        private bool showData;
        float spacer = 2;
        float singleLine = EditorGUIUtility.singleLineHeight;

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Property Height
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (showData)
            {
                return (EditorGUIUtility.singleLineHeight + 2) * 5;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region OnGUI

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            sceneAsset = property.FindPropertyRelative("sceneAsset");
            buildIndex = property.FindPropertyRelative("buildIndex");
            sceneName = property.FindPropertyRelative("sceneName");
            sceneFilePath = property.FindPropertyRelative("sceneFilePath");
            guid = property.FindPropertyRelative("guid");
            
            // open property and get reference to instance
            EditorGUI.BeginProperty(position, label, property);
            
            // draw property
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            GUIContent newLabel = new GUIContent(label.text, label.tooltip + tooltipSuffix);
            EditorGUI.PrefixLabel(position, label);
            float sceneAssetPositionX = position.x + EditorGUIUtility.labelWidth + spacer;
            float sceneAssetWidth = position.width - (EditorGUIUtility.labelWidth + spacer + spacer + 14);
            Rect sceneAssetRect = new Rect(sceneAssetPositionX, position.y, sceneAssetWidth, singleLine);
            sceneAsset.objectReferenceValue = EditorGUI.ObjectField(sceneAssetRect, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                // cache SceneData if SceneAsset updated in inspector
                if (sceneAsset.objectReferenceValue != null)
                {
                    buildIndex.intValue = SceneUtility.GetBuildIndexByScenePath(sceneFilePath.stringValue);
                    sceneName.stringValue = sceneAsset.objectReferenceValue.name;
                    sceneFilePath.stringValue = AssetDatabase.GetAssetOrScenePath(sceneAsset.objectReferenceValue);
                    guid.stringValue = AssetDatabase.AssetPathToGUID(sceneFilePath.stringValue);
                }
                else
                {
                    buildIndex.intValue = -1;
                    sceneName.stringValue = null;
                    sceneFilePath.stringValue = null;
                    guid.stringValue = null;
                }
            }

            // draw toggle
            GUI.enabled = true;
            string tooltip = "Toggle:\n- Show/Hide scene data.\n\nLine:\n- Green if scene is in build scenes and enabled\n- Orange if scene is not in build scenes or is in build scenes and disabled \n- Red if null";
            float togglePositionX = sceneAssetPositionX + sceneAssetWidth + spacer;
            Rect toggleRect = new Rect(togglePositionX, position.y, 14, 14);
            GUIContent toggleLabel = new GUIContent("", tooltip);
            showData = EditorGUI.Toggle(toggleRect, toggleLabel, showData);
            GUI.enabled = false;

            // draw line
            float linePositionX = position.xMax + (spacer * 2) - singleLine;
            float linePositionY = position.y + singleLine - spacer;
            float lineWidth = singleLine - (spacer * 2);
            Rect lineRect = new Rect(linePositionX, linePositionY, lineWidth, spacer);
            bool validBuildIndex = 0 <= buildIndex.intValue && buildIndex.intValue < EditorBuildSettings.scenes.Length;
            if (sceneAsset.objectReferenceValue == null)
            {
                EditorGUI.DrawRect(lineRect, new Color32(179, 050, 050, 255));
            }
            else if (sceneAsset.objectReferenceValue != null && !validBuildIndex)
            {
                EditorGUI.DrawRect(lineRect, new Color32(179, 128, 050, 255));
            }
            else if (sceneAsset.objectReferenceValue != null && validBuildIndex && EditorBuildSettings.scenes[buildIndex.intValue].enabled)
            {
                EditorGUI.DrawRect(lineRect, new Color32(050, 128, 050, 255));
            }
            
            // draw extra data if toggle selected
            EditorGUILayout.EndHorizontal();
            if (showData)
            {
                GUI.enabled = false;
                EditorGUI.indentLevel++;
                
                float buildIndexPositionY = position.y + singleLine + spacer;
                Rect buildIndexRect = new Rect(position.x, buildIndexPositionY, position.width, singleLine);
                GUIContent buildIndexLabel = new GUIContent("Build Index", "Runtime reference to the BuildIndex of the scene.");
                EditorGUI.IntField(buildIndexRect, buildIndexLabel, buildIndex.intValue);
                
                float sceneNamePositionY = buildIndexPositionY + singleLine + spacer;
                Rect sceneNameRect = new Rect(position.x, sceneNamePositionY, position.width, singleLine);
                GUIContent sceneNameLabel = new GUIContent("Scene Name", "Runtime reference to the SceneName of the scene.");
                EditorGUI.TextField(sceneNameRect, sceneNameLabel, sceneName.stringValue);
                
                float sceneFilePathPositionY = sceneNamePositionY + singleLine + spacer;
                Rect sceneFilePathRect = new Rect(position.x, sceneFilePathPositionY, position.width, singleLine);
                GUIContent sceneFilePathLabel = new GUIContent("Scene File Path", "Runtime reference to the SceneFilePath of the scene.");
                EditorGUI.TextField(sceneFilePathRect, sceneFilePathLabel, sceneFilePath.stringValue);

                float guidPositionY = sceneFilePathPositionY + singleLine + spacer;
                Rect guidRect = new Rect(position.x, guidPositionY, position.width, singleLine);
                GUIContent guidLabel = new GUIContent("GUID", "Runtime reference to the GUID of the scene.");
                EditorGUI.TextField(guidRect, guidLabel, guid.stringValue);
                
                EditorGUI.indentLevel--;
                GUI.enabled = true;
            }

            // close property
            EditorGUI.EndProperty();
        }

        #endregion

    } // class end
}
#endif