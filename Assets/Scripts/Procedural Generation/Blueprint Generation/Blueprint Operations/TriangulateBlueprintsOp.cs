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
        _bpg = bpg;
        _context = context;

        // Input Ports
        InputPorts.Add(blueprintListInput);

        // Output Ports
        OutputPorts.Add(context.ConsumeMemoryID().ToString());
    }

    public override bool Execute()
    {
        List<Blueprint> blueprintList = _context.GrabFromMemory(InputPorts[0]) as List<Blueprint>;
        List<Edge> edgeList = GenerateTriangulation(blueprintList);

        _context.AllocateOrModifyMem(OutputPorts[0], edgeList);
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
