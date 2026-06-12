/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class CheckOutOfBoundsOp : BlueprintOperation
    {
        public CheckOutOfBoundsOp(MapGenerationContext context, BlueprintGenerator bpg, string boundsAInput, string boundsBInput)
            : base(context, bpg)
        {
            OperationID = $"IntersectBoundsOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(boundsAInput);
            InputPorts.Add(boundsBInput);

            // Output Ports
            string memoryID = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID);
            if (_debugLogs) Debug.Log($"BoundsInt space allocated for memory with ID {memoryID}");
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out BoundsInt boundsA))
                return false;
            if (!TryGetInput(1, out BoundsInt boundsB))
                return false;

            BoundsInt intersectingBounds = CreateIntersectingBounds(boundsA, boundsB);

            _context.Malloc(OutputPorts[0], intersectingBounds);
            
            return true;
        }

        private BoundsInt CreateIntersectingBounds(BoundsInt intersectedBounds, BoundsInt intersectingBounds)
        {
            return CreateIntersectingBounds(intersectedBounds, intersectingBounds.size, intersectingBounds.position);
        }

        private BoundsInt CreateIntersectingBounds(BoundsInt intersectedBounds, Vector3Int size, Vector3Int offset)
        {
            Vector3Int position = intersectedBounds.min + offset;

            Vector3Int amountOutOfBounds = _bpg.CheckOutOfBounds(position, size, intersectedBounds);
            if (amountOutOfBounds != Vector3.zero)
            {
                Debug.LogWarning("Map Generator Warning: Desired intersecting bounds lies outside the overarching bounds. Adjusting size...");
                size -= amountOutOfBounds;
            }

            return new BoundsInt(position, size);
        }
    }
}
