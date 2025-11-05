using RyansLibrary.Labyrinth;
using UnityEngine;

public class LoadDataOp : BlueprintOperation
{
    public LoadDataOp(MapGenerationContext context, BlueprintGenerator bpg, string idInput, string newData)
            : base(context, bpg)
    {
        OperationID = $"AddIntOp:{context.ConsumeOperationID()}";
        _bpg = bpg;
        _context = context;

        // Input Ports
        InputPorts.Add(idInput);
        InputPorts.Add(newData);
    }

    public override bool Execute()
    {
        object newData = _context.GrabFromMemory(InputPorts[1]);

        _context.ModifyMemory(InputPorts[0], newData);

        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
