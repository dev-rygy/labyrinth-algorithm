using RyansLibrary.Labyrinth;
using UnityEngine;

public class ConsolePrintOp : BlueprintOperation
{
    public ConsolePrintOp(MapGenerationContext context, BlueprintGenerator bpg, string messageInput) : base(context, bpg)
    {
        OperationID = $"PrintOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(messageInput);
    }

    public override bool Execute()
    {
        if (!TryGetInput(0, out string msg))
            return false;

        Debug.Log($"Map Generator Print: {msg}");

        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
