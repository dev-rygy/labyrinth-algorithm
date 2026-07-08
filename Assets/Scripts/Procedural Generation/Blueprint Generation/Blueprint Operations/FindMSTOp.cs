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
    public class FindMSTOp : BlueprintOperation
    {
        public FindMSTOp(MapGenerationContext context, BlueprintGenerator bpg, string edgeListInput)
                : base(context, bpg)
        {
            OperationID = $"FindMST:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(edgeListInput);

            // Output Ports
            string memoryID = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID);
            if (_debugLogs) Debug.Log($"[MapGenerator][BlueprintOperation] FindMSTOp: List<Edge> space allocated for memory with ID {memoryID}");
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out List<Edge> edgeList))
                return false;

            if (edgeList is null)
            {
                LogNullError();
                return false;
            }

            List<Edge> mst = FindMinimumSpanningTree(edgeList);

            _context.Malloc(OutputPorts[0], mst);
            _context.AddToMSTList(mst);

            return true;
        }
        public List<Edge> FindMinimumSpanningTree(List<Edge> edges)
        {
            Vertex startingVertex = edges[0].U;

            return PrimsAlgorithm.MinimumSpanningTree(edges, startingVertex);
        }
    }
}
