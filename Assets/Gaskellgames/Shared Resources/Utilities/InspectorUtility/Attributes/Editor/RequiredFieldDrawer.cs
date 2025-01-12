#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(RequiredFieldAttribute))]
    public class RequiredFieldDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            RequiredFieldAttribute requiredField = attribute as RequiredFieldAttribute;

            if (property.objectReferenceValue == null)
            {
                GUI.backgroundColor = new Color32(requiredField.R, requiredField.G, requiredField.B, requiredField.A);
                EditorGUI.PropertyField(position, property, label);
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }

            EditorGUI.EndProperty();
        }

    } // class end
}

#endif