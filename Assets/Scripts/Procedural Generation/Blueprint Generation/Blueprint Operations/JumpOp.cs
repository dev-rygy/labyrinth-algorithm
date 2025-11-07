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
        if (!_context.TryGet(InputPorts[0], out string targetOpID))
        {
            LogInputError(0);
            return false;
        }

        // Branch to specified id
        _context.Jump(targetOpID);

        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
