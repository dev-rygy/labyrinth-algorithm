/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Does nothing - its only purpose is to exist at a known position in the operation queue so a JumpOp or
    /// BranchGreaterOrEqualOp has somewhere to jump *to* (its OperationID is the "address" other ops branch to).
    /// Equivalent to a label in assembly, most commonly placed right after a loop as the "break out of loop" target.
    /// </summary>
    public class NoOp : BlueprintOperation
    {
        public NoOp(MapGenerationContext context) : base(context)
        {
            OperationID = $"NoOp:{context.ConsumeOperationID()}";
        }

        public override bool Execute()
        {
            return true;
        }
    }
}
