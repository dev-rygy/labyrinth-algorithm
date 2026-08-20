/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   07/13/2026 (Ryan)
 * Notes:           
*/
using RyansLibrary.Labyrinth;
using RyansLibrary.Utilities;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Picks a single random element out of a Edge or Blueprint List and writes just that element out.
/// Compare to ListSelectRandomSetFromOp, which picks several elements at once.
/// </summary>
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
                _context.Set(OutputPorts[0], resultListEdge);
                return true;
            }
            else if (list is List<Blueprint> blueprintList)
            {
                Blueprint resultListBlueprint = SelectRandomElementFromList(blueprintList);
                _context.Set(OutputPorts[0], resultListBlueprint);
                return true;
            }
            else
            {
                Debug.LogError($"ListSelectRandomSetFromOp: Invalid input types for Blueprint select random operation. " +
                    $"Types can only be List<Edge> or List<Blueprint>.");
                return false;
            }
    }

    private T SelectRandomElementFromList<T>(List<T> list)
    {
        return ListUtils.SelectRandomElement(list);
    }
}
