/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/

namespace RyansLibrary.Labyrinth
{
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
