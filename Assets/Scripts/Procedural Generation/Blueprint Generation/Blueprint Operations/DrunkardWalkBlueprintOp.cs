using RyansLibrary.Labyrinth;
using System;
using UnityEngine;
using Random = UnityEngine.Random;  // Use Unity Engine's Random not System.Collection's Random

public class DrunkardWalkBlueprintOp : BlueprintOperation
{
    public DrunkardWalkBlueprintOp(MapGenerationContext context, BlueprintGenerator bpg, string pathInput, string branchedPathInput, string boundsInput, string startIndexInput, 
        string endIndexInput) : base(context, bpg)
    {
        OperationID = $"DrunkardWalkBlueprintOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(pathInput);          // Path Input
        InputPorts.Add(branchedPathInput);  // Branched Path Input
        InputPorts.Add(boundsInput);        // Bounds Input
        InputPorts.Add(startIndexInput);    // Start Index Input
        InputPorts.Add(endIndexInput);      // End Index Input
    }

    public override bool Execute()
    {
        if (!TryGetInput(0, out Path path))
            return false;
        if (!TryGetInput(1, out Path branchedPath))
            return false;
        if (!TryGetInput(2, out BoundsInt bounds))
            return false;
        if (!TryGetInput(3, out int startIndex))
            return false;
        if (!TryGetInput(4, out int endIndex))
            return false;

        return BlueprintDrunkardWalk(path, branchedPath, bounds, startIndex, endIndex);
    }

    public override bool Undo()
    {
        return false;
    }

    /// <summary>
    /// Drunkard Walk Algorithm, will walk a specified length and store it into a newly created path. The algorithm
    /// has been modified to handle collisions and create pseudo paths where rooms can potentially spawn later.
    /// </summary>
    /// <param name="path">A path with a length of atleast one.</param>
    /// <param name="startRoom">The starting room for the path. If null will create it's own start room</param>
    public bool BlueprintDrunkardWalk(Path path, Path branchedPath, BoundsInt bounds, int startIndex, int endIndex)
    {
        if (!path.IsInitialized)
        {
            Debug.LogWarning($"Map Generator Error: Path {path.Name} must be initialized for Drunkard Walk.");
            return false;
        }

        // Make sure the path has atleast one room cell that can spawn
        if (path.PathLength <= 0)
        {
            Debug.LogWarning($"Map Generator Error: Path {path.Name} has a length of 0 or is negative.");
            return false;
        }

        int randomStartingIndex = Random.Range(startIndex, endIndex);   // Choose a random room respecting the constraints

        // Attempt to place path in range
        bool pathPlaced = false;
        Func<int, int> circularIncrement = x => (x < endIndex) ? ++x : x = startIndex;
        for (int i = randomStartingIndex; i != randomStartingIndex - 1; i = circularIncrement(i))
        {
            // Choose new start room and clear rooms from last iteration if failed
            Blueprint startBlueprint = branchedPath.BlueprintList[i];
            path.ClearBlueprintRooms();

            if (!startBlueprint.Available)       // Check if start room is available
                continue;

            pathPlaced = BlueprintDrunkardWalkRecursive(path, bounds, startBlueprint);

            // Break out of loop to prevent duplicate path placement
            if (pathPlaced)
                break;
        }

        return pathPlaced;
    }

    private bool BlueprintDrunkardWalkRecursive(Path path, BoundsInt bounds, Blueprint previousBlueprint)
    {
        if (path.BlueprintCount() >= path.PathLength)
            return true;

        // Attempt to place a new room
        Blueprint newBlueprint = _bpg.PlaceBlueprintInRandomDirection(path, bounds, previousBlueprint);

        if (newBlueprint != null)    // New room was placed -> place next room
        {
            bool placed = BlueprintDrunkardWalkRecursive(path, bounds, newBlueprint);

            if (!placed)       // next room could not be placed? Continuation of path failed -> try prev room again
                return BlueprintDrunkardWalkRecursive(path, bounds, previousBlueprint);          // Backtrack
            else
                return true;
        }
        return false;    // No room could be placed
    }
}
