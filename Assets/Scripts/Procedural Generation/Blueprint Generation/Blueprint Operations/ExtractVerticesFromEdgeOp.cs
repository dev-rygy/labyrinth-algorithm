using RyansLibrary.Graphs;
using RyansLibrary.Labyrinth;
using System;
using UnityEngine;

public class ExtractVerticesFromEdgeOp : BlueprintOperation
{
    public ExtractVerticesFromEdgeOp(MapGenerationContext context, BlueprintGenerator bpg, string edgeInput) : base(context, bpg)
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

        if (_debugLogs) Debug.Log($"Vertex space allocated for memory with ID {memoryID1}");
        if (_debugLogs) Debug.Log($"Vertex space allocated for memory with ID {memoryID2}");
        if (_debugLogs) Debug.Log($"Vector3Int space allocated for memory with ID {memoryID3}");
        if (_debugLogs) Debug.Log($"Vector3Int space allocated for memory with ID {memoryID4}");
    }

    public override bool Execute()
    {
        if (!TryGetInput(0, out Edge edge))
            return false;

        if (edge is null)
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

    public override bool Undo()
    {
        return false;
    }
}
