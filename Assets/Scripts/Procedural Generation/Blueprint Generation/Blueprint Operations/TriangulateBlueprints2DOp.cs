/*
 * Created By:      Ryan Carpenter
 * Date Created:    07/14/2026
 * Last Modified:   07/14/2026 (Ryan)
 * Notes:           
*/
using RyansLibrary;
using RyansLibrary.Graphs;
using RyansLibrary.Labyrinth;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class TriangulateBlueprints2DOp : BlueprintOperation
    {
        public TriangulateBlueprints2DOp(MapGenerationContext context, string blueprintListInput)
                    : base(context)
        {
            OperationID = $"TriangulateBlueprints:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(blueprintListInput);

            // Output Ports
            string memoryID = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID);
            if (_debugLogs) Debug.Log($"[MapGenerator][BlueprintOperation] TriangulateBlueprintsOp: List<Edge> space allocated for memory with ID {memoryID}");
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out List<Blueprint> blueprintList))
                return false;

            if (blueprintList is null)
            {
                LogNullError();
                return false;
            }

            List<Edge> edgeList = GenerateTriangulation(blueprintList);

            _context.Malloc(OutputPorts[0], edgeList);
            _context.AddToTriangulationsList(edgeList);

            return true;
        }

        public List<Edge> GenerateTriangulation(List<Blueprint> blueprintList)
        {
            List<Vertex> waypoints = new List<Vertex>();

            foreach (Blueprint blueprint in blueprintList)
            {
                waypoints.Add(new Vertex<Blueprint>(blueprint.Position, blueprint));
            }

            // TODO: DelaunayTriangulation has issues solving cases with coplanar tetrahedra; instead consider
            // a C# library called MIConvexHull that has thousands of lines to solve these issues.
            DelaunayTriangulation2D triangulation = DelaunayTriangulation2D.Triangulate(waypoints);

            return triangulation.Edges;
        }
    }
}
