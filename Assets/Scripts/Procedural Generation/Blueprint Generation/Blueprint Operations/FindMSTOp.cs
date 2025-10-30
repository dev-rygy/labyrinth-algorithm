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
        _bpg = bpg;
        _context = context;

        // Input Ports
        InputPorts.Add(edgeListInput);

        // Output Ports
        OutputPorts.Add(context.ConsumeMemoryID().ToString());
    }

    public override bool Execute()
    {
        List<Edge> edgeList = _context.GrabFromMemory(InputPorts[0]) as List<Edge>;
        List<Edge> mst = FindMinimumSpanningTree(edgeList);

        _context.AllocateOrModifyMem(OutputPorts[0], mst);
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
