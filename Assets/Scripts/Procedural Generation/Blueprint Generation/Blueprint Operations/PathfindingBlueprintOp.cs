/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.AI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class PathfindingBlueprintOp : BlueprintOperation
    {
        public PathfindingBlueprintOp(MapGenerationContext context, string pathInput, string blueprintStartInput, string blueprintEndInput,
            string boundsInput, string obstructionsInput = "", string heuristicInput = "") : base(context)
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
            if (!TryGetInput(0, out Path path))
                return false;
            if (!TryGetInput(1, out Blueprint startBlueprint))
                return false;
            if (!TryGetInput(2, out Blueprint endBlueprint))
                return false;
            if (!TryGetInput(3, out BoundsInt bounds))
                return false;
            if (!TryGetInput(4, out List<Blueprint> obstructionList, false))
                return false;
            if (!TryGetInput(5, out Heuristic heuristic, false))
                return false;

            /* DEPRICATED: This was the old way of getting inputs, but it was not as clean as the new way above.
            List<Blueprint> obstructionList = null;
            Heuristic heuristic = Heuristic.Euclidean;
            if (InputPorts[4] != "")
            {
                if (!TryGetInput(4, out obstructionList))
                    return false;
            }
            if (InputPorts[5] != "")
            {
                if (!TryGetInput(5, out heuristic))
                    return false;
            }
            */

            if (path is null || startBlueprint is null || endBlueprint is null)
            {
                LogNullError();
                return false;
            }

            bool result = PathfindBlueprintFromPath(path, bounds, startBlueprint, endBlueprint, obstructionList, heuristic);

            return result;
        }

        private bool PathfindBlueprintFromPath(Path path, BoundsInt bounds, Blueprint startBlueprint, Blueprint endBlueprint, List<Blueprint> obstructions,
                Heuristic heuristic = Heuristic.Euclidean)
        {
            if (path is null)
            {
                Debug.LogError($"PathfindingBlueprintOp: Error: Path object was null for pathfind.");
                return false;
            }
            if (startBlueprint is null || endBlueprint is null)
            {
                Debug.LogError($"PathfindingBlueprintOp: Error: Starting/Ending Blueprint was null for pathfind.");
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
                Debug.LogError($"PathfindingBlueprintOp: Error: Pathfinding failed for edge.");
                return false;
            }

            Blueprint currentBlueprint = null;
            Blueprint previousBlueprint = null;
            foreach (Vector3Int pos in sequence)
            {
                if (_context.BlueprintDictionary.TryGetValue(pos, out var occupiedRoom))
                {
                    // Do not generate blueprint rooms if the space is already occupied
                    // Make the occupied blueprint room the currentRoom instead
                    currentBlueprint = occupiedRoom;
                }
                else
                    currentBlueprint = BlueprintGenerator.GenerateBlueprintRoom(_context, path, pos);

                if (previousBlueprint == null)
                {
                    previousBlueprint = currentBlueprint;
                    continue;
                }

                // Flag doorways of blueprint rooms
                Vector3Int difference = currentBlueprint.Position - previousBlueprint.Position;
                BlueprintGenerator.FlagEntryPoints(currentBlueprint, previousBlueprint, difference);

                previousBlueprint = currentBlueprint;
            }

            return true;
        }
    }
}
