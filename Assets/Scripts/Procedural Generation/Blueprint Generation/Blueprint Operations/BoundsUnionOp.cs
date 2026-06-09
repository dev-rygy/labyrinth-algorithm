/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   11/10/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class BoundsUnionOp : BlueprintOperation
    {
        public BoundsUnionOp(MapGenerationContext context, BlueprintGenerator bpg, string boundsAInput, string boundsBInput)
            : base(context, bpg)
        {
            OperationID = $"BoundsUnionOp:{context.ConsumeOperationID()}";

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

            BoundsInt combinedBounds = CombineBounds(boundsA, boundsB);

            _context.Malloc(OutputPorts[0], combinedBounds);

            return true;
        }

        // Helper
        private BoundsInt CombineBounds(BoundsInt boundsA, BoundsInt boundsB)
        {
            // Create shared bounds between two zones
            BoundsInt combinedBounds = new BoundsInt();
            Vector3Int position = new Vector3Int(
                                (int)(boundsA.position.x + boundsB.position.x) / 2,
                                (int)(boundsA.position.y + boundsB.position.y) / 2,
                                (int)(boundsA.position.z + boundsB.position.z) / 2);
            Vector3Int size = boundsA.size + boundsB.size;
            combinedBounds.position = position;
            combinedBounds.size = size;

            return combinedBounds;
        }
    }
}
