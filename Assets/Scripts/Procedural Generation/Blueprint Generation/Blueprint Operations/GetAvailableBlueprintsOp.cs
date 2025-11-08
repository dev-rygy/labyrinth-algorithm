using RyansLibrary.Labyrinth;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GetAvailableBlueprintsOp : BlueprintOperation
{
    public GetAvailableBlueprintsOp(MapGenerationContext context, BlueprintGenerator bpg, string pathInput, string availableToggleInput)
            : base(context, bpg)
    {
        OperationID = $"GetAvailableBlueprintsOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(pathInput);
        InputPorts.Add(availableToggleInput);

        // Output Ports
        string memoryID = context.ConsumeMemoryID().ToString();
        OutputPorts.Add(memoryID);
        if(_debugLogs) Debug.Log($"BoundsInt space allocated for memory with ID {memoryID}");
    }

    public override bool Execute()
    {
        if (!TryGetInput(0, out Path path))
            return false;
        if (!TryGetInput(1, out bool availbility))
            return false;

        if (path is null)
        {
            LogNullError();
            return false;
        }

        List<Blueprint> availableBlueprintList = GetAvailableBlueprints(path.BlueprintList, availbility);

        _context.Set(OutputPorts[0], availableBlueprintList);

        return true;
    }

    public override bool Undo()
    {
        return false;
    }

    private List<Blueprint> GetAvailableBlueprints(List<Blueprint> list, bool availbility)
    {
        return list.Where(bp => (bp.Available == availbility)).ToList();
    }
}
