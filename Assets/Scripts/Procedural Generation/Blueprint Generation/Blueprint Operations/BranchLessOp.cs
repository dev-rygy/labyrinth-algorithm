using RyansLibrary.Labyrinth;
using UnityEngine;

public class BranchLessOp : BlueprintOperation
{
    public BranchLessOp(MapGenerationContext context, BlueprintGenerator bpg, string targetOpIDInput, string intAInput, string intBInput) : base(context, bpg)
    {
        OperationID = $"BranchLessThanOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(targetOpIDInput);
        InputPorts.Add(intAInput);
        InputPorts.Add(intBInput);
    }

    public override bool Execute()
    {
        if (!TryGetInput(0, out string targetOpID))
            return false;
        if (!TryGetInput(1, out int itemA))
            return false;
        if (!TryGetInput(2, out int itemB))
            return false;

        if (itemA < itemB)
        {
            // Branch to specified id
            return _context.Jump(targetOpID);
        }

        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
