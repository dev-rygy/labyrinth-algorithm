using RyansLibrary.Labyrinth;
using UnityEngine;

public class NoOp : BlueprintOperation
{
    public NoOp(MapGenerationContext context, BlueprintGenerator bpg) : base(context, bpg)
    {
        OperationID = $"NoOp:{context.ConsumeOperationID()}";
    }

    public override bool Execute()
    {
        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
