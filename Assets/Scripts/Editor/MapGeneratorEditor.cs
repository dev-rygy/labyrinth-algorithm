/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/18/2024
 * Last Modified:   09/18/2025 (Ryan)
 * Notes:           Map Generator Debugging Editor
*/
using UnityEditor;
using UnityEngine;
using System;
using Unity.Properties;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Edit's the Map Generator's Inspector Editor.
    /// Class has total dependancy on the Map Generator Script.
    /// </summary>
    [CustomEditor(typeof(MapGeneratorController))]
    public class MapGeneratorEditor : Editor
    {
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

        // Stepwise variables
        private bool showStepwiseFunctions = false;
        private bool toggleStepwiseDebugging = false;

        private MapGeneratorController generator;

        private void OnEnable()
        {
            // Get the target script
            generator = (MapGeneratorController)target;
        }

        public override void OnInspectorGUI()
        {
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

            if (GUILayout.Button("Toggle Stepwise Generation"))
            {
                showStepwiseFunctions = !showStepwiseFunctions;
                toggleStepwiseDebugging = !toggleStepwiseDebugging;
            }

            if (showStepwiseFunctions)
            {
                if (GUILayout.Button("Step Generation", GUILayout.ExpandWidth(false)))
                {
                    if (generator.IsGenerating)
                    {
                        // Step through map generation
                        generator.Advance(1);
                    }
                }

                if (GUILayout.Button("Step All", GUILayout.ExpandWidth(false)))
                {
                    if (generator.IsGenerating)
                    {
                        // Execute all operations at once
                        generator.AdvanceAll();
                    }
                }
            }

            if (GUILayout.Button("Restart Generation"))
            {
                generator.ResetLabyrinth();
            }

            generator.ToggleStepwiseDebugging(toggleStepwiseDebugging);

            EditorGUILayout.EndToggleGroup();
        }
    }
}
