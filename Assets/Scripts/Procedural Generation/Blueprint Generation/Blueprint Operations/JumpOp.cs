/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Unconditional "goto" operation that moves the operation queue's cursor to the operation whose OperationID matches the
    /// given target (see MapGenerationContext.Jump).
    /// This is how the graph fakes a "for" loop over a plain linear operation queue.
    /// </summary>
    /// <remarks>It is safest to make the jump to a NoOp operation.</remarks>
    public class JumpOp : BlueprintOperation
    {
        public JumpOp(MapGenerationContext context, string targetOpIDInput)
                : base(context)
        {
            OperationID = $"JumpOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(targetOpIDInput);
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out string targetOpID))
                return false;

            // Branch to specified id
            return _context.Jump(targetOpID);
        }
    }
}
