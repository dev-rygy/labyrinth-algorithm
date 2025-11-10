using RyansLibrary.Labyrinth;
using UnityEngine;

public class GetPathLengthOp : BlueprintOperation
{
    public GetPathLengthOp(MapGenerationContext context, BlueprintGenerator bpg, string pathInput) : base(context, bpg)
    {
        OperationID = $"GetPathLengthOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(pathInput);     // Path

        // Output Ports
        string memoryID = context.ConsumeMemoryID().ToString();
        OutputPorts.Add(memoryID);      // Path Length
        if (_debugLogs) Debug.Log($"Path space allocated for memory with ID ({memoryID})");
    }

    public override bool Execute()
    {
        if (!TryGetInput(0, out Path path))
            return false;

        _context.Set(OutputPorts[0], path.BlueprintCount());
        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
