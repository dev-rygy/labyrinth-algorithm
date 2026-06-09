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
        public BoundsIntersectOp(MapGenerationContext context, BlueprintGenerator bpg, string boundsAInput, string boundsBInput)
            : base(context, bpg)
        {
            OperationID = $"BoundsIntersectOp:{context.ConsumeOperationID()}";

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

            BoundsInt intersectedBounds = IntersectBounds(boundsA, boundsB);

            _context.Malloc(OutputPorts[0], intersectedBounds);

            return true;
        }

        // Helper
        private BoundsInt IntersectBounds(BoundsInt boundsA, BoundsInt boundsB)
        {
            // Create shared bounds between two zones
            BoundsInt intersectedBounds = new BoundsInt();

            // Find what bounds has the absolute minimum and maximum values on each axis and use those to create the intersecting bounds
            Vector3Int min = new Vector3Int(Mathf.Max(boundsA.xMin, boundsB.xMin), Mathf.Max(boundsA.yMin, boundsB.yMin), Mathf.Max(boundsA.zMin, boundsB.zMin));
            Vector3Int max = new Vector3Int(Mathf.Min(boundsA.xMax, boundsB.xMax), Mathf.Min(boundsA.yMax, boundsB.yMax), Mathf.Min(boundsA.zMax, boundsB.zMax));

            intersectedBounds = new BoundsInt(min, max - min);

            return intersectedBounds;
        }
    }
}
