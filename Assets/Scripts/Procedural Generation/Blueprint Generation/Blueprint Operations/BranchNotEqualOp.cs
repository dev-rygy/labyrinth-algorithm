/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Conditional jump: branches to the target operation if intA != intB, otherwise falls through. See
    /// BranchGreaterOrEqualOp.cs for how these branch ops act as loop/if conditions on top of the linear
    /// operation queue.
    /// </summary>
    /// <remarks>It is safest to make the jump to a NoOp operation.</remarks>
    public class BranchNotEqualOp : BlueprintOperation
    {
        public BranchNotEqualOp(MapGenerationContext context, string targetOpIDInput, string intAInput, string intBInput) : base(context)
        {
            OperationID = $"BranchNotEqualOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(targetOpIDInput);
            InputPorts.Add(intAInput);
            InputPorts.Add(intBInput);
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out string targetOpID))
                return false;
            if (!TryGetInput(1, out int itemA))
                return false;
            if (!TryGetInput(2, out int itemB))
                return false;

            if (itemA != itemB)
            {
                // Branch to specified id
                return _context.Jump(targetOpID);
            }

            return true;
        }
    }
}
