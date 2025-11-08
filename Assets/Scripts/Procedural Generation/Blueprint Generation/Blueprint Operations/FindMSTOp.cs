using RyansLibrary.Graphs;
using RyansLibrary.Labyrinth;
using System.Collections.Generic;
using UnityEngine;

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
        if (_debugLogs) Debug.Log($"List<Edge> space allocated for memory with ID {memoryID}");
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

        _context.Set(OutputPorts[0], mst);
        _context.AddToMSTList(mst);

        return true;
    }

    public override bool Undo()
    {
        return false;
    }

    public List<Edge> FindMinimumSpanningTree(List<Edge> edges)
    {
        Vertex startingVertex = edges[0].U;

        return PrimsAlgorithm.MinimumSpanningTree(edges, startingVertex);
    }
}
