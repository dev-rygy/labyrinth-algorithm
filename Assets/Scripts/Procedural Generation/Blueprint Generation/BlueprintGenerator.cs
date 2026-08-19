/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   07/16/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;  // Use Unity Engine's Random not System.Collection's Random

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Holds the properties of a psuedo room (Blueprint) that does not actually exist in the world.
    /// Is meant to be parsed and replaced by actual rooms later on.
    /// </summary>
    /// <remarks>
    /// The whole labyrinth is generated in two passes: first the algorithm reasons about the *layout* using cheap,
    /// abstract Blueprint objects on a room-sized grid (one unit = one room cell, see "room coords" throughout this
    /// file), then a later pass (RoomGenerator) walks the finished blueprint graph and instantiates real room
    /// prefabs in world space. Working in this abstract space first is what makes graph algorithms like Delaunay
    /// triangulation / MST / A* / drunkard-walk cheap and simple: they only ever operate on integer grid positions
    /// and a dictionary lookup, never on actual geometry.
    /// </remarks>
    public class Blueprint
    {
        public readonly string CellID;
        public readonly Vector3Int Position;    // Position of blueprint coords on grid
        public bool Available { get; set; }       // Prevents/allows parsing algorithm from using blueprint in algorithm
        public bool[] EntryPointFlags { get; set; }

        // Constructor
        public Blueprint(Vector3Int postion, string cellID = "Blueprint")
        {
            Available = true;
            CellID = cellID;
            Position = postion;
            EntryPointFlags = new bool[6];       // A flag to mark which entrances should be open for a room
        }
    }

    /// <summary>
    /// Static toolbox of primitive operations for building and querying the blueprint grid (placing rooms,
    /// checking collisions/bounds, flagging doorways between neighboring rooms, etc). This is the layer that the
    /// higher-level Blueprint Operations (DrunkardWalkBlueprintOp, PathfindingBlueprintOp, PlaceBoundedBlueprintsOp,
    /// ...) are built on top of - those classes decide *when* and *where* to call into these methods, while this
    /// class enforces the low-level invariants (no two rooms in the same cell, doorways always flagged on both
    /// sides, etc).
    /// </summary>
    public static class BlueprintGenerator
    {
        // Amount of faces on a blueprint room; This should never be changed unless unique shaped rooms are made in the future
        const int k_standardFaceCount = 6;

        private static bool _debugLogs = false;

        #region Blueprint Room Generation
        /// <summary>
        /// Generate a new blueprint room at the desired location. Add it to the master path and
        /// desired path passed in as an arguement. Generate a blueprint room gizmo if debug is enabled.
        /// NOTE: Position must be in room coords
        /// </summary>
        /// <param name="path">The desired path to add the new blueprint room to.</param>
        /// <param name="origin">The desired position to spawn the new room at. Must be in world coords</param>
        /// <returns>Blueprint room created in room coords.</returns>
        public static Blueprint GenerateBlueprint(MapGenerationContext context, Path path, Vector3Int origin, bool available = true)
        {
            if (context == null || context.BlueprintDictionary == null || path == null)
            {
                Debug.LogError("Context/Dictionary/Path not initialized to spawn Blueprint.");
                return null;
            }

            if (context.BlueprintDictionary.TryGetValue(origin, out Blueprint blueprint))
            {
                Debug.LogError("Blueprint already exists in location. Terminating spawn.");
                return null;
            }

            string blueprintName = $"BlueprintRoom ({context.BlueprintDictionary.Count()})";
            Blueprint newBlueprint = new Blueprint(origin, blueprintName);
            newBlueprint.Available = available;

            // Update paths and masters with new blueprint room
            path.Add(newBlueprint);
            context.BlueprintDictionary.Add(origin, newBlueprint);      // Add to Master Dictionary (required)

            if (_debugLogs) Debug.Log($"Generated blueprint room {blueprintName} at {origin}");
            return newBlueprint;
        }

        /// <summary>
        /// Generates a variety of blueprint rooms based on a given dimension starting at a point
        /// If a collision occurs then the method returns a null list.
        /// position and dimensions must be in room coordinates!
        /// </summary>
        /// <param name="path">Path to add blueprint rooms to</param>
        /// <param name="origin">Start position</param>
        /// <param name="roomDimensions"></param>
        /// <returns>Blueprint rooms generated in a list if needed.</returns>
        public static List<Blueprint> GenerateBlueprintsFromDimensions(MapGenerationContext context, Path path, Vector3Int origin, Vector3Int roomDimensions, bool available = true)
        {
            List<Blueprint> roomBlueprints = new List<Blueprint>();
            List<Vector3Int> blueprintroomPositions = new List<Vector3Int>();

            for (int x = origin.x; x < (origin.x + roomDimensions.x); x++)      // traverse x dimensions
            {
                for (int y = origin.y; y < (origin.y + roomDimensions.y); y++)      // traverse y dimensions
                {
                    for (int z = origin.z; z < (origin.z + roomDimensions.z); z++)      // traverse z dimensions
                    {
                        Vector3Int blueprintroomPos = new Vector3Int(x, y, z);

                        if (CheckCollision(context, blueprintroomPos, out Blueprint collidedBlueprint))
                        {
                            if (_debugLogs)
                                Debug.LogWarning($"Failed to generate blueprint room due to collision with {collidedBlueprint.CellID}");
                            return null;
                        }

                        blueprintroomPositions.Add(blueprintroomPos);
                    }
                }
            }

            // If no errors then generate blueprint rooms from dimensions
            foreach (Vector3Int spawnPosition in blueprintroomPositions)
                roomBlueprints.Add(GenerateBlueprint(context, path, spawnPosition, available));      // Call to method above

            return roomBlueprints;
        }

        /// <summary>
        /// Will place rooms randomly in a zone but will pull rooms randomly from the main path.
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        public static bool PlaceBoundedBlueprints(MapGenerationContext context, Path path, BoundsInt bounds, Vector3Int dimensions, out Vector3Int spawnPosition, bool available = true)
        {
            // Adjust the upper bounds so that the room's volume will properly fit within the bounded space; in
            // other words it will never spawn outside it's bounds
            Vector3Int adjUpperBound = new Vector3Int(
                bounds.xMax - dimensions.x,
                bounds.yMax - dimensions.y,
                bounds.zMax - dimensions.z
            );

            // Choose random spawn pos in the room's bounds;
            // NOTE: this random position is in room coords
            Vector3Int randomSpawnPos = new Vector3Int(
                Random.Range(bounds.xMin, adjUpperBound.x + 1),
                Random.Range(bounds.yMin, adjUpperBound.y + 1),
                Random.Range(bounds.zMin, adjUpperBound.z + 1)
            );

            // Append the newly generated blueprint rooms to the end of the list
            List<Blueprint> newBlueprints = GenerateBlueprintsFromDimensions(context, path, randomSpawnPos, dimensions, available);

            spawnPosition = randomSpawnPos;

            // do not advance iteration if nothing was spawned
            if (newBlueprints == null)
                return false;

            return true;
        }

        // This is the single-step move used by the drunkard-walk algorithm (see DrunkardWalkBlueprintOp): given the
        // last room placed, try to grow the path by one room in a random cardinal direction. Every direction is
        // tried at most once per call (never the same failed direction twice) before giving up, so the caller can
        // reliably tell "no room fits here at all" apart from "just got unlucky."
        public static Blueprint PlaceBlueprintInRandomDirection(MapGenerationContext context, Path path, BoundsInt bounds, Blueprint previousBlueprint, bool canGoVertical)
        {
            // If path can be placed in a vertical direction then all 6 faces are available, otherwise only 4 horizontal faces are available
            int availableDirections = canGoVertical ? k_standardFaceCount : k_standardFaceCount - 2;

            // Chose a position in a random cardinal direction and check for collisions
            bool[] attempts = new bool[availableDirections];
            int failedAttempts = 0;

            while (failedAttempts < availableDirections)
            {
                // Choose a random direction to be the potential position for the next room.
                int directionalIndex = Random.Range(0, availableDirections);
                // attempts[] marks directions already ruled out this call; FindIndexCircular walks forward from the
                // random index to the next direction not yet tried, so repeated failures can't roll the same dud twice.
                directionalIndex = ArrayUtils.FindIndexCircular(attempts, directionalIndex, x => x == false);

                if (directionalIndex < 0)
                    return null;

                Vector3Int tempPos = previousBlueprint.Position + GetDirectionFromIndex(directionalIndex);

                // Check position in hash map; if failed then flag face attempt and try choosing a new position 
                if (!CheckOutOfBounds(tempPos, bounds) && !CheckCollision(context, tempPos))     // If position is not out of bounds and not colliding with another room
                {
                    // Return new blueprint room
                    Blueprint newBlueprint = GenerateBlueprint(context, path, tempPos);
                    FlagEntryPoints(newBlueprint, previousBlueprint, directionalIndex);                    // Flag the face that touches the opposite room

                    return newBlueprint;
                }

                attempts[directionalIndex] = true;
                failedAttempts++;
            }

            return null;
        }

        private static Vector3Int GetDirectionFromIndex(int index)
        {
            switch (index)
            {
                // F0 - F5 is the face count for a unit room, this will be used later for entranceways
                case 0:
                    return Vector3Int.right;    // F0 : (1, 0, 0); Wall Right
                case 1:
                    return Vector3Int.left;     // F1 : (-1, 0, 0); Wall Left
                case 2:
                    return Vector3Int.forward;  // F2 : (0, 0, 1); Wall Forward
                case 3:
                    return Vector3Int.back;     // F3 : (0, 0, -1); Wall Back
                case 4:
                    return Vector3Int.up;       // F4 : (0, 1, 0); Wall Top
                case 5:
                    return Vector3Int.down;     // F5 : (0, 1, 0); Wall Bot
                default:
                    Debug.LogError("Direction choosen does not exist.");
                    return Vector3Int.zero;
            }
        }

        private static int GetIndexFromDirection(Vector3Int direction)
        {
            if (direction == Vector3Int.right)          // F0 : (1, 0, 0); Wall Right
                return 0;
            else if (direction == Vector3Int.left)      // F1 : (-1, 0, 0); Wall Left
                return 1;
            else if (direction == Vector3Int.forward)   // F2 : (0, 0, 1); Wall Forward
                return 2;
            else if (direction == Vector3Int.back)      // F3 : (0, 0, -1); Wall Back
                return 3;
            else if (direction == Vector3Int.up)        // F4 : (0, 1, 0); Wall Top
                return 4;
            else if (direction == Vector3Int.down)      // F5 : (0, 1, 0); Wall Bot
                return 5;
            else
                return -1;   // Default or error case
        }

        public static void FlagEntryPoints(Blueprint blueprintA, Blueprint blueprintB, Vector3Int difference)
        {
            // TODO: Bad way of handling this. Find a better way
            int entrFlagIdx = GetIndexFromDirection(difference);

            FlagEntryPoints(blueprintA, blueprintB, entrFlagIdx);
        }

        /// <summary>
        /// Pass in two rooms and link their entrancways together.
        /// </summary>
        /// <param name="blueprintA">First blueprint room</param>
        /// <param name="blueprintB">Second blueprint room</param>
        /// <param name="entrFlagIdx">The index of the choosen face of the *first* room.</param>
        public static void FlagEntryPoints(Blueprint blueprintA, Blueprint blueprintB, int entrFlagIdx) // Flag the entranceways to be activated in each room
        {
            if (entrFlagIdx < 0)
            {
                Debug.LogError("The two blueprints are invalid for entrance connection");
                return;
            }

            // Faces are stored in opposite pairs at adjacent indices (0/1 = right/left, 2/3 = forward/back,
            // 4/5 = up/down - see GetDirectionFromIndex), so "the opposite face" is always +1 (from an even index)
            // or -1 (from an odd index). This lets a door be opened on both sides of the connection with simple
            // integer math instead of a lookup table.
            // Flag the fact of the next room facing the prev. room
            if (Math.IsEven(entrFlagIdx))                                   // If choosen an even numbered side then set opposite to true (Ex. F4 -> F3 = true)
                blueprintA.EntryPointFlags[entrFlagIdx + 1] = true;
            else                                                            // If choosen an odd numbered side then set opposite to true (Ex. F3 -> F4 = true)
                blueprintA.EntryPointFlags[entrFlagIdx - 1] = true;

            // Flag the face of the prev. room facing the next room
            blueprintB.EntryPointFlags[entrFlagIdx] = true;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Choose a random room in a path. If endIndex = -1 => endIndex = path's last room.
        /// </summary>
        /// <param name="path">The path to choose the starting room from</param>
        /// <param name="startIndex">Index to start from</param>
        /// <returns>The Choosen Blueprint Room.</returns>
        public static Blueprint ChooseRandomBlueprintInPath(Path path, int startIndex = 0, int endIndex = -1)
        {
            // Default the endIndex to the path's end index
            if (endIndex == -1)
                endIndex = path.BlueprintCount() - 1;

            // Check if range is valid
            if ((startIndex < 0) || (startIndex > endIndex) || (endIndex > (path.BlueprintCount() - 1)))
            {
                Debug.LogError("Path index out of range or set incorrectly.");
                return null;
            }

            // Check if path to choose from is valid
            if (path.BlueprintCount() <= 0)
            {
                Debug.LogError($"A starting room could not be choosen because {path.Name} has no rooms.");
                return null;
            }

            // TODO: Make a enum/layer mask perameter that can choose a room from a specific type or types

            // Choose a random room respecting the constraints and return
            int randomBlueprintListIndex = Random.Range(startIndex, endIndex);
            Blueprint blueprint = path.BlueprintList[randomBlueprintListIndex];

            // TODO: Make a circular array handle this
            if (!blueprint.Available)
            {
                //Debug.LogWarning("Map Generator Warning: unavailable room choosen for path start. Choosing a new room...");
                blueprint = ChooseRandomBlueprintInPath(path, startIndex, endIndex);
            }

            if (_debugLogs) Debug.Log($"Random room choosen from {path.Name} at index {randomBlueprintListIndex}");

            return blueprint;
        }

        public static Blueprint FindClosestBlueprintInPath(Path path, Vector3Int point)     // UNUSED
        {
            if (path == null)
            {
                Debug.LogError("Path was null.");
                return null;
            }

            Blueprint closestCell = null;
            float distance = Mathf.Infinity;
            foreach (Blueprint blueprint in path.BlueprintList)
            {
                if (blueprint.Available)
                {
                    float currentDistance = Vector3Int.Distance(point, blueprint.Position);
                    if (currentDistance < distance)
                    {
                        closestCell = blueprint;
                        distance = currentDistance;
                    }
                }
            }

            return closestCell;
        }

        public static List<Blueprint> FindBlueprintsWithAvailibility(List<Blueprint> blueprintList, bool availibility)
        {
            return new List<Blueprint>(blueprintList.Where(b => (b.Available == availibility)).ToList());
        }

        public static Blueprint FindFirstBlueprintWithAvailibility(List<Blueprint> blueprintList, bool availibility)
        {
            return blueprintList.FirstOrDefault(b => (b.Available == availibility));
        }

        public static BoundsInt CombineBounds(BoundsInt boundsA, BoundsInt boundsB)
        {
            // Create shared bounds between two zones
            BoundsInt combinedBounds = new BoundsInt();
            Vector3Int position = new Vector3Int(
                                (int)(boundsA.position.x + boundsB.position.x) / 2,
                                (int)(boundsA.position.y + boundsB.position.y) / 2,
                                (int)(boundsA.position.z + boundsB.position.z) / 2);
            Vector3Int size = boundsA.size + boundsB.size;
            combinedBounds.position = position;
            combinedBounds.size = size;

            return combinedBounds;
        }

        public static BoundsInt CreateIntersectingBounds(BoundsInt intersectedBounds, Vector3Int size, Vector3Int offset)
        {
            Vector3Int position = intersectedBounds.min + offset;

            Vector3Int amountOutOfBounds = CheckOutOfBounds(position, size, intersectedBounds);
            if (amountOutOfBounds != Vector3.zero)
            {
                Debug.LogWarning("Desired intersecting bounds lies outside the overarching bounds. Adjusting size...");
                size -= amountOutOfBounds;
            }

            return new BoundsInt(position, size);
        }

        public static BoundsInt CreateIntersectingBounds(BoundsInt intersectedBounds, BoundsInt intersectingBounds)
        {
            return CreateIntersectingBounds(intersectedBounds, intersectingBounds.size, intersectingBounds.position);
        }

        /// <summary>
        /// Check if a point lies outside the bounds of the zone.
        /// *** The point must be in world coords ***
        /// </summary>
        /// <param name="desiredPosition">The desired position to spawn the next room</param>
        /// <returns>Returns TRUE if the space is out of bounds and FALSE otherwise.</returns>
        public static bool CheckOutOfBounds(Vector3Int desiredPosition, Vector3Int upperBound, Vector3Int lowerBound)
        {
            Vector3Int differenceUpper = upperBound - desiredPosition;
            Vector3Int differenceLower = lowerBound - desiredPosition;
            if (differenceUpper.x <= 0 || differenceUpper.y <= 0 || differenceUpper.z <= 0)        // Valid space
                return false;
            if (differenceLower.x > 0 || differenceLower.y > 0 || differenceLower.z > 0)        // Valid space
                return false;

            return true;           // Invalid space
        }

        public static bool CheckOutOfBounds(Vector3Int desiredPos, BoundsInt bounds)
        {
            if (bounds.Contains(desiredPos))        // Valid space
                return false;

            return true;           // Invalid space
        }

        /// <summary>
        /// Checks if a room is out of bounds with just it's origin and dimensions.
        /// Returns the difference of how much of the room lied outside the bounds.
        /// **** Points and dimensions must be in world coords ****
        /// </summary>
        /// <param name="origin">Blueprint origin</param>
        /// <param name="dimensions">Blueprint dimensions</param>
        /// <param name="bounds">Bounds</param>
        /// <returns>Zero if in bounds, otherwise will return the offset amount in room coords</returns>
        public static Vector3Int CheckOutOfBounds(Vector3Int origin, Vector3Int dimensions, BoundsInt bounds)
        {
            // Find the lower and upper cell point of the blueprint
            Vector3Int lowerPoint = origin;
            Vector3Int upperPoint = origin + (dimensions - Vector3Int.one);

            Vector3Int difference = Vector3Int.zero;

            // X Axis
            if (lowerPoint.x < bounds.min.x)
                difference.x = lowerPoint.x - bounds.min.x;         // negative (left out of bounds)
            else if (upperPoint.x > bounds.max.x - 1)
                difference.x = upperPoint.x - (bounds.max.x - 1);   // positive (right out of bounds)

            // Y Axis
            if (lowerPoint.y < bounds.min.y)
                difference.y = lowerPoint.y - bounds.min.y;         // negative (below)
            else if (upperPoint.y > bounds.max.y - 1)
                difference.y = upperPoint.y - (bounds.max.y - 1);   // positive (above)

            // Z Axis
            if (lowerPoint.z < bounds.min.z)
                difference.z = lowerPoint.z - bounds.min.z;         // negative (back)
            else if (upperPoint.z > bounds.max.z - 1)
                difference.z = upperPoint.z - (bounds.max.z - 1);   // positive (front)

            return difference;
        }

        /// <summary>
        /// Checks for a collision with another blueprint cell
        /// </summary>
        /// <param name="position">The position of the blueprint room</param>
        /// <param name="collidedBlueprint">If any room was found to collide then return the room otherwise will be null</param>
        /// <returns>Collided or not collided</returns>
        public static bool CheckCollision(MapGenerationContext context, Vector3Int position, out Blueprint collidedBlueprint)
        {
            return context.BlueprintDictionary.TryGetValue(position, out collidedBlueprint);
        }

        /// <summary>
        /// Checks for a collision with another blueprint cell
        /// </summary>
        /// <param name="position">The position of the blueprint room</param>
        /// <param name="collidedRoom">If any room was found to collide then return the room otherwise will be null</param>
        /// <returns>Collided or not collided</returns>
        public static bool CheckCollision(MapGenerationContext context, Vector3Int position)
        {
            return context.BlueprintDictionary.ContainsKey(position);
        }

        public static List<Blueprint> ToggleAvailableBlueprintsInRoom(MapGenerationContext context, Path path, List<RoomCell> roomCells, Vector3Int roomOrigin, bool available = true)
        {
            List<Blueprint> availibleBlueprints = new List<Blueprint>();

            // Set cells that are supposed to be available to available
            foreach (RoomCell cell in roomCells)
            {
                if (!cell.IsAvilable)   // Make sure cell is available
                    continue;

                Vector3Int cellPosition = roomOrigin + cell.Position;      // Find the actual position in room space of the cell

                if (context.BlueprintDictionary.TryGetValue(cellPosition, out Blueprint blueprint))
                {
                    availibleBlueprints.Add(blueprint);
                    blueprint.Available = available;
                }
                else
                    availibleBlueprints.Add(GenerateBlueprint(context, path, cellPosition, available));
            }

            return availibleBlueprints;
        }
        #endregion

        #region Debug
        public static void ToggleDebugLogs(bool toggle)
        {
            _debugLogs = toggle;
        }
        #endregion
    }
}
