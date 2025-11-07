using RyansLibrary.AI;
using RyansLibrary.Labyrinth;
using System;
using UnityEngine;

public class PathfindingHeuristicBlueprintData : BlueprintData
{
    Heuristic heuristic;

    public PathfindingHeuristicBlueprintData(MapGenerationContext context, Heuristic heuristic) : base(context)
    {
        string memoryID = context.ConsumeMemoryID().ToString();
        DataID = $"HeuristicData:{memoryID}";
        this.heuristic = heuristic;

        // Output Ports
        OutputPorts.Add(memoryID);      // Heuristic
    }

    public override void LoadIntoMemory()
    {
        _context.Set(OutputPorts[0], heuristic);
        Debug.Log($"Heuristic Data Loaded Into Memory with ID {OutputPorts[0]}");
    }
}
