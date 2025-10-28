using UnityEngine;

public class BoundsIntBlueprintData : BlueprintData
{
    MapGenerationContext _context;
    BoundsInt _bounds;

    public BoundsIntBlueprintData(MapGenerationContext context, BoundsInt bounds) : base()
    {
        DataID = $"PlaceFixedUniqueBlueprint:{context.ConsumeOperationID()}";
        _context = context;
        _bounds = bounds;

        // Output Ports
        OutputPorts.Add(context.ConsumeMemoryID().ToString());
    }

    public override void LoadIntoMemory()
    {
        _context.AllocateMemory(OutputPorts[0], _bounds);
    }
}
