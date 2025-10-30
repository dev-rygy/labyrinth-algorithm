using RyansLibrary.Labyrinth;
using UnityEngine;

public class StringBlueprintData : BlueprintData
{
    MapGenerationContext _context;
    string stringData;

    public StringBlueprintData(MapGenerationContext context, string stringData) : base()
    {
        string memoryID = context.ConsumeMemoryID().ToString();
        DataID = $"StringData:{memoryID}";
        _context = context;
        this.stringData = stringData;

        // Output Ports
        OutputPorts.Add(memoryID);
    }

    public override void LoadIntoMemory()
    {
        _context.AllocateMemory(OutputPorts[0], stringData);
        Debug.Log($"String Data Loaded Into Memory with ID {OutputPorts[0]}");
    }
}
