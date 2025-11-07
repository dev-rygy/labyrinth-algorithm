using RyansLibrary.AI;
using RyansLibrary.Labyrinth;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathfindingBlueprintOp : BlueprintOperation
{
    public PathfindingBlueprintOp(MapGenerationContext context, BlueprintGenerator bpg, string pathInput, string blueprintStartInput, string blueprintEndInput, 
        string boundsInput, string obstructionsInput = "", string heuristicInput = "") : base(context, bpg)
    {
        OperationID = $"PathfindingBlueprintOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(pathInput);                  // Path
        InputPorts.Add(blueprintStartInput);        // Blueprint Start
        InputPorts.Add(blueprintEndInput);          // Blueprint End
        InputPorts.Add(boundsInput);                // Bounds
        InputPorts.Add(obstructionsInput);          // Obstruction Blueprint List
        InputPorts.Add(heuristicInput);             // Pathfinding Heuristic
    }

    public override bool Execute()
    {
        if (!_context.TryGet(InputPorts[0], out Path path))
        {
            LogInputError(0, "path");
            return false;
        }
        if (!_context.TryGet(InputPorts[1], out Blueprint startBlueprint))
        {
            LogInputError(1, "startBlueprint");
            return false;
        }
        if (!_context.TryGet(InputPorts[2], out Blueprint endBlueprint))
        {
            LogInputError(2, "endBlueprint");
            return false;
        }
        if (!_context.TryGet(InputPorts[3], out BoundsInt bounds))
        {
            LogInputError(3, "boundsInt");
            return false;
        }

        List<Blueprint> obstructionList = null;
        Heuristic heuristic = Heuristic.Euclidean;
        if (InputPorts[4] != "")
        {
            if (!_context.TryGet(InputPorts[4], out obstructionList))
            {
                LogInputError(4, "obstructions");
                return false;
            }
        }
        if (InputPorts[5] != "")
        {
            if (!_context.TryGet(InputPorts[5], out heuristic))
            {
                LogInputError(5, "heuristic");
                return false;
            }
        }

        if (path is null || startBlueprint is null || endBlueprint is null)
        {
            LogNullError();
            return false;
        }

        bool result = PathfindBlueprintFromPath(path, bounds, startBlueprint, endBlueprint, obstructionList, heuristic);

        return result;
    }

    public override bool Undo()
    {
        return false;
    }

    private bool PathfindBlueprintFromPath(Path path, BoundsInt bounds, Blueprint startBlueprint, Blueprint endBlueprint, List<Blueprint> obstructions,
            Heuristic heuristic = Heuristic.Euclidean)
    {
        if (path is null)
        {
            Debug.LogError("Error: Path object was null for pathfind.");
            return false;
        }
        if (startBlueprint is null || endBlueprint is null)
        {
            Debug.LogError("Error: Starting/Ending Blueprint was null for pathfind.");
            return false;
        }

        HashSet<Vector3Int> obstructionPositions = null;
        if (obstructions is not null)
        {
            // Convert to HashSet for faster access
            obstructionPositions = new HashSet<Vector3Int>(obstructions.Select(b => b.Position));
        }

        // Find a sequence of points in room coordinates
        SimpleAStar3D aStar = new SimpleAStar3D(bounds);
        List<Vector3Int> sequence = aStar.FindPath(startBlueprint.Position, endBlueprint.Position, obstructionPositions, heuristic);

        if (sequence == null)
        {
            Debug.LogError($"Blueprint Generator Error: Pathfinding failed for edge.");
            return false;
        }

        Blueprint currentBlueprint = null;
        Blueprint previousBlueprint = null;
        foreach (Vector3Int pos in sequence)
        {
            if (_bpg.GetMasterDictionary().TryGetValue(pos, out var occupiedRoom))
            {
                // Do not generate blueprint rooms if the space is already occupied
                // Make the occupied blueprint room the currentRoom instead
                currentBlueprint = occupiedRoom;
            }
            else
                currentBlueprint = _bpg.GenerateBlueprintRoom(path, pos);

            if (previousBlueprint == null)
            {
                previousBlueprint = currentBlueprint;
                continue;
            }

            // Flag doorways of blueprint rooms
            Vector3Int difference = currentBlueprint.Position - previousBlueprint.Position;
            _bpg.FlagEntryPoints(currentBlueprint, previousBlueprint, difference);

            previousBlueprint = currentBlueprint;
        }

        return true;
    }
}
