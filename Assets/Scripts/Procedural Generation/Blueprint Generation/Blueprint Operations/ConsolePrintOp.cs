using RyansLibrary.Labyrinth;
using UnityEngine;

public class ConsolePrintOp : BlueprintOperation
{
    public ConsolePrintOp(MapGenerationContext context, BlueprintGenerator bpg, string printInput) : base(context, bpg)
    {
        OperationID = $"PrintOp:{context.ConsumeOperationID()}";
        _bpg = bpg;
        _context = context;

        // Input Ports
        InputPorts.Add(printInput);
    }

    public override bool Execute()
    {
        object input = _context.GrabFromMemory(InputPorts[0]);

        Debug.Log(input);

        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
