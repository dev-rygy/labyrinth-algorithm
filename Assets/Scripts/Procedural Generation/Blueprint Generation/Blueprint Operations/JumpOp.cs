using RyansLibrary.Labyrinth;
using UnityEngine;

public class JumpOp : BlueprintOperation
{
    public JumpOp(MapGenerationContext context, BlueprintGenerator bpg, string targetOpIDInput)
            : base(context, bpg)
    {
        OperationID = $"JumpOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(targetOpIDInput);
    }

    public override bool Execute()
    {
        if (!TryGetInput(0, out string targetOpID))
            return false;

        // Branch to specified id
        return _context.Jump(targetOpID);
    }

    public override bool Undo()
    {
        return false;
    }
}
