using RyansLibrary.Graphs;
using RyansLibrary.Labyrinth;
using RyansLibrary.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ListUnionOp : BlueprintOperation
{
    public ListUnionOp(MapGenerationContext context, BlueprintGenerator bpg, string listAInput, string listBInput, string listType)
            : base(context, bpg)
    {
        OperationID = $"ListUnionOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(listAInput);     // 1st list
        InputPorts.Add(listBInput);     // 2nd list
        InputPorts.Add(listType);       // list type

        // Output Ports
        string memoryID1 = context.ConsumeMemoryID().ToString();
        string memoryID2 = context.ConsumeMemoryID().ToString();
        OutputPorts.Add(memoryID1);      // List
        OutputPorts.Add(memoryID2);      // List count
        Debug.Log($"List<T> space allocated for memory with ID {memoryID1}");
        Debug.Log($"Int space allocated for memory with ID {memoryID2}");
    }


    public override bool Execute()
    {
        if (!_context.TryGet(InputPorts[2], out string listType))
        {
            LogInputError(2);
            return false;
        }

        switch (listType)
        {
            case "Edge":
                if (!_context.TryGet(InputPorts[0], out List<Edge> edgeListA))
                {
                    LogInputError(0);
                    return false;
                }
                if (!_context.TryGet(InputPorts[1], out List<Edge> edgeListB))
                {
                    LogInputError(1);
                    return false;
                }
                if (edgeListA is null || edgeListB is null)
                {
                    LogNullError();
                    return false;
                }
                List<Edge> resultEdgeList = TakeListUnion(edgeListA, edgeListB);
                _context.Set(OutputPorts[0], resultEdgeList);
                _context.Set(OutputPorts[1], resultEdgeList.Count);
                return true;
            case "Blueprint":
                if (!_context.TryGet(InputPorts[0], out List<Blueprint> blueprintListA))
                {
                    LogInputError(0);
                    return false;
                }
                if (!_context.TryGet(InputPorts[1], out List<Blueprint> blueprintListB))
                {
                    LogInputError(1);
                    return false;
                }
                if (blueprintListA is null || blueprintListB is null)
                {
                    LogNullError();
                    return false;
                }
                List<Blueprint> resultBlueprintList = TakeListUnion(blueprintListA, blueprintListB);
                _context.Set(OutputPorts[0], resultBlueprintList);
                _context.Set(OutputPorts[1], resultBlueprintList.Count);
                return true;
            default:
                Debug.LogError($"Map Generator Error: Invalid Type for {OperationID}.");
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
