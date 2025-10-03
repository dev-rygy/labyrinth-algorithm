/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/03/2025
 * Last Modified:   10/03/2025 (Ryan)
 * Notes:           
*/
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RyansLibrary.UnityEditor
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;  // disable editing
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
    }
}
#endif
