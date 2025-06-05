using UnityEditor;
using UnityEngine;
using RyansLibrary.Labyrinth;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    private bool isDebugging = true;

    private bool showLogs = false;
    private bool blueprintLogs = false;
    private bool roomGenLogs = false;
    private bool pathfindingLogs = false;

    private bool showGizmos = false;
    private bool blueprintGizmos = false;
    private bool triangulationGizmos = false;
    private bool boundGizmos = false;

    public override void OnInspectorGUI()
    {
        // Get the target script
        MapGenerator generator = (MapGenerator)target;

        // Default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();

        isDebugging = EditorGUILayout.Toggle("Toggle Debug", isDebugging);

        if (isDebugging)
        {
            if (GUILayout.Button("Toggle Logs"))
            {
                showLogs = !showLogs;
            }

            if (showLogs)
            {
                blueprintLogs = EditorGUILayout.Toggle("Blueprint Generator Logs", blueprintLogs);
                roomGenLogs = EditorGUILayout.Toggle("Room Generator Logs", roomGenLogs);
                pathfindingLogs = EditorGUILayout.Toggle("Pathfinding Logs", pathfindingLogs);
            }
            else
            {
                blueprintLogs = false;
                roomGenLogs = false;
                pathfindingLogs = false;
            }

            if (GUILayout.Button("Toggle Gizmos"))
            {
                showGizmos = !showGizmos;
            }

            if (showGizmos)
            {
                blueprintGizmos = EditorGUILayout.Toggle("Blueprint Generator Gizmos", blueprintGizmos);
                triangulationGizmos = EditorGUILayout.Toggle("Triangulation Gizmos", triangulationGizmos);
                boundGizmos = EditorGUILayout.Toggle("Bounds Gizmos", boundGizmos);
            }
        }
    }
}
