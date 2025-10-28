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
        OutputPorts.Add(context.ConsumeMemoryID().ToString());
    }

    public override void LoadIntoMemory()
    {
        _context.AllocateMemory(OutputPorts[0], _roomEntry);
    }
}
