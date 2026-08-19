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
    /// <summary>
    /// Places a "unique" room prefab at a random valid position within a bounds, retrying with a new
    /// random position whenever it collides with something already placed.
    /// </summary>
    // TODO: There are no good loop safeguards in this operation. Needs fixing later.
    public class PlaceBoundedBlueprintsOp : BlueprintOperation
    {
        public PlaceBoundedBlueprintsOp(MapGenerationContext context, string pathInput, string roomEntryInput, string boundsInput) : base(context)
        {
            OperationID = $"PlaceBoundedUniqueBlueprint:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(pathInput);
            InputPorts.Add(roomEntryInput);
            InputPorts.Add(boundsInput);

            // Output Ports
            string memoryID = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID);      // Blueprint List
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out Path path))
                return false;
            if (!TryGetInput(1, out RoomEntry entry))
                return false;
            if (!TryGetInput(2, out BoundsInt bounds))
                return false;

            if (path is null || entry is null)
            {
                LogNullError();
                return false;
            }

            bool result = false;
            while (!result)
            {
                result = PlaceBoundedUniqueRoomBlueprints(path, entry, bounds);
            }

            return true;
        }

        private bool PlaceBoundedUniqueRoomBlueprints(Path path, RoomEntry entry, BoundsInt bounds)
        {
            if (entry.Prefab.TryGetComponent(out Room room))      // Prefab in entry does not have a Room Component
            {
                bool result = BlueprintGenerator.PlaceBoundedBlueprints(_context, path, bounds, room.RoomDimensions, out Vector3Int spawnPosition, false);

                if (!result)
                {
                    if (DebugLogs) Debug.LogWarning($"PlaceBoundedBlueprintsOp: Constrained Room {entry.Prefab.name} " +
                        $"collided with another room and could not be placed. Retrying...");
                    return false;
                }

                entry.SpawnPosition = spawnPosition;

                List<Blueprint> availableBlueprints = BlueprintGenerator.ToggleAvailableBlueprintsInRoom(_context, path, room.RoomCells, spawnPosition);
                if (availableBlueprints is null)
                {
                    Debug.LogError($"PlaceBoundedBlueprintsOp: Unique Room \"{room.name}\" has no available blueprint cells.");
                    return false;
                }

                _context.Set(OutputPorts[0], availableBlueprints);
                return true;
            }
            Debug.LogError($"PlaceBoundedBlueprintsOp: {entry.Prefab.name} does not have a Room script!");
            return false;
        }
    }
}
