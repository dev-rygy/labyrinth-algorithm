using RyansLibrary.Labyrinth;
using UnityEngine;

public class BoolBlueprintData : BlueprintData
{
    bool boolean;

    public BoolBlueprintData(MapGenerationContext context, bool boolean) : base(context)
    {
        string memoryID = context.ConsumeMemoryID().ToString();
        DataID = $"BoolData:{memoryID}";
        this.boolean = boolean;

        // Output Ports
        OutputPorts.Add(memoryID);      // RoomEntry
    }

    public override void LoadIntoMemory()
    {
        _context.AllocateMemory(OutputPorts[0], boolean);
        Debug.Log($"Bool Data Loaded Into Memory with ID {OutputPorts[0]}");
    }
}
