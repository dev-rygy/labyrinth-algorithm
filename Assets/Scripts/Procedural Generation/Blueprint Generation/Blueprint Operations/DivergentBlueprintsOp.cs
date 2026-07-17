/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   07/16/2026 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class DivergentBlueprintsOp : BlueprintOperation
    {
        public DivergentBlueprintsOp(MapGenerationContext context, string pathInput, string boundsInput, string dimensionsInput, string cellCountInput,
            string maxPlacementAttempsInput) : base(context)
        {
            OperationID = $"PlaceDivergentBlueprints:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(pathInput);
            InputPorts.Add(boundsInput);
            InputPorts.Add(dimensionsInput);
            InputPorts.Add(cellCountInput);
            InputPorts.Add(maxPlacementAttempsInput);
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out Path path))
                return false;
            if (!TryGetInput(1, out BoundsInt bounds))
                return false;
            if (!TryGetInput(2, out Vector3Int dimensions))
                return false;
            if (!TryGetInput(3, out int cellCount))
                return false;
            if (!TryGetInput(4, out int maxPlacementAttempts))
                return false;

            if (path == null)
            {
                LogNullError();
                return false;
            }

            return PlaceDivergentBlueprints(path, bounds, dimensions, cellCount, maxPlacementAttempts);
        }
        private bool PlaceDivergentBlueprints(Path path, BoundsInt bounds, Vector3Int dimensions, int cellCount, int maxPlacementAttempts)
        {
            int indexOffset = dimensions.x * dimensions.y;      // Increment by the cells taken up from the room dimensions

            if (cellCount % indexOffset != 0)       // Not all cells desired can fit within the dimensions of the divergent rooms
            {
                Debug.LogError($"DivergentBlueprintsOp: the path's desired cell spawn exceeds/falls short of the divergent " +
                    $"rooms with dimensions ({dimensions.x},{dimensions.y},{dimensions.z}).");
                return false;
            }

            for (int i = 0; i < cellCount; i += indexOffset)
            {
                bool successfullyPlaced = false;
                int placementAttempts = 0;

                while (!successfullyPlaced && placementAttempts < maxPlacementAttempts)
                {
                    // Attempt to spawn blueprints
                    successfullyPlaced = BlueprintGenerator.PlaceBoundedBlueprints(_context, path, bounds, Vector3Int.one, out Vector3Int spawnPos);

                    if (!successfullyPlaced)     // Failed placement
                        placementAttempts++;        // Increase attempts
                }

                // If divergent room failed to generate a certain number of times then return false
                if (!successfullyPlaced)
                {
                    Debug.LogError($"DivergentBlueprintsOp: A divergent room in path {path.name} has exhausted all of it's placement attempts.");
                    return false;
                }
            }

            return true;
        }
    }
}
