/*
 * Created By:      Ryan Carpenter
 * Date Created:    05/11/2025
 * Last Modified:   10/23/2025 (Ryan)
 * Notes:           Blueprint Generator
 *                  Handles all blueprint cell generation
 *                  Has many functions to generate blueprint rooms using
 *                  multiple techniques. Most common techniques are 
 *                  cached in BlueprintGenerator class
*/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;  // Use Unity Engine's Random not System.Collection's Random

using RyansLibrary.AI;
using RyansLibrary.Graphs;
using RyansLibrary.Utils;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Holds the properties of a psuedo room that does not actually exist in the world.
    /// Is meant to be parsed and replaced by actual rooms later on.
    /// </summary>
    public class Blueprint
    {
        public readonly string CellID;
        public readonly Vector3Int Position;      // Position of blueprint room in room coords
        public bool Available { get; set; }
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

    public class BlueprintGenerator
    {
        // Amount of faces on a blueprint room; This should never be changed unless unique shaped rooms are made in the future
        const int STANDARD_FACE_COUNT = 6;

        // ***** Master References *****
        // The Master Path holds a reference to all bluprint rooms generated
        private readonly Path _masterPathReference;
        // Master Dictionary used for quick access like checking locations for conflicts and checking locations for room shape conditions.
        // Holds reference to all blueprint rooms
        // Keys are in room coords
        private readonly Dictionary<Vector3Int, Blueprint> _masterDictionaryReference;

        private bool _debugLogs = false;

        public BlueprintGenerator(Path masterPath, Dictionary<Vector3Int, Blueprint> masterDictionary)
        {
            _masterPathReference = masterPath;
            _masterDictionaryReference = masterDictionary;
        }

        #region Unique Blueprint Placement
        /// <summary>
        /// Places fixed room within the bounds of an zone; returns false if the
        /// room could not be placed correctly.
        /// </summary>
        /// <param name="entry">Room Entry</param>
        /// <param name="path">Path to store blueprints in</param>
        /// <param name="bounds">Bounds to constrict placement</param>
        /// <returns></returns>
        public bool PlaceFixedUniqueRoomBlueprints(Path path, RoomEntry entry, BoundsInt bounds)
        {
            if (entry.Prefab.TryGetComponent(out Room room))      // Prefab in entry does not have a Room Component
            {
                // Adjust parameters to fit the zone's actual position
                Vector3Int zoneOffset = bounds.position;
                Vector3Int roomOrigin = entry.SpawnPosition + zoneOffset;

                // Check Collision with the zone's bounds
                Vector3 difference = CheckOutOfBounds(roomOrigin, room.RoomDimensions, bounds);
                if (difference != Vector3.zero)     // Room was outside the bounds of the zone
                {
                    Debug.LogError($"Blueprint Generator Error: Unique Room \"{room.name}\" was outside of bounds and could not be placed.\n" +
                        $"It was {difference} units outside the bounds of the zone.");
                    return false;
                }

                // TODO: Use hash map instead of List for faster lookup maybe?
                // Check Collision with other rooms
                List<Blueprint> blueprintList;
                blueprintList = GenerateBlueprintsFromDimensions(path, roomOrigin, room.RoomDimensions, false);      // Fill room space with blueprint rooms
                if (blueprintList is null)     // Room was outside the bounds of the zone
                {
                    Debug.LogError($"Blueprint Generator Error: Unique Room \"{room.name}\" was obstructed and could not be placed");
                    return false;
                }

                List<Blueprint> availableBlueprints = ToggleAvailableCellsInUniqueRoom(path, entry.AvailableCells, roomOrigin);
                if (availableBlueprints is null)
                {
                    Debug.LogError($"Blueprint Generator Error: Unique Room \"{room.name}\" has no available blueprint cells.");
                    return false;
                }

                return true;
            }
            Debug.LogError($"Map Generator Error: {entry.Prefab.name} does not have a Room script!");
            return false;
        }

        public bool PlaceBoundedUniqueRoomBlueprints(Path path, RoomEntry entry, BoundsInt bounds)
        {
            if (entry.Prefab.TryGetComponent(out Room room))      // Prefab in entry does not have a Room Component
            {
                bool result = PlaceBoundedBlueprints(path, bounds, room.RoomDimensions, out Vector3Int spawnPosition, false);

                if (!result)
                {
                    if (_debugLogs)
                    {
                        Debug.LogWarning($"Map Generator Warning: Constrained Room {entry.Prefab.name} " +
                            $"collided with another room and could not be placed. Retrying...");
                    }
                    return false;
                }

                entry.SpawnPosition = spawnPosition;

                List<Blueprint> availableBlueprints = ToggleAvailableCellsInUniqueRoom(path, entry.AvailableCells, spawnPosition);
                if (availableBlueprints is null)
                {
                    Debug.LogError($"Blueprint Generator Error: Unique Room \"{room.name}\" has no available blueprint cells.");
                    return false;
                }

                return true;
            }
            Debug.LogError($"Map Generator Error: {entry.Prefab.name} does not have a Room script!");
            return false;
        }

        public List<Blueprint> ToggleAvailableCellsInUniqueRoom(Path path, List<Vector3Int> availableCells, Vector3Int roomOrigin, bool available = true)
        {
            List<Blueprint> availibleBlueprints = new List<Blueprint>();

            // Set cells that are supposed to be available to available
            foreach (Vector3Int cell in availableCells)
            {
                Vector3Int cellPosition = roomOrigin + cell;      // Find the actual position in room space of the cell

                if (_masterDictionaryReference.TryGetValue(cellPosition, out Blueprint blueprint))
                {
                    availibleBlueprints.Add(blueprint);
                    blueprint.Available = available;
                }
                else
                    availibleBlueprints.Add(GenerateBlueprintRoom(path, cellPosition, available));
            }

            return availibleBlueprints;
        }

        public bool PlaceDivergentBlueprints(Path path, BoundsInt bounds, Vector3Int dimensions, int cellCount, int maxPlacementAttempts)
        {
            int indexOffset = dimensions.x * dimensions.y;      // Increment by the cells taken up from the room dimensions

            if (cellCount % indexOffset != 0)       // Not all cells desired can fit within the dimensions of the divergent rooms
            {
                Debug.LogError($"Map Generator Error: The path's disired cell spawn exceeds/falls short of the divergent " +
                    $"rooms with dimensions ({dimensions.x},{dimensions.y},{dimensions.z}).");
                return false;
            }

            for (int i = 0; i < cellCount; i += indexOffset)
            {
                bool successfullyPlaced = false;
                int placementAttempts = 0;

                while (!successfullyPlaced && placementAttempts < maxPlacementAttempts)
                {
                    // Attempt to spawn blueprints
                    successfullyPlaced = PlaceBoundedBlueprints(path, bounds, Vector3Int.one, out Vector3Int spawnPos);

                    if (!successfullyPlaced)     // Failed placement
                        placementAttempts++;        // Increase attempts
                }

                // If divergent room failed to generate a certain number of times then return false
                if (!successfullyPlaced)
                {
                    Debug.LogError($"Map Generator Error: A divergent room in path {path.name} has exhaused all of it's placement attempts.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Will place rooms randomly in an zone but will pull rooms randomly from the main path.
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        public bool PlaceBoundedBlueprints(Path path, BoundsInt bounds, Vector3Int dimensions, out Vector3Int spawnPosition, bool available = true)
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
            List<Blueprint> newBlueprints = GenerateBlueprintsFromDimensions(path, randomSpawnPos, dimensions, available);

            spawnPosition = randomSpawnPos;

            // do not advance iteration if nothing was spawned
            if (newBlueprints == null)
                return false;

            return true;
        }
        #endregion

        #region Blueprint Graphs
        // TODO: make a graph class/struct that can hold edges and return that instead of a DelaunayTriangulation3D value
        public List<Edge> GenerateTriangulationFromPath(Path path)
        {
            // Make a new list and remove all rooms that are not available
            List<Blueprint> availableBlueprints = path.BlueprintList.Where(bp => bp.Available).ToList();

            return GenerateTriangulation(availableBlueprints);
        }

        // TODO: make a graph class/struct that can hold edges and return that instead of a DelaunayTriangulation3D value
        public List<Edge> GenerateTriangulation(List<Blueprint> blueprintList)
        {
            List<Vertex> waypoints = new List<Vertex>();

            foreach (Blueprint blueprint in blueprintList)
            {
                waypoints.Add(new Vertex<Blueprint>(blueprint.Position, blueprint));
            }

            DelaunayTriangulation3D triangulation = DelaunayTriangulation3D.Triangulate(waypoints);

            return triangulation.Edges;
        }

        public List<Edge> FindMinimumSpanningTree(List<Edge> edges)
        {
            Vertex startingVertex = edges[0].U;

            return PrimsAlgorithm.MinimumSpanningTree(edges, startingVertex);
        }
        #endregion

        #region Blueprint Pathfind
        public bool PathfindBlueprintFromPath(Path path, BoundsInt bounds, Blueprint startBlueprint, Blueprint endBlueprint, List<Blueprint> obstructions, 
            Heuristic heuristic = Heuristic.Euclidean)
        {
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
                if (_masterDictionaryReference.TryGetValue(pos, out var occupiedRoom))
                {
                    // Do not generate blueprint rooms if the space is already occupied
                    // Make the occupied blueprint room the currentRoom instead
                    currentBlueprint = occupiedRoom;
                }
                else
                    currentBlueprint = GenerateBlueprintRoom(path, pos);

                if (previousBlueprint == null)
                {
                    previousBlueprint = currentBlueprint;
                    continue;
                }

                // Flag doorways of blueprint rooms
                Vector3Int difference = currentBlueprint.Position - previousBlueprint.Position;
                FlagEntryPoints(currentBlueprint, previousBlueprint, difference);

                previousBlueprint = currentBlueprint;
            }
            return true;
        }
        #endregion

        #region Blueprint Random
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
            Func<int, int> circularIncrement = x => (x < endIndex + 1) ? ++x : x = startIndex;
            for (int i = randomStartingIndex; i != randomStartingIndex - 1; i = circularIncrement(i))
            {
                // Choose new start room
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
            Blueprint newBlueprint = PlaceBlueprintInRandomDirection(path, bounds, previousBlueprint);

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

        private Blueprint PlaceBlueprintInRandomDirection(Path path, BoundsInt bounds, Blueprint previousBlueprint)
        {
            // Chose a position in a random cardinal direction and check for collisions
            bool[] attempts = new bool[STANDARD_FACE_COUNT];
            int failedAttempts = 0;

            while (failedAttempts < STANDARD_FACE_COUNT)
            {
                // Choose a random direction to be the potential position for the next room.
                int directionalIndex = Random.Range(0, STANDARD_FACE_COUNT);
                directionalIndex = ArrayUtils.FindIndexCircular<bool>(attempts, directionalIndex, x => x == false);

                if (directionalIndex < 0)
                    return null;

                Vector3Int tempPos = previousBlueprint.Position + GetDirectionFromIndex(directionalIndex);

                // Check position in hash map; if failed then flag face attempt and try choosing a new position 
                if (!CheckOutOfBounds(tempPos, bounds) && !CheckCollision(tempPos))     // If position is not out of bounds and not colliding with another room
                {
                    // Return new blueprint room
                    Blueprint newBlueprint = GenerateBlueprintRoom(path, tempPos);
                    FlagEntryPoints(newBlueprint, previousBlueprint, directionalIndex);                    // Flag the face that touches the opposite room

                    return newBlueprint;
                }

                attempts[directionalIndex] = true;
                failedAttempts++;
            }

            return null;
        }

        private Vector3Int GetDirectionFromIndex(int index)
        {
            switch (index)
            {
                // E0 - E5 is the face count for a unit room, this will be used later for entranceways
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
                    Debug.LogError("Map Generator Error: Direction choosen does not exist.");
                    return Vector3Int.zero;
            }
        }
        #endregion

        #region Blueprint Room Generation
        /// <summary>
        /// Generate a new blueprint room at the desired location. Add it to the master path and
        /// desired path passed in as an arguement. Generate a blueprint room gizmo if debug is enabled.
        /// NOTE: Position must be in room coords
        /// </summary>
        /// <param name="path">The desired path to add the new blueprint room to.</param>
        /// <param name="origin">The desired position to spawn the new room at. Must be in world coords</param>
        /// <returns>Blueprint room created in room coords.</returns>
        public Blueprint GenerateBlueprintRoom(Path path, Vector3Int origin, bool available = true)
        {
            string blueprintName = $"BlueprintRoom ({_masterPathReference.BlueprintCount()})";
            Blueprint newBlueprint = new Blueprint(origin, blueprintName);
            newBlueprint.Available = available;

            if (_debugLogs) Debug.Log($"Generated blueprint room {blueprintName}");

            // Update paths and masters with new blueprint room
            path?.Add(newBlueprint);
            _masterPathReference?.Add(newBlueprint);                    // Add to Master List (required)
            _masterDictionaryReference?.Add(origin, newBlueprint);    // Add to Master Dictionary (required)
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
        public List<Blueprint> GenerateBlueprintsFromDimensions(Path path, Vector3Int origin, Vector3Int roomDimensions, bool available = true)
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

                        if (CheckCollision(blueprintroomPos, out Blueprint collidedBlueprint))
                        {
                            if (_debugLogs) 
                                Debug.LogWarning($"Map Generator Warning: Failed to generate blueprint room due to collision with {collidedBlueprint.CellID}");
                            return null;
                        }

                        blueprintroomPositions.Add(blueprintroomPos);
                    }
                }
            }

            // If no errors then generate blueprint rooms from dimensions
            foreach (Vector3Int spawnPosition in blueprintroomPositions)
                roomBlueprints.Add(GenerateBlueprintRoom(path, spawnPosition, available));      // Call to method above

            return roomBlueprints;
        }

        public void FlagEntryPoints(Blueprint blueprintA, Blueprint blueprintB, Vector3Int difference)
        {
            // TODO: Bad way of handling this. Find a better way
            int entrFlagIdx;
            if (difference == Vector3Int.right)
                entrFlagIdx = 0;
            else if (difference == Vector3Int.left)
                entrFlagIdx = 1;
            else if (difference == Vector3Int.forward)
                entrFlagIdx = 2;
            else if (difference == Vector3Int.back)
                entrFlagIdx = 3;
            else if (difference == Vector3Int.up)
                entrFlagIdx = 4;
            else if (difference == Vector3Int.down)
                entrFlagIdx = 5;
            else
                entrFlagIdx = -1;   // Default or error case

            FlagEntryPoints(blueprintA, blueprintB, entrFlagIdx);
        }

        /// <summary>
        /// Pass in two rooms and link their entrancways together. 
        /// </summary>
        /// <param name="blueprintA">First blueprint room</param>
        /// <param name="blueprintB">Second blueprint room</param>
        /// <param name="entrFlagIdx">The index of the choosen face of the *first* room.</param>
        public void FlagEntryPoints(Blueprint blueprintA, Blueprint blueprintB, int entrFlagIdx) // Flag the entranceways to be activated in each room
        {
            if (entrFlagIdx < 0)
            {
                Debug.LogError("Map Generator Error: Two rooms are invalid for entrance connection");
                return;
            }

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
        public Blueprint ChooseRandomBlueprintInPath(Path path, int startIndex = 0, int endIndex = -1)
        {
            // Default the endIndex to the path's end index
            if (endIndex == -1)
                endIndex = path.BlueprintCount() - 1;

            // Check if range is valid
            if ((startIndex < 0) || (startIndex > endIndex) || (endIndex > (path.BlueprintCount() - 1)))
            {
                Debug.LogError("Map Generator Error: Path index out of range or set incorrectly.");
                return null;
            }

            // Check if path to choose from is valid
            if (path.BlueprintCount() <= 0)
            {
                Debug.LogError($"Map Generator Error: A starting room could not be choosen because {path.Name} has no rooms.");
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

            if (_debugLogs) Debug.Log($"Map Generator: Random room choosen from {path.Name} at index {randomBlueprintListIndex}");

            return blueprint;
        }

        public Blueprint FindClosestBlueprintInPath(Path path, Vector3Int point)     // UNUSED
        {
            if (path == null)
            {
                Debug.LogError("Blueprint Generator Error: Path was null.");
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

        public List<Blueprint> FindBlueprintsWithAvailibility(List<Blueprint> blueprintList, bool availibility)
        {
            return new List<Blueprint>(blueprintList.Where(b => (b.Available == availibility)).ToList());
        }

        public Blueprint FindFirstBlueprintWithAvailibility(List<Blueprint> blueprintList, bool availibility)
        {
            return blueprintList.FirstOrDefault(b => (b.Available == availibility));
        }

        public BoundsInt CombineBounds(BoundsInt boundsA, BoundsInt boundsB)
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

        public BoundsInt CreateIntersectingBounds(BoundsInt intersectedBounds, Vector3Int size, Vector3Int offset)
        {
            Vector3Int position = intersectedBounds.min + offset;

            Vector3Int amountOutOfBounds = CheckOutOfBounds(position, size, intersectedBounds);
            if (amountOutOfBounds != Vector3.zero)
            {
                Debug.LogWarning("Map Generator Warning: Desired intersecting bounds lies outside the overarching bounds. Adjusting size...");
                size -= amountOutOfBounds;
            }

            return new BoundsInt(position, size);
        }

        public BoundsInt CreateIntersectingBounds(BoundsInt intersectedBounds, BoundsInt intersectingBounds)
        {
            return CreateIntersectingBounds(intersectedBounds, intersectingBounds.size, intersectingBounds.position);
        }

        /// <summary>
        /// Check if a point lies outside the bounds of the zone.
        /// *** The point must be in world coords ***
        /// </summary>
        /// <param name="desiredPosition">The desired position to spawn the next room</param>
        /// <returns>Returns TRUE if the space is out of bounds and FALSE otherwise.</returns>
        public bool CheckOutOfBounds(Vector3Int desiredPosition, Vector3Int upperBound, Vector3Int lowerBound)
        {
            Vector3Int differenceUpper = upperBound - desiredPosition;
            Vector3Int differenceLower = lowerBound - desiredPosition;
            if (differenceUpper.x <= 0 || differenceUpper.y <= 0 || differenceUpper.z <= 0)        // Valid space
                return false;
            if (differenceLower.x > 0 || differenceLower.y > 0 || differenceLower.z > 0)        // Valid space
                return false;

            return true;           // Invalid space
        }

        public bool CheckOutOfBounds(Vector3Int desiredPos, BoundsInt bounds)
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
        public Vector3Int CheckOutOfBounds(Vector3Int origin, Vector3Int dimensions, BoundsInt bounds)
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
        public bool CheckCollision(Vector3Int position, out Blueprint collidedBlueprint)
        {
            return _masterDictionaryReference.TryGetValue(position, out collidedBlueprint);
        }

        /// <summary>
        /// Checks for a collision with another blueprint cell
        /// </summary>
        /// <param name="position">The position of the blueprint room</param>
        /// <param name="collidedRoom">If any room was found to collide then return the room otherwise will be null</param>
        /// <returns>Collided or not collided</returns>
        public bool CheckCollision(Vector3Int position)
        {
            return _masterDictionaryReference.ContainsKey(position);
        }
        #endregion

        #region Debug
        public void ToggleDebugLogs(bool toggle)
        {
            _debugLogs = toggle;
        }
        #endregion
    }
}
