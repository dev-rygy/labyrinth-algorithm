using RyansLibrary.Labyrinth;
using UnityEngine;

public class AddIntOp : BlueprintOperation
{
    public AddIntOp(MapGenerationContext context, BlueprintGenerator bpg, string intAInput, string intBInput, string dataID = "")
            : base(context, bpg)
    {
        OperationID = $"AddIntOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(intAInput);
        InputPorts.Add(intBInput);
        InputPorts.Add(dataID);

        if (dataID != "")
            OutputPorts.Add(dataID);
        else
            OutputPorts.Add(context.ConsumeMemoryID().ToString());
    }

    public override bool Execute()
    {
        int intA = (int)_context.GrabFromMemory(InputPorts[0]);
        int intB = (int)_context.GrabFromMemory(InputPorts[1]);

        int sum = intA + intB;
        
        _context.AllocateOrModifyMem(OutputPorts[0], sum);

        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
