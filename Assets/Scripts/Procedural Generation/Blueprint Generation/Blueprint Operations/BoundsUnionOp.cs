/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   11/10/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.Utilities;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Operation that combines two BoundsInt volumes into the largest volume that can possibly
    /// contain both (see BoundsIntUtils.CombineBounds). Used to grow a zone's bounds to encompass
    /// a neighboring zone/room rather than clipping to their overlap (the opposite of BoundsIntersectOp).
    /// </summary>
    public class BoundsUnionOp : BlueprintOperation
    {
        public BoundsUnionOp(MapGenerationContext context, string boundsAInput, string boundsBInput)
            : base(context)
        {
            OperationID = $"BoundsUnionOp:{context.ConsumeOperationID()}";

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

            BoundsInt combinedBounds = BoundsIntUtils.CombineBounds(boundsA, boundsB);

            _context.Set(OutputPorts[0], combinedBounds);

            return true;
        }
    }
}
