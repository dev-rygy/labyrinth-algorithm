using RyansLibrary.Labyrinth;
using UnityEngine;

public class RoomEntryBlueprintData : BlueprintData
{
    MapGenerationContext _context;
    RoomEntry _roomEntry;

    public RoomEntryBlueprintData(MapGenerationContext context, RoomEntry roomEntry) : base()
    {
        DataID = $"PlaceFixedUniqueBlueprint:{context.ConsumeOperationID()}";
        _context = context;
        _roomEntry = roomEntry;

        // Output Ports
        OutputPorts.Add(context.ConsumeMemoryID().ToString());      // RoomEntry
        OutputPorts.Add(context.ConsumeMemoryID().ToString());      // Bounds
    }

    public override void LoadIntoMemory()
    {
        _context.AllocateMemory(OutputPorts[0], _roomEntry);
        _context.AllocateMemory(OutputPorts[1], _roomEntry.Bounds);
        Debug.Log($"RoomEntry Data Loaded Into Memory with ID {OutputPorts[0]}");
        Debug.Log($"Bounds Data Loaded Into Memory with ID {OutputPorts[1]}");
    }
}
