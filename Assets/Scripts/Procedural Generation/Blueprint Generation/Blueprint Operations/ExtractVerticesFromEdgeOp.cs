/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.Utilities;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Unpacks a graph Edge (produced by triangulation/MST) into its two endpoint Vertex objects and their raw
    /// grid positions. Graph edges only carry abstract vertices, so this is the bridge that lets downstream
    /// operations (FindBlueprintFromPositionOp, PathfindingBlueprintOp) go from "an edge in the room graph" to
    /// "the two actual Blueprint rooms it connects."
    /// </summary>
    public class ExtractVerticesFromEdgeOp : BlueprintOperation
    {
        public ExtractVerticesFromEdgeOp(MapGenerationContext context, string edgeInput) : base(context)
        {
            OperationID = $"ExtractVerticesFromEdgeOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(edgeInput);      // Edge

            // Output Ports
            string memoryID1 = context.ConsumeMemoryID().ToString();
            string memoryID2 = context.ConsumeMemoryID().ToString();
            string memoryID3 = context.ConsumeMemoryID().ToString();
            string memoryID4 = context.ConsumeMemoryID().ToString();

            OutputPorts.Add(memoryID1);  // Vertex U
            OutputPorts.Add(memoryID2);  // Vertex V
            OutputPorts.Add(memoryID3);  // Vertex U Position
            OutputPorts.Add(memoryID4);  // Vertex V Position
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out Edge edge))
                return false;

            if (edge is null)       // '==' operator is overloaded for Edge class, so we can use 'is null' to check for null
            {
                LogNullError();
                return false;
            }

            Vertex u = edge.U;
            Vertex v = edge.V;

            Vector3Int uPos = new Vector3Int((int)u.Position.x, (int)u.Position.y, (int)u.Position.z);
            Vector3Int vPos = new Vector3Int((int)v.Position.x, (int)v.Position.y, (int)v.Position.z);

            _context.Set(OutputPorts[0], u);
            _context.Set(OutputPorts[1], v);
            _context.Set(OutputPorts[2], uPos);
            _context.Set(OutputPorts[3], vPos);

            return true;
        }
    }
}
