/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/18/2024
 * Last Modified:   09/18/2025 (Ryan)
 * Notes:           Map Generator Debugging Editor
*/
using UnityEditor;
using UnityEngine;
using System;
using TMPro;

namespace RyansLibrary.Labyrinth
{
    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorEditor : Editor
    {
        public event Action OnGenerationRestart;

        private bool isDebugging = true;

        // Logs
        private bool showLogs = false;
        private bool showBlueprintLogs = false;
        private bool showRoomGenLogs = false;

        // Gizmos
        private bool showGizmos = false;
        private bool showBlueprintGizmos = false;
        private bool showTriangulationGizmos = false;
        private bool showBoundGizmos = false;

        private bool generationStarted = false;

        public override void OnInspectorGUI()
        {
            // Get the target script
            MapGenerator generator = (MapGenerator)target;

            if (generator == null)
                return;

            // Default inspector
            DrawDefaultInspector();

            EditorGUILayout.Space();

            isDebugging = EditorGUILayout.BeginToggleGroup("Toggle Debug", isDebugging);

            // Logs
            if (GUILayout.Button("Toggle Logs"))
            {
                showLogs = !showLogs;
            }

            if (showLogs)
            {
                showBlueprintLogs = EditorGUILayout.Toggle("Blueprint Generator Logs", showBlueprintLogs);
                showRoomGenLogs = EditorGUILayout.Toggle("Room Generator Logs", showRoomGenLogs);
            }
            else
            {
                showBlueprintLogs = false;
                showRoomGenLogs = false;
            }

            // Communicate with Map Generator script
            generator.ToggleBlueprintLogs(showBlueprintLogs);
            generator.ToggleRoomGeneratorLogs(showRoomGenLogs);

            // Gizmos
            if (GUILayout.Button("Toggle Gizmos"))
            {
                showGizmos = !showGizmos;
                generator.ToggleGizmos(showGizmos);
            }

            if (showGizmos)
            {
                showBlueprintGizmos = EditorGUILayout.Toggle("Blueprint Generator Gizmos", showBlueprintGizmos);
                showTriangulationGizmos = EditorGUILayout.Toggle("Triangulation Gizmos", showTriangulationGizmos);
                showBoundGizmos = EditorGUILayout.Toggle("Bounds Gizmos", showBoundGizmos);
            }
            else
            {
                showBlueprintGizmos = false;
                showTriangulationGizmos = false;
                showBoundGizmos = false;
            }

            // Communicate with Map Generator script
            generator.ToggleBlueprintGizmos(showBlueprintGizmos);
            generator.ToggleTriangulationGizmos(showTriangulationGizmos);
            generator.ToggleBoundsGizmos(showBoundGizmos);

            if (GUILayout.Button("Begin/Restart Generation"))
            {
                // Reset Map Generation
                OnGenerationRestart?.Invoke();
            }

            if (generationStarted)
            {
                if (GUILayout.Button("Step Generation"))
                {
                    // TODO: Step Through Map Generation
                }
            }

            EditorGUILayout.EndToggleGroup();
        }
    }
}
