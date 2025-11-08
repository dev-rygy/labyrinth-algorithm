using RyansLibrary.Graphs;
using RyansLibrary.Labyrinth;
using RyansLibrary.Utils;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SelectRandomSetFromListOp : BlueprintOperation
{
    public SelectRandomSetFromListOp(MapGenerationContext context, BlueprintGenerator bpg, string listInput, string elementCountInput, string listType) : base(context, bpg)
    {
        OperationID = $"SelectRandomFromListOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(listInput);
        InputPorts.Add(elementCountInput);
        InputPorts.Add(listType);

        // Output Ports
        string memoryID = context.ConsumeMemoryID().ToString();
        OutputPorts.Add(memoryID);
        Debug.Log($"List<Edge> space allocated for memory with ID {memoryID}");
    }


    public override bool Execute()
    {
        if (!TryGetInput(1, out int elementCount))
            return false;
        if (!TryGetInput(2, out string listType))
            return false;

        switch (listType)
        {
            case "Edge":
                if (!TryGetInput(0, out List<Edge> edgeList))
                    return false;
                if (edgeList is null)
                {
                    LogNullError();
                    return false;
                }

                List<Edge> resultListEdge = SelectRandomSetFromList(edgeList, elementCount);
                _context.Set(OutputPorts[0], resultListEdge);
                _context.AddToRandomCyclesList(resultListEdge);
                return true;
            case "Blueprint":
                if (!TryGetInput(1, out List<Blueprint> blueprintList))
                    return false;
                if (blueprintList is null)
                {
                    LogNullError();
                    return false;
                }

                List<Blueprint> resultListBlueprint = SelectRandomSetFromList(blueprintList, elementCount);
                _context.Set(OutputPorts[0], resultListBlueprint);
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

    private List<T> SelectRandomSetFromList<T>(List<T> list, int elementCount)
    {
        return ListUtils.SelectRandomSet(list, elementCount);
    }
}
