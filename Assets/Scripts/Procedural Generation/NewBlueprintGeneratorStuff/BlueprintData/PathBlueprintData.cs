using RyansLibrary.Labyrinth;
using UnityEngine;

public class PathBlueprintData : BlueprintData
{
    MapGenerationContext _context;
    Path _path;

    public PathBlueprintData(MapGenerationContext context, Path path) : base()
    {
        DataID = $"PlaceFixedUniqueBlueprint:{context.ConsumeOperationID()}";
        _context = context;
        _path = path;

        // Output Ports
        OutputPorts.Add(context.ConsumeMemoryID().ToString());
    }

    public override void LoadIntoMemory()
    {
        _context.AllocateMemory(OutputPorts[0], _path);
    }
}
