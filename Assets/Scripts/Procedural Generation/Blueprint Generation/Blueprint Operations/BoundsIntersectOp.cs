/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   11/10/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Operation that computes the overlapping region of two BoundsInt volumes (see BoundsIntUtils.IntersectBounds). Used when a
    /// zone needs to be constrained to the area shared between two other bounds, e.g. clipping a sub-zone so it
    /// never spills outside its parent zone.
    /// </summary>
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

            _context.Set(OutputPorts[0], intersectedBounds);

            return true;
        }
    }
}
