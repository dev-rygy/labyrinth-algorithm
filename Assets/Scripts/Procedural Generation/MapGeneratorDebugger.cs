/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/30/2026
 * Last Modified:   06/30/2026 (Ryan)
 * Notes:           
*/
using RyansLibrary.Console;
using RyansLibrary.Graphs;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Pure visualization layer - draws zone bounds, triangulations, MSTs, and the random re-added cycle edges as
    /// gizmos while a MapGeneratorController is generating. Has no influence on generation itself; it just listens
    /// to MapGenerationContext's debug events (OnNewTriangulation/OnNewMST/OnNewRandomCycles) to capture the
    /// intermediate graph state at each stage of MapGeneratorController.LoadMainPathConnectionsOperations for
    /// inspection.
    /// </summary>
    [RequireComponent(typeof(MapGeneratorController))]
    public class MapGeneratorDebugger : MonoBehaviour
    {
        [Header("Debugging")]
        [Space]
        [SerializeField] private Color _zoneBoundsColor;
        [SerializeField] private Color _zoneSpawnBoundsColor;
        [SerializeField] private Color _uniqueRoomBoundsColor;
        [SerializeField] private Color _triangulationColor;
        // [SerializeField] private Color _circumcircleColor;   DEPRICATED
        [SerializeField] private Color _contiguousGraphColor;
        [SerializeField] private Color _randomCyclesColor;
        [SerializeField] private Color _currentEdgeColor;

        // Editor Gizmos
        [SerializeField] private bool _debugGizmos = false;
        [SerializeField] private bool _debugBlueprintGizmos = true;
        [SerializeField] private bool _debugTriangulationGizmos = true;
        [SerializeField] private bool _debugBoundsGizmos = true;

        private MapGeneratorController _controller;

        // Lets the single set of Draw* methods below target either UnityEngine.Gizmos
        // (Scene view, edit mode - via OnDrawGizmos) or RuntimeGizmos (play mode + Player
        // builds - via Update), without duplicating the geometry logic for each.
        private readonly IGizmoDrawer _editorDrawer = new EditorGizmoDrawer();
        private readonly IGizmoDrawer _runtimeDrawer = new RuntimeGizmoDrawer();

        // Debugging Lists - These lists are intended to store intermediate results for debugging and visualization purposes
        public List<List<Edge>> Triangulations { get; private set; }
        public List<List<Edge>> MinimumSpanningTrees { get; private set; }
        public List<List<Edge>> RandomCycles { get; private set; }

        #region Mono
        private void Awake()
        {
            _controller = GetComponent<MapGeneratorController>();

            // Initialize Debugging Lists
            Triangulations = new();
            MinimumSpanningTrees = new();
            RandomCycles = new();
        }

        private void Start()
        {
            RegisterConsoleCommands();
        }

        private void OnEnable()
        {
            MapGeneratorController.OnGenerationStarted += () => _debugGizmos = true;
            MapGeneratorController.OnGenerationDone += () => _debugGizmos = false;
            ApplicationController.OnMenu += () => _debugGizmos = false;

            MapGenerationContext.OnNewTriangulation += (triangulation) => Triangulations.Add(triangulation);
            MapGenerationContext.OnNewMST += (mst) => MinimumSpanningTrees.Add(mst);
            MapGenerationContext.OnNewRandomCycles += (rndCycle) => RandomCycles.Add(rndCycle);

            MapGeneratorController.OnGenerationReset += ClearDebuggingLists;
        }

        private void OnDisable()
        {
            MapGeneratorController.OnGenerationStarted -= () => _debugGizmos = true;
            MapGeneratorController.OnGenerationDone -= () => _debugGizmos = false;
            ApplicationController.OnMenu -= () => _debugGizmos = false;

            MapGenerationContext.OnNewTriangulation -= (triangulation) => Triangulations.Add(triangulation);
            MapGenerationContext.OnNewMST -= (mst) => Triangulations.Add(mst);
            MapGenerationContext.OnNewRandomCycles -= (rndCycle) => Triangulations.Add(rndCycle);

            MapGeneratorController.OnGenerationReset -= ClearDebuggingLists;
        }

        private void Update()
        {
            // OnDrawGizmos already covers the edit-mode Scene view preview; this path exists
            // so the same visuals show up in Play mode and in actual Player builds, where
            // OnDrawGizmos is never called.
            if (!_debugGizmos || !Application.isPlaying || _controller == null)
                return;

            DrawAll(_runtimeDrawer);
        }

        public void ClearDebuggingLists()
        {
            // Clear debugging lists (clear what exists)
            Triangulations?.Clear();
            MinimumSpanningTrees?.Clear();
            RandomCycles?.Clear();
        }

        private void OnDrawGizmos()
        {
            _controller ??= GetComponent<MapGeneratorController>();

            if (!_debugGizmos || _controller == null)
                return;

            DrawAll(_editorDrawer);
        }
        #endregion

        #region Gizmos
        private void DrawAll(IGizmoDrawer drawer)
        {
            foreach (Zone zone in _controller.Zones)
            {
                // Draw zone blueprints
                if (_debugBlueprintGizmos)
                    DrawBluePrintGizmos(zone, drawer);

                // Draw triangulation blueprints
                if (_debugTriangulationGizmos)
                {
                    DrawTriangulation(drawer, _triangulationColor);
                    DrawMSTs(drawer, _contiguousGraphColor);
                    DrawRandomCycles(drawer, _randomCyclesColor);
                }

                if (_debugBoundsGizmos)
                {
                    // Draw zone bounds
                    DrawBoundingBox(zone.Bounds, Vector3.zero, _zoneBoundsColor, drawer);

                    // Draw unique room bounds
                    foreach (RoomEntry room in zone.UniqueRooms)
                    {
                        if (room == null)
                            continue;

                        if (room.PlacementType != RoomPlacementType.Constrained)
                            continue;

                        DrawBoundingBox(room.Bounds, zone.Bounds.position, _uniqueRoomBoundsColor, drawer);
                    }
                }
            }

            return;

            foreach (ZoneConnectionEntry entry in _controller.ZoneConnections)
            {
                // Draw zone connection blueprints
                if (_debugBlueprintGizmos)
                    DrawBluePrintGizmos(entry.ConnectionZone, drawer);

                // Draw zone connection bounds
                if (_debugBoundsGizmos)
                {
                    DrawBoundingBox(entry.ConnectionZone.Bounds, Vector3.zero, _zoneBoundsColor, drawer);
                    DrawBoundingBox(entry.ZoneSpawnBounds, Vector3.zero, _zoneSpawnBoundsColor, drawer);
                }
            }
        }

        private void DrawBoundingBox(BoundsInt bounds, Vector3 boundsOffset, Color color, IGizmoDrawer drawer)
        {
            int gridUnitSize = _controller.GridUnitSize;
            Vector3 boundsSize = gridUnitSize * bounds.size;
            Vector3 boundsCenter = (bounds.center + new Vector3(-0.5f, -0.5f, -0.5f) + boundsOffset) * gridUnitSize;

            drawer.SetColor(color);
            drawer.WireCube(boundsCenter, boundsSize);
        }

        private void DrawTriangulation(IGizmoDrawer drawer, Color color)
        {
            if (_controller.Context is null || Triangulations is null)
                return;

            int gridUnitSize = _controller.GridUnitSize;
            foreach (List<Edge> edgeList in Triangulations)
            {
                // Draw remaining edges from triangulation
                foreach (Edge e in edgeList)
                {
                    drawer.SetColor(color);
                    drawer.Line(e.V.Position * gridUnitSize, e.U.Position * gridUnitSize);
                }
            }
        }

        private void DrawMSTs(IGizmoDrawer drawer, Color color)
        {
            if (_controller.Context is null || MinimumSpanningTrees is null)
                return;

            int gridUnitSize = _controller.GridUnitSize;
            foreach (List<Edge> edgeList in MinimumSpanningTrees)
            {
                if (edgeList is null)
                    continue;

                // Draw the minimum spanning tree of the zone
                foreach (Edge e in edgeList)
                {
                    drawer.SetColor(color);
                    drawer.Line(e.V.Position * gridUnitSize, e.U.Position * gridUnitSize);
                }
            }
        }

        private void DrawRandomCycles(IGizmoDrawer drawer, Color color)
        {
            if (_controller.Context is null || RandomCycles is null)
                return;

            int gridUnitSize = _controller.GridUnitSize;
            foreach (List<Edge> edgeList in RandomCycles)
            {
                if (edgeList is null)
                    continue;

                foreach (Edge e in edgeList)
                {
                    drawer.SetColor(color);
                    drawer.Line(e.V.Position * gridUnitSize, e.U.Position * gridUnitSize);
                }
            }
        }

        private void DrawBluePrintGizmos(Zone zone, IGizmoDrawer drawer)
        {
            if (zone.MainPath.BlueprintList == null)
                return;

            int gridUnitSize = _controller.GridUnitSize;
            Vector3 unitSize = Vector3.one * gridUnitSize;

            // Draw Gizmos for main path
            foreach (Blueprint blueprint in zone.MainPath.BlueprintList)
            {
                drawer.SetColor(zone.MainPath.PathGizmoColor);
                drawer.Cube(blueprint.Position * gridUnitSize, unitSize);
            }

            foreach (Path path in zone.Paths)
            {
                if (path.BlueprintList == null)
                    return;

                // Draw Gizmos for alt paths
                foreach (Blueprint blueprint in path.BlueprintList)
                {
                    drawer.SetColor(path.PathGizmoColor);
                    drawer.Cube(blueprint.Position * gridUnitSize, unitSize);
                }
            }
        }
        #endregion

        #region Console Commands
        private void RegisterConsoleCommands()
        {
            ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.togglegizmos",
                "Toggles map generator gizmos. Enter 'true' for on and 'false' for off.",
                args =>
                {
                    if (args.Length < 1)
                    {
                        Debug.LogWarning("No argument given, please enter 'true' or 'false'.");
                        return;
                    }

                    if (args[0] == "true")
                    {
                        _debugGizmos = true;
                    }
                    else if (args[0] == "false")
                    {
                        _debugGizmos = false;
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid argument {args[0]}. Please input either 'true' or 'false'.");
                    }
                    Debug.Log($"Map Generator Gizmos set to {_debugGizmos}.");
                }
                ));

            ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.togglegizmos.blueprintgizmos",
                "Toggles blueprint gizmos. \"mapgenerator.togglegizmos\" must be enabled first.",
                args =>
                {
                    _debugBlueprintGizmos = !_debugBlueprintGizmos;
                    Debug.Log($"Blueprint Gizmos set to '{_debugBlueprintGizmos}'.");
                }));

            ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.togglegizmos.triangulationgizmos",
                "Toggles triangulation gizmos. \"mapgenerator.togglegizmos\" must be enabled first.",
                args =>
                {
                    _debugTriangulationGizmos = !_debugTriangulationGizmos;
                    Debug.Log($"Triangulation Gizmos set to '{_debugTriangulationGizmos}'.");
                }));

            ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.togglegizmos.boundsgizmos",
                "Toggles zone bound gizmos. \"mapgenerator.togglegizmos\" must be enabled first.",
                args =>
                {
                    _debugBoundsGizmos = !_debugBoundsGizmos;
                    Debug.Log($"Bounds Gizmos set to '{_debugBoundsGizmos}'.");
                }));

        }
        #endregion
    }
}
