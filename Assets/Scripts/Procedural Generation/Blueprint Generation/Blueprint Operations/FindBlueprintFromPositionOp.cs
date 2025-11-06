using RyansLibrary.Labyrinth;
using RyansLibrary.Graphs;
using UnityEngine;

public class FindBlueprintFromPositionOp : BlueprintOperation
{
    public FindBlueprintFromPositionOp(MapGenerationContext context, BlueprintGenerator bpg, string positionInput) : base(context, bpg)
    {
        OperationID = $"FindBlueprintFromVertexPositionOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(positionInput);

        // Output Ports
        OutputPorts.Add(context.ConsumeMemoryID().ToString());      // Blueprint
        OutputPorts.Add(context.ConsumeMemoryID().ToString());      // bool found
    }

    public override bool Execute()
    {
        Vector3Int position = (Vector3Int)_context.GrabFromMemory(InputPorts[0]);

        bool result = _bpg.GetMasterDictionary().TryGetValue(position, out Blueprint blueprint);

        _context.AllocateOrModifyMem(OutputPorts[0], blueprint);
        _context.AllocateOrModifyMem(OutputPorts[1], result);

        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
