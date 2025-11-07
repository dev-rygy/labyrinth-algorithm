using RyansLibrary.Graphs;
using RyansLibrary.Labyrinth;
using RyansLibrary.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ListDifferenceOp : BlueprintOperation
{
    public ListDifferenceOp(MapGenerationContext context, BlueprintGenerator bpg, string listAInput, string listBInput, string listType)
            : base(context, bpg)
    {
        OperationID = $"ListDifferenceOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(listAInput);
        InputPorts.Add(listBInput);
        InputPorts.Add(listType);

        // Output Ports
        string memoryID = context.ConsumeMemoryID().ToString();
        OutputPorts.Add(memoryID);
        Debug.Log($"List<Edge> space allocated for memory with ID {memoryID}");
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
                List<Edge> resultEdgeList = TakeListDifference(edgeListA, edgeListB);
                _context.Set(OutputPorts[0], resultEdgeList);
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
                List<Blueprint> resultBlueprintList = TakeListDifference(blueprintListA, blueprintListB);
                _context.Set(OutputPorts[0], resultBlueprintList);
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

    private List<T> TakeListDifference<T>(List<T> listA, List<T> listB)
    {
        return ListUtils.Difference(listA, listB);
    }
}
