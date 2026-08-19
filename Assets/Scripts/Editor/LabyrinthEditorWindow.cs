using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RyansLibrary.UnityEditor
{
    public class LabyrinthEditorWindow : EditorWindow
    {
        [MenuItem("Window/Labyrinth Debugger")]
        public static void ShowWindow()
        {
            LabyrinthEditorWindow wnd = GetWindow<LabyrinthEditorWindow>();
            wnd.titleContent = new GUIContent("Labyrinth Debugger");
        }

        public void CreateGUI()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            // VisualElements objects can contain other VisualElement following a tree hierarchy
            Label label = new Label("Hello World!");
            root.Add(label);

            // Create button
            Button button = new Button();
            button.name = "button";
            button.text = "Button";
            root.Add(button);

            // Create toggle
            Toggle toggle = new Toggle();
            toggle.name = "toggle";
            toggle.label = "Toggle";
            root.Add(toggle);
        }
    }
}
