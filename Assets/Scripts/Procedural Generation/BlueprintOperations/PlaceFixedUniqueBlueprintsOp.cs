/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   10/26/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.Labyrinth;
using System.Collections.Generic;
using UnityEngine;

public class PlaceFixedUniqueBlueprintsOp : BlueprintOperation
{
    BlueprintGenerator _bpg;
    MapGenerationContext _context;

    public PlaceFixedUniqueBlueprintsOp(MapGenerationContext context, string pathInput, string roomEntryInput, string boundsInput)
    {
        OperationID = $"PlaceFixedUniqueBlueprint:{context.ConsumeOperationID()}";
        _context = context;

        // Input Ports
        InputPorts.Add(pathInput);
        InputPorts.Add(roomEntryInput);
        InputPorts.Add(boundsInput);

        // Output Ports
        OutputPorts.Add(context.ConsumeMemoryID().ToString());
    }

    public override bool Execute()
    {
        Path path = _context.GrabFromMemory(InputPorts[0]) as Path;
        RoomEntry entry = _context.GrabFromMemory(InputPorts[1]) as RoomEntry;
        BoundsInt bounds = (BoundsInt)_context.GrabFromMemory(InputPorts[2]);

        if (path == null || entry == null)
            return false;

        return PlaceFixedUniqueRoomBlueprints(path, entry, bounds);
    }

    public override bool Undo()
    {
        return false;
    }

    /// <summary>
    /// Places fixed room within the bounds of an zone; returns false if the
    /// room could not be placed correctly.
    /// </summary>
    /// <param name="entry">Room Entry</param>
    /// <param name="path">Path to store blueprints in</param>
    /// <param name="bounds">Bounds to constrict placement</param>
    /// <returns></returns>
    public bool PlaceFixedUniqueRoomBlueprints(Path path, RoomEntry entry, BoundsInt bounds)
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
                Debug.LogError($"Blueprint Generator Error: Unique Room \"{room.name}\" was outside of bounds and could not be placed.\n" +
                    $"It was {difference} units outside the bounds of the zone.");
                return false;
            }

            // TODO: Use hash map instead of List for faster lookup maybe?
            // Check Collision with other rooms
            List<Blueprint> blueprintList;
            blueprintList = _bpg.GenerateBlueprintsFromDimensions(path, roomOrigin, room.RoomDimensions, false);      // Fill room space with blueprint rooms
            if (blueprintList is null)     // Room was outside the bounds of the zone
            {
                Debug.LogError($"Blueprint Generator Error: Unique Room \"{room.name}\" was obstructed and could not be placed");
                return false;
            }

            List<Blueprint> availableBlueprints = _bpg.ToggleAvailableCellsInUniqueRoom(path, entry.AvailableCells, roomOrigin);
            if (availableBlueprints is null)
            {
                Debug.LogError($"Blueprint Generator Error: Unique Room \"{room.name}\" has no available blueprint cells.");
                return false;
            }

            _context.AllocateOrModifyMem(OutputPorts[0], blueprintList);

            return true;
        }
        Debug.LogError($"Map Generator Error: {entry.Prefab.name} does not have a Room script!");
        return false;
    }
}
