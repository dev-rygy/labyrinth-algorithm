/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class PlaceFixedBlueprintsOp : BlueprintOperation
    {
        public PlaceFixedBlueprintsOp(MapGenerationContext context, BlueprintGenerator bpg, string pathInput, string roomEntryInput, string boundsInput)
            : base(context, bpg)
        {
            OperationID = $"PlaceFixedUniqueBlueprint:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(pathInput);
            InputPorts.Add(roomEntryInput);
            InputPorts.Add(boundsInput);

            // Output Ports
            string memoryID = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID);      // Blueprint List
            if (_debugLogs) Debug.Log($"[MapGenerator][BlueprintOperation] PlaceFixedBlueprintsOp: List<Blueprint> space allocated for memory with ID {memoryID}.");
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out Path path))
                return false;
            if (!TryGetInput(1, out RoomEntry entry))
                return false;
            if (!TryGetInput(2, out BoundsInt bounds))
                return false;

            if (path == null || entry == null)
            {
                LogNullError();
                return false;
            }

            return PlaceFixedUniqueRoomBlueprints(path, entry, bounds);
        }

        /// <summary>
        /// Places fixed room within the bounds of an zone; returns false if the
        /// room could not be placed correctly.
        /// </summary>
        /// <param name="entry">Room Entry</param>
        /// <param name="path">Path to store blueprints in</param>
        /// <param name="bounds">Bounds to constrict placement</param>
        /// <returns></returns>
        private bool PlaceFixedUniqueRoomBlueprints(Path path, RoomEntry entry, BoundsInt bounds)
        {
            if (entry.Prefab.TryGetComponent(out Room room))      // Prefab in entry does not have a Room Component
            {
                // Adjust parameters to fit the zone's actual position
                Vector3Int zoneOffset = bounds.position;
                Vector3Int roomOrigin = entry.SpawnPosition + zoneOffset;

                // Check Collision with the zone's bounds
                Vector3 difference = _bpg.CheckOutOfBounds(roomOrigin, room.RoomDimensions, bounds);
                if (difference != Vector3.zero)     // Room was outside the bounds of the zone
                {
                    Debug.LogError($"[MapGenerator][BlueprintOperation] PlaceFixedBlueprintsOp: Unique Fixed Room \"{room.name}\" was " +
                        $"outside of bounds and could not be placed. It was {difference} units outside the bounds of the zone.");
                    return false;
                }

                // Check Collision with other rooms
                List<Blueprint> blueprintList;
                blueprintList = _bpg.GenerateBlueprintsFromDimensions(path, roomOrigin, room.RoomDimensions, false);      // Fill room space with blueprint rooms
                if (blueprintList is null)     // Room was outside the bounds of the zone
                {
                    Debug.LogError($"[MapGenerator][BlueprintOperation] PlaceFixedBlueprintsOp: Unique Fixed Room \"{room.name}\" was obstructed and could not be placed.");
                    return false;
                }

                List<Blueprint> availableBlueprints = ToggleAvailableCellsInUniqueRoom(path, room.AvailableCellData, roomOrigin);
                if (availableBlueprints is null)
                {
                    Debug.LogError($"[MapGenerator][BlueprintOperation] PlaceFixedBlueprintsOp: Unique Room \"{room.name}\" has no available blueprint cells.");
                    return false;
                }

                _context.Malloc(OutputPorts[0], blueprintList);
                return true;
            }
            Debug.LogError($"[MapGenerator][BlueprintOperation] PlaceFixedBlueprintsOp: {entry.Prefab.name} does not have a Room script!");
            return false;
        }

        private List<Blueprint> ToggleAvailableCellsInUniqueRoom(Path path, List<Vector3Int> availableCells, Vector3Int roomOrigin, bool available = true)
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
