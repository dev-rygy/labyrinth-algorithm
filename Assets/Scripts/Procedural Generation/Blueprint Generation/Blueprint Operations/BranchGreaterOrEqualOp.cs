/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Conditional jump operation: if intA >= intB, jump to the target operation, otherwise fall through to the next queued
    /// operation as normal. Used as a loop's exit test - e.g. "if (loopIndex >= edgeCount) jump past the loop body"
    /// </summary>
    /// <remarks>It is safest to make the jump to a NoOp operation.</remarks>
    public class BranchGreaterOrEqualOp : BlueprintOperation
    {
        public BranchGreaterOrEqualOp(MapGenerationContext context, string targetOpIDInput, string intAInput, string intBInput) : base(context)
        {
            OperationID = $"BranchGreaterOrEqualOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(targetOpIDInput);        // Target for jump
            InputPorts.Add(intAInput);              // int input left
            InputPorts.Add(intBInput);              // int input right
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out string targetOpID))
                return false;
            if (!TryGetInput(1, out int itemA))
                return false;
            if (!TryGetInput(2, out int itemB))
                return false;

            if (itemA >= itemB)
            {
                // Branch to specified id
                return _context.Jump(targetOpID);
            }

            return true;
        }
    }
}
