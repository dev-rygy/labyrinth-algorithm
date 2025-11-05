using RyansLibrary.Labyrinth;
using UnityEngine;

public class JumpOp : BlueprintOperation
{
    public JumpOp(MapGenerationContext context, BlueprintGenerator bpg, string operatorIDInput)
            : base(context, bpg)
    {
        OperationID = $"JumpOp:{context.ConsumeOperationID()}";
        _bpg = bpg;
        _context = context;

        // Input Ports
        InputPorts.Add(operatorIDInput);
    }

    public override bool Execute()
    {
        string operatorID = (string)_context.GrabFromMemory(InputPorts[0]);

        Debug.Log($"Jumping to operation with ID {operatorID}");

        // Branch to specified id
        _context.Jump(operatorID);

        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
