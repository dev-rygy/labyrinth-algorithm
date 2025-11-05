using RyansLibrary.Labyrinth;
using UnityEngine;

public class BranchEqualOp : BlueprintOperation
{
    public BranchEqualOp(MapGenerationContext context, BlueprintGenerator bpg, string operatorIDInput, string intAInput, string intBInput)
            : base(context, bpg)
    {
        OperationID = $"BranchEqualOp:{context.ConsumeOperationID()}";
        _bpg = bpg;
        _context = context;

        // Input Ports
        InputPorts.Add(operatorIDInput);
        InputPorts.Add(intAInput);
        InputPorts.Add(intBInput);
    }

    public override bool Execute()
    {
        string operatorID = (string)_context.GrabFromMemory(InputPorts[0]);
        int itemA = (int)_context.GrabFromMemory(InputPorts[1]);
        int itemB = (int)_context.GrabFromMemory(InputPorts[2]);

        if (itemA == itemB)
        {
            // Branch to specified id
            _context.Jump(operatorID);
        }

        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
