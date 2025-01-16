using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(LabelOverrideAttribute))]
    public class LabelOverrideDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            LabelOverrideAttribute labelOverride = attribute as LabelOverrideAttribute;

            if (ForceArray(property) == false)
            {
                label.text = labelOverride.label;
            }
            EditorGUI.PropertyField(position, property, label);

            EditorGUI.EndProperty();
        }

        // ForceArray code from Jamora & vexe : https://answers.unity.com/questions/603882/serializedproperty-isnt-being-detected-as-an-array.html
        // Updated by Gaskellgames
        bool ForceArray(SerializedProperty property)
        {
            string path = property.propertyPath;
            int indexOfDot = path.IndexOf('.');
            if (indexOfDot == -1)
            {
                return false;
            }
            else
            {
                string propName = path.Substring(0, indexOfDot);
                SerializedProperty serializedProperty = property.serializedObject.FindProperty(propName);
                return serializedProperty.isArray;
            }
        }
        
    } // class end
}
