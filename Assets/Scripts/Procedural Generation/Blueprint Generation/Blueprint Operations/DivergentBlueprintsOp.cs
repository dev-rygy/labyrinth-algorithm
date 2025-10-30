/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class DivergentBlueprintsOp : BlueprintOperation
    {
        public DivergentBlueprintsOp(MapGenerationContext context, BlueprintGenerator bpg, string pathInput, string boundsInput, string dimensionsInput, string cellCountInput, 
            string maxPlacementAttempsInput) : base(context, bpg)
        {
            OperationID = $"PlaceDivergentBlueprints:{context.ConsumeOperationID()}";
            _bpg = bpg;
            _context = context;

            // Input Ports
            InputPorts.Add(pathInput);
            InputPorts.Add(boundsInput);
            InputPorts.Add(dimensionsInput);
            InputPorts.Add(cellCountInput);
            InputPorts.Add(maxPlacementAttempsInput);
        }

        public override bool Execute()
        {
            Path path = _context.GrabFromMemory(InputPorts[0]) as Path;
            BoundsInt bounds = (BoundsInt)_context.GrabFromMemory(InputPorts[1]);
            Vector3Int dimensions = (Vector3Int)_context.GrabFromMemory(InputPorts[2]);
            int cellCount = (int)_context.GrabFromMemory(InputPorts[3]);
            int maxPlacementAttempts = (int)_context.GrabFromMemory(InputPorts[4]);

            if (path == null)
            {
                Debug.LogError($"The path was null.");
                return false;
            }

            return PlaceDivergentBlueprints(path, bounds, dimensions, cellCount, maxPlacementAttempts);
        }

        public override bool Undo()
        {
            return true;
        }

        private bool PlaceDivergentBlueprints(Path path, BoundsInt bounds, Vector3Int dimensions, int cellCount, int maxPlacementAttempts)
        {
            int indexOffset = dimensions.x * dimensions.y;      // Increment by the cells taken up from the room dimensions

            if (cellCount % indexOffset != 0)       // Not all cells desired can fit within the dimensions of the divergent rooms
            {
                Debug.LogError($"Map Generator Error: The path's disired cell spawn exceeds/falls short of the divergent " +
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
                    successfullyPlaced = PlaceBoundedBlueprints(path, bounds, Vector3Int.one, out Vector3Int spawnPos);

                    if (!successfullyPlaced)     // Failed placement
                        placementAttempts++;        // Increase attempts
                }

                // If divergent room failed to generate a certain number of times then return false
                if (!successfullyPlaced)
                {
                    Debug.LogError($"Map Generator Error: A divergent room in path {path.name} has exhaused all of it's placement attempts.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Will place rooms randomly in an zone but will pull rooms randomly from the main path.
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        private bool PlaceBoundedBlueprints(Path path, BoundsInt bounds, Vector3Int dimensions, out Vector3Int spawnPosition, bool available = true)
        {
            // Adjust the upper bounds so that the room's volume will properly fit within the bounded space; in
            // other words it will never spawn outside it's bounds
            Vector3Int adjUpperBound = new Vector3Int(
                bounds.xMax - dimensions.x,
                bounds.yMax - dimensions.y,
                bounds.zMax - dimensions.z
            );

            // Choose random spawn pos in the room's bounds;
            // NOTE: this random position is in room coords
            Vector3Int randomSpawnPos = new Vector3Int(
                Random.Range(bounds.xMin, adjUpperBound.x + 1),
                Random.Range(bounds.yMin, adjUpperBound.y + 1),
                Random.Range(bounds.zMin, adjUpperBound.z + 1)
            );

            // Append the newly generated blueprint rooms to the end of the list
            List<Blueprint> newBlueprints = _bpg.GenerateBlueprintsFromDimensions(path, randomSpawnPos, dimensions, available);

            spawnPosition = randomSpawnPos;

            // do not advance iteration if nothing was spawned
            if (newBlueprints == null)
                return false;

            return true;
        }
    }
}
