using RyansLibrary.Labyrinth;
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
        OutputPorts.Add(context.ConsumeMemoryID().ToString());
    }

    public override bool Execute()
    {
        if (!_context.TryGet(InputPorts[0], out Path path))
        {
            LogInputError(0);
            return false;
        }
        if (!_context.TryGet(InputPorts[1], out bool availbility))
        {
            LogInputError(1);
            return false;
        }

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
