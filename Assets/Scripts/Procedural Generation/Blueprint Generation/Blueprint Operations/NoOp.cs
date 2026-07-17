/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/

namespace RyansLibrary.Labyrinth
{
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
