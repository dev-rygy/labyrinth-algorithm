/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   11/01/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.Labyrinth;
using UnityEngine;

/// <summary>
/// Operation that reads how many blueprint rooms currently exist in a Path and returns the count mainly for
/// downstream operations (e.g. bounds checks, loop conditions driven by BranchGreaterOrEqualOp) that can react
/// to path length without reaching into the Path object directly.
/// </summary>
public class GetPathLengthOp : BlueprintOperation
{
    public GetPathLengthOp(MapGenerationContext context, string pathInput) : base(context)
    {
        OperationID = $"GetPathLengthOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(pathInput);     // Path

        // Output Ports
        string memoryID = context.ConsumeMemoryID().ToString();
        OutputPorts.Add(memoryID);      // Path Length
    }

    public override bool Execute()
    {
        if (!TryGetInput(0, out Path path))
            return false;

        _context.Set(OutputPorts[0], path.BlueprintCount());
        return true;
    }
}
