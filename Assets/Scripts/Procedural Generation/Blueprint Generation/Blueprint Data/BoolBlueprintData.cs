using RyansLibrary.Labyrinth;
using UnityEngine;

public class BoolBlueprintData : BlueprintData
{
    MapGenerationContext _context;
    bool boolean;

    public BoolBlueprintData(MapGenerationContext context, bool boolean) : base()
    {
        string memoryID = context.ConsumeMemoryID().ToString();
        DataID = $"BoolData:{memoryID}";
        _context = context;
        this.boolean = boolean;

        // Output Ports
        OutputPorts.Add(memoryID);      // RoomEntry
    }

    public override void LoadIntoMemory()
    {
        _context.AllocateMemory(OutputPorts[0], boolean);
        Debug.Log($"Int Data Loaded Into Memory with ID {OutputPorts[0]}");
    }
}
