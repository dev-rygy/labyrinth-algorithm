using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(DebugButtonAttribute))]
    public class DebugButtonDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight + (EditorGUIUtility.standardVerticalSpacing * 2);
        }
        
        public override void OnGUI(Rect position)
        {
            DebugButtonAttribute buttonAttribute = attribute as DebugButtonAttribute;

            // deactivate button if not in playmode as send message only works in playmode
            if (!EditorApplication.isPlaying)
            {
                GUI.enabled = false;
            }
            
            // draw button ...
            Rect buttonPosition = new Rect(position.x, position.y, position.width, position.height - EditorGUIUtility.standardVerticalSpacing);
            if (GUI.Button(buttonPosition, new GUIContent(buttonAttribute.buttonLabel, buttonAttribute.tooltip)))
            {
                ExecuteButton(buttonAttribute.methodName);
            }

            GUI.enabled = true;
        }

        private void ExecuteButton(string methodName)
        {
            Selection.activeGameObject.SendMessage(methodName);
        }
        
    } // class end
}
