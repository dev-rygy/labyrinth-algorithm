/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   07/13/2026 (Ryan)
 * Notes:           
*/
using RyansLibrary.Labyrinth;
using RyansLibrary.Graphs;
using RyansLibrary.Utils;
using System.Collections.Generic;
using UnityEngine;

public class ListSelectRandomElementOp : BlueprintOperation
{
    public ListSelectRandomElementOp(MapGenerationContext context, string listInput) : base(context)
    {
        OperationID = $"SelectRandomElementFromListOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(listInput);  // List

        // Output Ports
        string memoryID = context.ConsumeMemoryID().ToString();
        OutputPorts.Add(memoryID);  // Random element from the list

        if (_debugLogs) Debug.Log($"[MapGenerator][BlueprintOperation] ListSelectRandomElementOp: List<T> space allocated for memory with ID {memoryID}");
    }


    public override bool Execute()
    {
            if (!TryGetInput(0, out object list))
                return false;

            if (list is null)
            {
                LogNullError();
                return false;
            }

            if (list is List<Edge> edgeList)
            {
                Edge resultListEdge = SelectRandomElementFromList(edgeList);
                _context.Malloc(OutputPorts[0], resultListEdge);
                return true;
            }
            else if (list is List<Blueprint> blueprintList)
            {
                Blueprint resultListBlueprint = SelectRandomElementFromList(blueprintList);
                _context.Malloc(OutputPorts[0], resultListBlueprint);
                return true;
            }
            else
            {
                Debug.LogError($"[MapGenerator][BlueprintOperation] ListSelectRandomSetFromOp: Invalid input types for Blueprint select random operation. " +
                    $"Types can only be List<Edge> or List<Blueprint>.");
                return false;
            }
    }

    private T SelectRandomElementFromList<T>(List<T> list)
    {
        return ListUtils.SelectRandomElement(list);
    }
}
