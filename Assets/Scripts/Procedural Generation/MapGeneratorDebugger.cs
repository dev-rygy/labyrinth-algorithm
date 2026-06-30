/*
 * Created By:      Ryan Carpenter
 * Date Created:    06/30/2026
 * Last Modified:   06/30/2026 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using UnityEngine;
using RyansLibrary.Graphs;

namespace RyansLibrary.Labyrinth
{
    [RequireComponent(typeof(MapGeneratorController))]
    public class MapGeneratorDebugger : MonoBehaviour
    {
        [Header("Debugging")]
        [Space]
        [SerializeField] private Color _boundingBoxColor;
        [SerializeField] private Color _triangulationColor;
        // [SerializeField] private Color _circumcircleColor;   DEPRICATED
        [SerializeField] private Color _contiguousGraphColor;
        [SerializeField] private Color _randomCyclesColor;
        [SerializeField] private Color _currentEdgeColor;

        // Editor Gizmos
        [SerializeField] private bool _debugGizmos = false;
        [SerializeField] private bool _debugBlueprintGizmos = false;
        [SerializeField] private bool _debugTriangulationGizmos = false;
        [SerializeField] private bool _debugBoundsGizmos = false;

        private MapGeneratorController _controller;

        // Lets the single set of Draw* methods below target either UnityEngine.Gizmos
        // (Scene view, edit mode - via OnDrawGizmos) or RuntimeGizmos (play mode + Player
        // builds - via Update), without duplicating the geometry logic for each.
        private readonly IGizmoDrawer _editorDrawer = new EditorGizmoDrawer();
        private readonly IGizmoDrawer _runtimeDrawer = new RuntimeGizmoDrawer();

        private void Awake()
        {
            _controller = GetComponent<MapGeneratorController>();
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

        private void OnDrawGizmos()
        {
            if (!_debugGizmos || _controller == null)
                return;

            DrawAll(_editorDrawer);
        }

        #region Gizmos
        private void DrawAll(IGizmoDrawer drawer)
        {
            foreach (Zone zone in _controller.Zones)
            {
                if (_debugBlueprintGizmos)
                    DrawBluePrintGizmos(zone, drawer);

                if (_debugTriangulationGizmos)
                {
                    DrawTriangulation(drawer);
                    DrawMSTs(drawer);
                    DrawRandomCycles(drawer);
                }

                if (_debugBoundsGizmos)
                    DrawBoundingBox(zone.Bounds, drawer);
            }

            foreach (ZoneConnectionEntry entry in _controller.ZoneConnections)
            {
                if (_debugBlueprintGizmos)
                    DrawBluePrintGizmos(entry.ConnectionZone, drawer);

                if (_debugBoundsGizmos)
                    DrawBoundingBox(entry.ConnectionZone.Bounds, drawer);
            }
        }

        private void DrawBoundingBox(BoundsInt bounds, IGizmoDrawer drawer)
        {
            int gridUnitSize = _controller.GridUnitSize;
            Vector3 boundsSize = gridUnitSize * bounds.size;
            Vector3 boundsCenter = (bounds.center + new Vector3(-0.5f, -0.5f, -0.5f)) * gridUnitSize;

            drawer.SetColor(_boundingBoxColor);
            drawer.WireCube(boundsCenter, boundsSize);
        }

        private void DrawTriangulation(IGizmoDrawer drawer)
        {
            if (_controller.Context is null || _controller.Context.Triangulations is null)
                return;

            int gridUnitSize = _controller.GridUnitSize;
            foreach (List<Edge> edgeList in _controller.Context.Triangulations)
            {
                // Draw remaining edges from triangulation
                foreach (Edge e in edgeList)
                {
                    drawer.SetColor(_triangulationColor);
                    drawer.Line(e.V.Position * gridUnitSize, e.U.Position * gridUnitSize);
                }
            }
        }

        private void DrawMSTs(IGizmoDrawer drawer)
        {
            if (_controller.Context is null || _controller.Context.MinimumSpanningTrees is null)
                return;

            int gridUnitSize = _controller.GridUnitSize;
            foreach (List<Edge> edgeList in _controller.Context.MinimumSpanningTrees)
            {
                if (edgeList is null)
                    continue;

                // Draw the minimum spanning tree of the zone
                foreach (Edge e in edgeList)
                {
                    drawer.SetColor(_contiguousGraphColor);
                    drawer.Line(e.V.Position * gridUnitSize, e.U.Position * gridUnitSize);
                }
            }
        }

        private void DrawRandomCycles(IGizmoDrawer drawer)
        {
            if (_controller.Context is null || _controller.Context.RandomCycles is null)
                return;

            int gridUnitSize = _controller.GridUnitSize;
            foreach (List<Edge> edgeList in _controller.Context.RandomCycles)
            {
                if (edgeList is null)
                    continue;

                foreach (Edge e in edgeList)
                {
                    drawer.SetColor(_randomCyclesColor);
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
    }
}
