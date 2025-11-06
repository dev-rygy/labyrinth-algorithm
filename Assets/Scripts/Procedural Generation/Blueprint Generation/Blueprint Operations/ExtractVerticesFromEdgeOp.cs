using RyansLibrary.Labyrinth;
using RyansLibrary.Graphs;
using UnityEngine;

public class ExtractVerticesFromEdgeOp : BlueprintOperation
{
    public ExtractVerticesFromEdgeOp(MapGenerationContext context, BlueprintGenerator bpg, string edgeInput) : base(context, bpg)
    {
        OperationID = $"ExtractVerticesFromEdgeOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(edgeInput);      // Edge

        // Output Ports
        OutputPorts.Add(context.ConsumeMemoryID().ToString());  // Vertex U
        OutputPorts.Add(context.ConsumeMemoryID().ToString());  // Vertex V
        OutputPorts.Add(context.ConsumeMemoryID().ToString());  // Vertex U Position
        OutputPorts.Add(context.ConsumeMemoryID().ToString());  // Vertex V Position
    }

    public override bool Execute()
    {
        Edge edge = _context.GrabFromMemory(InputPorts[0]) as Edge;

        Vertex u = edge.U;
        Vertex v = edge.V;

        Vector3Int uPos = new Vector3Int((int)u.Position.x, (int)u.Position.y, (int)u.Position.z);
        Vector3Int vPos = new Vector3Int((int)v.Position.x, (int)v.Position.y, (int)v.Position.z);

        _context.AllocateOrModifyMem(OutputPorts[0], u);
        _context.AllocateOrModifyMem(OutputPorts[1], v);
        _context.AllocateOrModifyMem(OutputPorts[2], uPos);
        _context.AllocateOrModifyMem(OutputPorts[3], vPos);

        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
