#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(StringDropdownAttribute))]
    public class StringDropdownDrawer : PropertyDrawer
    {
        public override void OnGUI (Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            StringDropdownAttribute stringDropdown = attribute as StringDropdownAttribute;
            
            string[] list = stringDropdown.list;
            
            if (property.propertyType == SerializedPropertyType.String)
            {
                // get selected index
                int index = Mathf.Max(0, Array.IndexOf(list, property.stringValue));
                index = EditorGUI.Popup(position, property.displayName, index, list);

                // convert index to string value
                property.stringValue = list[index];
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