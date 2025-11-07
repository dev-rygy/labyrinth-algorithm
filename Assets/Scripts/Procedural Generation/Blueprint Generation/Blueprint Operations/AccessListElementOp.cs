using RyansLibrary.Labyrinth;
using RyansLibrary.Graphs;
using System.Collections.Generic;
using UnityEngine;

public class AccessListElementOp : BlueprintOperation
{
    public AccessListElementOp(MapGenerationContext context, BlueprintGenerator bpg, string indexInput, string listInput, string listType) : base(context, bpg)
    {
        OperationID = $"AccessListElementOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(indexInput);     // Index of element
        InputPorts.Add(listInput);      // The list itself
        InputPorts.Add(listType);       // The list's type

        // Output Ports
        string memoryID = context.ConsumeMemoryID().ToString();
        OutputPorts.Add(memoryID);      // Element
        Debug.Log($"Object space allocated for memory with ID {memoryID}");
    }

    public override bool Execute()
    {
        if (!_context.TryGet(InputPorts[0], out int index))
        {
            LogInputError(0);
            return false;
        }
        if (!_context.TryGet(InputPorts[2], out string listType))
        {
            LogInputError(2);
            return false;
        }

        switch (listType)
        {
            case "Edge":
                if (!_context.TryGet(InputPorts[1], out List<Edge> edgeList))
                {
                    LogInputError(1);
                    return false;
                }
                if (edgeList is null)
                {
                    LogNullError();
                    return false;
                }
                Edge edgeElement = edgeList[index];
                _context.Set(OutputPorts[0], edgeElement);
                return true;
            case "Blueprint":
                if (!_context.TryGet(InputPorts[1], out List<Blueprint> blueprintList))
                {
                    LogInputError(1);
                    return false;
                }
                if (blueprintList is null)
                {
                    LogNullError();
                    return false;
                }
                Blueprint blueprintElement = blueprintList[index];
                _context.Set(OutputPorts[0], blueprintElement);
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
}
