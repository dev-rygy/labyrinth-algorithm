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

            // Coming soon label
            Label label = new Label("Coming Soon!");
            root.Add(label);
        }
    }
}
