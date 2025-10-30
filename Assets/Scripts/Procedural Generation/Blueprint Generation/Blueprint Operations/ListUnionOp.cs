using RyansLibrary.Labyrinth;
using RyansLibrary.Utils;
using RyansLibrary.Graphs;
using System.Collections.Generic;
using UnityEngine;

public class ListUnionOp : BlueprintOperation
{
    public ListUnionOp(MapGenerationContext context, BlueprintGenerator bpg, string listAInput, string listBInput, string listType)
            : base(context, bpg)
    {
        OperationID = $"ListUnionOp:{context.ConsumeOperationID()}";
        _bpg = bpg;
        _context = context;

        // Input Ports
        InputPorts.Add(listAInput);
        InputPorts.Add(listBInput);
        InputPorts.Add(listType);

        // Output Ports
        OutputPorts.Add(context.ConsumeMemoryID().ToString());
    }


    public override bool Execute()
    {
        string listType = (string)_context.GrabFromMemory(InputPorts[2]);

        switch (listType)
        {
            case "Edge":
                List<Edge> listAEdge = _context.GrabFromMemory(InputPorts[0]) as List<Edge>;
                List<Edge> listBEdge = _context.GrabFromMemory(InputPorts[1]) as List<Edge>;
                List<Edge> resultListEdge = TakeListUnion(listAEdge, listBEdge);
                _context.AllocateOrModifyMem(OutputPorts[0], resultListEdge);
                return true;
            case "Blueprint":
                List<Blueprint> listABlueprint = _context.GrabFromMemory(InputPorts[0]) as List<Blueprint>;
                List<Blueprint> listBBlueprint = _context.GrabFromMemory(InputPorts[1]) as List<Blueprint>;
                List<Blueprint> resultListBlueprint = TakeListUnion(listABlueprint, listBBlueprint);
                _context.AllocateOrModifyMem(OutputPorts[0], resultListBlueprint);
                return true;
            default:
                Debug.LogError("Blueprint Operator Error: Invalid Type for Union Operator.");
                return false;
        }
    }

    public override bool Undo()
    {
        return false;
    }

    private List<T> TakeListUnion<T>(List<T> listA, List<T> listB)
    {
        return ListUtils.Union(listA, listB);
    }
}
