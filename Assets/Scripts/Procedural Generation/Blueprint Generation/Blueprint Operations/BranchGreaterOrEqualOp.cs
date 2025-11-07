using RyansLibrary.Labyrinth;
using UnityEngine;

public class BranchGreaterOrEqualOp : BlueprintOperation
{
    public BranchGreaterOrEqualOp(MapGenerationContext context, BlueprintGenerator bpg, string targetOpIDInput, string intAInput, string intBInput) : base(context, bpg)
    {
        OperationID = $"BranchGreaterOrEqualOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(targetOpIDInput);        // Target for jump
        InputPorts.Add(intAInput);              // int input left
        InputPorts.Add(intBInput);              // int input right
    }

    public override bool Execute()
    {
        if (!_context.TryGet(InputPorts[0], out string targetOpID))
        {
            LogInputError(0, "string");
            return false;
        }
        if (!_context.TryGet(InputPorts[1], out int itemA))
        {
            LogInputError(1, "intA");
            return false;
        }
        if (!_context.TryGet(InputPorts[2], out int itemB))
        {
            LogInputError(2, "intB");
            return false;
        }

        if (itemA >= itemB)
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
