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
        if (!TryGetInput(0, out int index))
            return false;
        if (!TryGetInput(2, out string listType))
            return false;

        switch (listType)
        {
            case "Edge":
                if (!TryGetInput(1, out List<Edge> edgeList))
                    return false;
                if (edgeList is null)
                {
                    LogNullError();
                    return false;
                }

                Edge edgeElement = edgeList[index];
                _context.Set(OutputPorts[0], edgeElement);
                return true;
            case "Blueprint":
                if (!TryGetInput(1, out List<Blueprint> blueprintList))
                    return false;
                if (blueprintList is null)
                {
                    LogNullError();
                    return false;
                }

                Blueprint blueprintElement = blueprintList[index];
                _context.Set(OutputPorts[0], blueprintElement);
                return true;
            default:
                Debug.LogError($"Map Generator Error: Invalid input type 'T' for {OperationID}.");
                return false;
        }
    }

    public override bool Undo()
    {
        return false;
    }
}
