using RyansLibrary.Labyrinth;
using System.Collections.Generic;
using UnityEngine;

public class PlaceBoundedUniqueBlueprintsOp : BlueprintOperation
{
    public PlaceBoundedUniqueBlueprintsOp(MapGenerationContext context, BlueprintGenerator bpg, string pathInput, string roomEntryInput, string boundsInput) 
        : base(context, bpg)
    {
        OperationID = $"PlaceBoundedUniqueBlueprint:{context.ConsumeOperationID()}";
        _bpg = bpg;
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
        {
            Debug.LogError($"The path or entry input was null.");
            return false;
        }

        return PlaceBoundedUniqueRoomBlueprints(path, entry, bounds);
    }

    public override bool Undo()
    {
        return false;
    }

    public bool PlaceBoundedUniqueRoomBlueprints(Path path, RoomEntry entry, BoundsInt bounds)
    {
        if (entry.Prefab.TryGetComponent(out Room room))      // Prefab in entry does not have a Room Component
        {
            bool result = PlaceBoundedBlueprints(path, bounds, room.RoomDimensions, out Vector3Int spawnPosition, false);

            if (!result)
            {
                if (_debugLogs)
                {
                    Debug.LogWarning($"Map Generator Warning: Constrained Room {entry.Prefab.name} " +
                        $"collided with another room and could not be placed. Retrying...");
                }
                return false;
            }

            entry.SpawnPosition = spawnPosition;

            List<Blueprint> availableBlueprints = ToggleAvailableCellsInUniqueRoom(path, entry.AvailableCells, spawnPosition);
            if (availableBlueprints is null)
            {
                Debug.LogError($"Blueprint Generator Error: Unique Room \"{room.name}\" has no available blueprint cells.");
                return false;
            }

            return true;
        }
        Debug.LogError($"Map Generator Error: {entry.Prefab.name} does not have a Room script!");
        return false;
    }

    /// <summary>
    /// Will place rooms randomly in an zone but will pull rooms randomly from the main path.
    /// </summary>
    /// <param name="zone"></param>
    /// <returns></returns>
    public bool PlaceBoundedBlueprints(Path path, BoundsInt bounds, Vector3Int dimensions, out Vector3Int spawnPosition, bool available = true)
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
