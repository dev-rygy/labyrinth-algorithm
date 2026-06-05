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
    public class PlaceBoundedBlueprintsOp : BlueprintOperation
    {
        public PlaceBoundedBlueprintsOp(MapGenerationContext context, BlueprintGenerator bpg, string pathInput, string roomEntryInput, string boundsInput) : base(context, bpg)
        {
            OperationID = $"PlaceBoundedUniqueBlueprint:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(pathInput);
            InputPorts.Add(roomEntryInput);
            InputPorts.Add(boundsInput);
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
                bool result = PlaceBoundedBlueprints(path, bounds, room.RoomDimensions, out Vector3Int spawnPosition, false);

                if (!result)
                {
                    Debug.LogWarning($"[MapGenerator][BlueprintOperation] Constrained Room {entry.Prefab.name} " +
                        $"collided with another room and could not be placed. Retrying...");
                    return false;
                }

                entry.SpawnPosition = spawnPosition;

                List<Blueprint> availableBlueprints = ToggleAvailableCellsInUniqueRoom(path, entry.AvailableCells, spawnPosition);
                if (availableBlueprints is null)
                {
                    Debug.LogError($"[MapGenerator][BlueprintOperation] Unique Room \"{room.name}\" has no available blueprint cells.");
                    return false;
                }

                return true;
            }
            Debug.LogError($"[MapGenerator][BlueprintOperation] {entry.Prefab.name} does not have a Room script!");
            return false;
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

        public List<Blueprint> ToggleAvailableCellsInUniqueRoom(Path path, List<Vector3Int> availableCells, Vector3Int roomOrigin, bool available = true)
        {
            List<Blueprint> availibleBlueprints = new List<Blueprint>();

            // Set cells that are supposed to be available to available
            foreach (Vector3Int cell in availableCells)
            {
                Vector3Int cellPosition = roomOrigin + cell;      // Find the actual position in room space of the cell

                if (_bpg.GetMasterDictionary().TryGetValue(cellPosition, out Blueprint blueprint))
                {
                    availibleBlueprints.Add(blueprint);
                    blueprint.Available = available;
                }
                else
                    availibleBlueprints.Add(_bpg.GenerateBlueprintRoom(path, cellPosition, available));
            }

            return availibleBlueprints;
        }
    }
}
