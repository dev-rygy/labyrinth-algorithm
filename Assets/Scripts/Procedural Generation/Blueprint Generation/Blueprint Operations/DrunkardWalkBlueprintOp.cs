/*
 * Created By:      Ryan Carpenter
 * Date Created:    11/01/2025
 * Last Modified:   11/01/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.Labyrinth;
using System;
using UnityEngine;
using Random = UnityEngine.Random;  // Use Unity Engine's Random not System.Collection's Random

public class DrunkardWalkBlueprintOp : BlueprintOperation
{
    public DrunkardWalkBlueprintOp(MapGenerationContext context, string pathInput, string branchedPathInput, string boundsInput, string startIndexInput,
        string endIndexInput, string canGoVertical) : base(context)
    {
        OperationID = $"DrunkardWalkBlueprintOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(pathInput);          // Path Input
        InputPorts.Add(branchedPathInput);  // Branched Path Input
        InputPorts.Add(boundsInput);        // Bounds Input
        InputPorts.Add(startIndexInput);    // Start Index Input
        InputPorts.Add(endIndexInput);      // End Index Input
        InputPorts.Add(canGoVertical);      // Can Go Vertical Input
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
        if (!TryGetInput(5, out bool canGoVertical))
            return false;

        return BlueprintDrunkardWalk(path, branchedPath, bounds, startIndex, endIndex, canGoVertical);
    }

    /// <summary>
    /// Drunkard Walk Algorithm, will walk a specified length and store it into a newly created path. The algorithm
    /// has been modified to handle collisions and create pseudo paths where rooms can potentially spawn later.
    /// </summary>
    /// <param name="path">A path with a length of atleast one.</param>
    /// <param name="startRoom">The starting room for the path. If null will create it's own start room</param>
    public bool BlueprintDrunkardWalk(Path path, Path branchedPath, BoundsInt bounds, int startIndex, int endIndex, bool canGoVertical)
    {
        if (!path.IsInitialized || !branchedPath.IsInitialized)
        {
            Debug.LogError($"DrunkardWalkBlueprintOp: Path {path.Name} or branched path {branchedPath.Name} must be initialized for Drunkard Walk.");
            return false;
        }

        // Make sure the path has atleast one room cell that can spawn
        if (path.DesiredPathLength <= 0)
        {
            Debug.LogError($"DrunkardWalkBlueprintOp: Path {path.Name} has a desired length of 0 or is negative.");
            return false;
        }

        if (endIndex >= branchedPath.BlueprintCount() || startIndex < 0)
        {
            Debug.LogError($"DrunkardWalkBlueprintOp: Ending index ({endIndex}) was greater than the path's length ({branchedPath.BlueprintCount()}) " +
                $"OR starting index ({startIndex}) was less than 0.");
            return false;
        }

        if (startIndex > endIndex)
        {
            Debug.LogError($"DrunkardWalkBlueprintOp: Starting index ({startIndex}) was greater than ending index ({endIndex}).");
            return false;
        }

        // Start index is the same as end index; therefore, only one index to check.
        if (startIndex == endIndex)
        {
            // Choose new start room and clear rooms from last iteration if failed
            Blueprint startBlueprint = branchedPath.BlueprintList[startIndex];
            path.ClearBlueprints();

            return BlueprintDrunkardWalkRecursive(path, bounds, startBlueprint, canGoVertical);
        }

        int randomStartingIndex = Random.Range(startIndex, endIndex);   // Choose a random room respecting the constraints

        // For safety the random start must be one index above the start or higher.
        randomStartingIndex = (randomStartingIndex == startIndex) ? startIndex + 1 : randomStartingIndex;

        // Attempt to place path in range
        bool pathPlaced = false;
        Func<int, int> circularIncrement = x => (x < endIndex) ? ++x : x = startIndex;
        for (int i = randomStartingIndex; i != randomStartingIndex - 1; i = circularIncrement(i))
        {
            // Choose new start room and clear rooms from last iteration if failed
            Blueprint startBlueprint = branchedPath.BlueprintList[i];
            path.ClearBlueprints();

            if (!startBlueprint.Available)       // Check if start room is available
                continue;

            pathPlaced = BlueprintDrunkardWalkRecursive(path, bounds, startBlueprint, canGoVertical);

            // Break out of loop to prevent duplicate path placement
            if (pathPlaced)
                break;
        }

        return pathPlaced;
    }

    private bool BlueprintDrunkardWalkRecursive(Path path, BoundsInt bounds, Blueprint previousBlueprint, bool canGoVertical)
    {
        if (path.BlueprintCount() >= path.DesiredPathLength)
            return true;

        // Attempt to place a new room
        Blueprint newBlueprint = BlueprintGenerator.PlaceBlueprintInRandomDirection(_context, path, bounds, previousBlueprint, canGoVertical);

        if (newBlueprint != null)    // New room was placed -> place next room
        {
            bool placed = BlueprintDrunkardWalkRecursive(path, bounds, newBlueprint, canGoVertical);

            if (!placed)       // next room could not be placed? Continuation of path failed -> try prev room again
                return BlueprintDrunkardWalkRecursive(path, bounds, previousBlueprint, canGoVertical);          // Backtrack
            else
                return true;
        }
        return false;    // No room could be placed
    }
}
