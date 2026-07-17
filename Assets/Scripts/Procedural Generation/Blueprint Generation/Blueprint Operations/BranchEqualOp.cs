/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/

namespace RyansLibrary.Labyrinth
{
    public class BranchEqualOp : BlueprintOperation
    {
        public BranchEqualOp(MapGenerationContext context, string targetOpIDInput, string intAInput, string intBInput) : base(context)
        {
            OperationID = $"BranchEqualOp:{context.ConsumeOperationID()}";

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

            if (itemA == itemB)
            {
                // Branch to specified id
                return _context.Jump(targetOpID);
            }

            return true;
        }
    }
}
