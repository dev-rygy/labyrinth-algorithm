/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class IntersectingBoundsOp : BlueprintOperation
    {
        public IntersectingBoundsOp(MapGenerationContext context, BlueprintGenerator bpg, string boundsAInput, string boundsBInput)
            : base(context, bpg)
        {
            OperationID = $"CreateIntersectingBounds:{context.ConsumeOperationID()}";
            _bpg = bpg;
            _context = context;

            // Input Ports
            InputPorts.Add(boundsAInput);
            InputPorts.Add(boundsBInput);

            // Output Ports
            OutputPorts.Add(context.ConsumeMemoryID().ToString());
        }

        public override bool Execute()
        {
            BoundsInt boundsA = (BoundsInt)_context.GrabFromMemory(InputPorts[0]);
            BoundsInt boundsB = (BoundsInt)_context.GrabFromMemory(InputPorts[1]);

            BoundsInt intersectingBounds = CreateIntersectingBounds(boundsA, boundsB);

            _context.AllocateOrModifyMem(OutputPorts[0], intersectingBounds);
            return true;
        }

        public override bool Undo()
        {
            return false;
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
