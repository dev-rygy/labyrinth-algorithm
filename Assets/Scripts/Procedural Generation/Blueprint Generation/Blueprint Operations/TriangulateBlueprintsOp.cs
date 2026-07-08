/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.Graphs;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class TriangulateBlueprintsOp : BlueprintOperation
    {
        public TriangulateBlueprintsOp(MapGenerationContext context, BlueprintGenerator bpg, string blueprintListInput)
                : base(context, bpg)
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

            DelaunayTriangulation3D triangulation = DelaunayTriangulation3D.Triangulate(waypoints);

            return triangulation.Edges;
        }
    }
}
