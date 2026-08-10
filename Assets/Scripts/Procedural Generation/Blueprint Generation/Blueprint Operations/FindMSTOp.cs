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
    /// <summary>
    /// Operation that finds the Minimum Spanning Tree via Prim's algorithm (RyansLibrary.Graphs.PrimsAlgorithm)
    /// Uses a 3D graph's edges as input.
    /// </summary>
    public class FindMSTOp : BlueprintOperation
    {
        public FindMSTOp(MapGenerationContext context, string edgeListInput)
                : base(context)
        {
            OperationID = $"FindMST:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(edgeListInput);

            // Output Ports
            string memoryID = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID);
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out List<Edge> edgeList))
                return false;

            if (edgeList == null)
            {
                LogNullError();
                return false;
            }

            List<Edge> mst = FindMinimumSpanningTree(edgeList);

            _context.Set(OutputPorts[0], mst);
            _context.InvokeNewMST(mst);

            return true;
        }
        public List<Edge> FindMinimumSpanningTree(List<Edge> edges)
        {
            Vertex startingVertex = edges[0].U;

            return PrimsAlgorithm.MinimumSpanningTree(edges, startingVertex);
        }
    }
}
