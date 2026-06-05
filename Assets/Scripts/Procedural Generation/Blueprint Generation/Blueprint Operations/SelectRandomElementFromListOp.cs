/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   11/10/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.Labyrinth;
using RyansLibrary.Utils;
using RyansLibrary.Graphs;
using System.Collections.Generic;
using UnityEngine;

public class SelectRandomElementFromListOp : BlueprintOperation
{
    public SelectRandomElementFromListOp(MapGenerationContext context, BlueprintGenerator bpg, string listInput, string listType) : base(context, bpg)
    {
        OperationID = $"SelectRandomElementFromListOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(listInput);
        InputPorts.Add(listType);

        // Output Ports
        string memoryID = context.ConsumeMemoryID().ToString();
        OutputPorts.Add(memoryID);
        if (_debugLogs) Debug.Log($"List<Edge> space allocated for memory with ID {memoryID}");
    }


    public override bool Execute()
    {
        if (!TryGetInput(1, out string listType))
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

                Edge randomEdge = SelectRandomSetFromList(edgeList);
                _context.Malloc(OutputPorts[0], randomEdge);
                return true;
            case "Blueprint":
                if (!TryGetInput(0, out List<Blueprint> blueprintList))
                    return false;
                if (blueprintList is null)
                {
                    LogNullError();
                    return false;
                }

                Blueprint randomBlueprint = SelectRandomSetFromList(blueprintList);
                _context.Malloc(OutputPorts[0], randomBlueprint);
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

    private T SelectRandomSetFromList<T>(List<T> list)
    {
        return ListUtils.SelectRandomElement(list);
    }
}
