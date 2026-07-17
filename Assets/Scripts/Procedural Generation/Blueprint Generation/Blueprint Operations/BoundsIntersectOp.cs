/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   11/10/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class BoundsIntersectOp : BlueprintOperation
    {
        public BoundsIntersectOp(MapGenerationContext context, string boundsAInput, string boundsBInput)
            : base(context)
        {
            OperationID = $"BoundsIntersectOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(boundsAInput);
            InputPorts.Add(boundsBInput);

            // Output Ports
            string memoryID = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID);
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out BoundsInt boundsA))
                return false;
            if (!TryGetInput(1, out BoundsInt boundsB))
                return false;

            BoundsInt intersectedBounds = BoundsIntUtils.IntersectBounds(boundsA, boundsB);

            _context.Malloc(OutputPorts[0], intersectedBounds);

            return true;
        }
    }
}
