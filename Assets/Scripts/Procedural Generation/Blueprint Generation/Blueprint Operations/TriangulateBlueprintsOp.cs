using RyansLibrary;
using RyansLibrary.Graphs;
using RyansLibrary.Labyrinth;
using System.Collections.Generic;
using UnityEngine;

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
        if (_debugLogs) Debug.Log($"List<Edge> space allocated for memory with ID {memoryID}");
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

        _context.Set(OutputPorts[0], edgeList);
        _context.AddToTriangulationsList(edgeList);

        return true;
    }

    public override bool Undo()
    {
        return false;
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
