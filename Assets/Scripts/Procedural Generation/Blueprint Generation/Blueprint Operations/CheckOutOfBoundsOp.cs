/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Despite the name, this doesn't return a bool - it builds the sub-bounds of boundsA that intersects
    /// boundsB's size/position, shrinking that sub-bounds if it would otherwise poke outside boundsA
    /// (BlueprintGenerator.CheckOutOfBounds reports how far out and by how much to shrink). Used to safely carve a
    /// smaller bounded region (e.g. a room or sub-zone) out of a larger one without ever exceeding its parent.
    /// </summary>
    public class CheckOutOfBoundsOp : BlueprintOperation
    {
        public CheckOutOfBoundsOp(MapGenerationContext context, string boundsAInput, string boundsBInput)
            : base(context)
        {
            OperationID = $"IntersectBoundsOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(boundsAInput);       // Bounds A
            InputPorts.Add(boundsBInput);       // Bounds B

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

            BoundsInt intersectingBounds = CreateIntersectingBounds(boundsA, boundsB);

            _context.Set(OutputPorts[0], intersectingBounds);
            if (DebugLogs) Debug.Log($"CheckOutOfBoundsOp: Intersecting bounds loaded into memory with ID {OutputPorts[0]}");
            return true;
        }

        private BoundsInt CreateIntersectingBounds(BoundsInt intersectedBounds, BoundsInt intersectingBounds)
        {
            return CreateIntersectingBounds(intersectedBounds, intersectingBounds.size, intersectingBounds.position);
        }

        private BoundsInt CreateIntersectingBounds(BoundsInt intersectedBounds, Vector3Int size, Vector3Int offset)
        {
            Vector3Int position = intersectedBounds.min + offset;

            Vector3Int amountOutOfBounds = BlueprintGenerator.CheckOutOfBounds(position, size, intersectedBounds);
            if (amountOutOfBounds != Vector3.zero)
            {
                Debug.LogWarning("CheckOutOfBoundsOp: Desired intersecting bounds lies outside the overarching bounds. Adjusting size...");
                size -= amountOutOfBounds;
            }

            return new BoundsInt(position, size);
        }
    }
}
