using RyansLibrary.Labyrinth;
using UnityEngine;

public class BranchNotEqualOp : BlueprintOperation
{
    public BranchNotEqualOp(MapGenerationContext context, BlueprintGenerator bpg, string targetOpIDInput, string intAInput, string intBInput) : base(context, bpg)
    {
        OperationID = $"BranchNotEqualOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(targetOpIDInput);
        InputPorts.Add(intAInput);
        InputPorts.Add(intBInput);
    }

    public override bool Execute()
    {
        if (!_context.TryGet(InputPorts[0], out string targetOpID))
        {
            LogInputError(0);
            return false;
        }
        if (!_context.TryGet(InputPorts[1], out int itemA))
        {
            LogInputError(1);
            return false;
        }
        if (!_context.TryGet(InputPorts[2], out int itemB))
        {
            LogInputError(2);
            return false;
        }

        if (itemA != itemB)
        {
            // Branch to specified id
            _context.Jump(targetOpID);
        }

        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
