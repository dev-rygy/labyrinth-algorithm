/*
 * Created By:      Ryan Carpenter
 * Date Created:    05/11/2025
 * Last Modified:   10/03/2025 (Ryan)
 * Notes:           Blueprint Generator
 *                  Handles all blueprint cell generation
 *                  Has many functions to generate blueprint rooms using
 *                  multiple techniques. Most common techniques are 
 *                  cached in BlueprintGenerator class
*/
using System.Collections;
using System.Collections.Generic;
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
    public class BlueprintRoom
    {
        public string RoomName { get; private set; }
        public Vector3Int Position { get; private set; }        // Position of blueprint room in room coords
        public bool Available { get; set; }
        public bool[] entrancewayFlags;

        // Constructor
        public BlueprintRoom(Vector3Int postion, string roomName = "Blueprint Room")
        {
            Available = true;
            RoomName = roomName;
            Position = postion;
            entrancewayFlags = new bool[6];       // A flag to mark which entrances should be open for a room
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
        private readonly Dictionary<Vector3Int, BlueprintRoom> _masterDictionaryReference;

        private bool _debugLogs = false;

        public BlueprintGenerator(Path masterPath, Dictionary<Vector3Int, BlueprintRoom> masterDictionary)
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
        public bool PlaceFixedUniqueRoomBlueprints(RoomEntry entry, Path path, BoundsInt bounds)
        {
            if (entry.Prefab.TryGetComponent(out Room room))      // Prefab in entry does not have a Room Component
            {
                // Adjust parameters to fit the zone's actual position
                Vector3Int zoneOffset = bounds.position;
                Vector3Int adjustedSpawnPos = entry.SpawnPosition + zoneOffset;

                // Check Collision with the zone's bounds
                Vector3 difference = CheckOutOfBounds(adjustedSpawnPos, room.RoomDimensions, bounds);
                if (difference != Vector3.zero)     // Room was outside the bounds of the zone
                {
                    Debug.LogError($"Blueprint Generator Error: Unique Room \"{room.name}\" was outside of bounds and could not be placed.\n" +
                        $"It was {difference} units outside the bounds of the zone.");
                    return false;
                }

                // TODO: Use hash map instead of List for faster lookup maybe?
                // Check Collision with other rooms
                List<BlueprintRoom> rooms;
                rooms = GenerateBlueprintsFromDimensions(path, adjustedSpawnPos, room.RoomDimensions, false);      // Fill room space with blueprint rooms

                if (rooms == null)     // Room was outside the bounds of the zone
                {
                    Debug.LogError($"Blueprint Generator Error: Unique Room \"{room.name}\" was obstructed and could not be placed");
                    return false;
                }

                // Set cells that are supposed to be available to available
                foreach (Vector3Int cell in entry.AvailableCells)
                {
                    Vector3Int cellPosition = adjustedSpawnPos + cell;      // Find the actual position in room space of the cell

                    if (_masterDictionaryReference.TryGetValue(cellPosition, out BlueprintRoom r))
                        r.Available = true;
                    else
                        GenerateBlueprintRoom(path, cellPosition, true);
                }

                return true;
            }
            Debug.LogError($"Map Generator Error: {entry.Prefab.name} does not have a Room script!");
            return false;
        }

        public bool PlaceBoundedUniqueRoomBlueprints(RoomEntry entry, Path path, BoundsInt bounds)
        {
            if (entry.Prefab.TryGetComponent<Room>(out Room room))      // Prefab in entry does not have a Room Component
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

                // Set cells that are supposed to be available to available
                foreach (Vector3Int cell in entry.AvailableCells)
                {
                    Vector3Int actualPos = spawnPosition + cell;      // Find the actual position in room space of the cell

                    if (_masterDictionaryReference.TryGetValue(actualPos, out BlueprintRoom r))
                        r.Available = true;
                    else
                        GenerateBlueprintRoom(path, actualPos, true);
                }
                return true;
            }
            Debug.LogError($"Map Generator Error: {entry.Prefab.name} does not have a Room script!");
            return false;
        }

        /// <summary>
        /// Will place rooms randomly in an zone but will pull rooms randomly from the main path.
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        public bool PlaceBoundedBlueprints(Path path, BoundsInt bounds, Vector3Int dimensions, out Vector3Int spawnPosition, bool available = true)
        {
            // Adjust the upper bounds so that the room's volume will properly fit within the bounded space
            Vector3Int adjUpperBound = new Vector3Int(
                bounds.xMax - dimensions.x,
                bounds.yMax - dimensions.y,
                bounds.zMax - dimensions.z
            );

            // Choose random spawn pos in the room's bounds;
            // NOTE: this random position is in room coords
            Vector3Int randomSpawnPos = new Vector3Int
            (
                Random.Range(bounds.xMin, adjUpperBound.x + 1),
                Random.Range(bounds.yMin, adjUpperBound.y + 1),
                Random.Range(bounds.zMin, adjUpperBound.z + 1)
            );

            // Append the newly generated blueprint rooms to the end of the list
            List<BlueprintRoom> newRooms = GenerateBlueprintsFromDimensions(path, randomSpawnPos, dimensions, available);

            spawnPosition = randomSpawnPos;

            // do not advance iteration if nothing was spawned
            if (newRooms == null)
                return false;

            return true;
        }
        #endregion

        #region Blueprint Graphs
        public DelaunayTriangulation3D GenerateTriangulationFromPath(Path path)
        {
            List<Vertex> vertices = new List<Vertex>();

            foreach (BlueprintRoom room in path.BlueprintRooms)
            {
                // if room is not available then forget about triangulating/pathfinding to it
                if (!room.Available)
                    continue;

                vertices.Add(new Vertex<BlueprintRoom>(room.Position, room));
            }

            // Perform Delaunay Triangulation
            return DelaunayTriangulation3D.Triangulate(vertices);
        }

        public List<Edge> FindMinimumSpanningTree(List<Edge> edges, Vertex startingVertex)
        {
            return PrimsAlgorithm.MinimumSpanningTree(edges, startingVertex);
        }

        public BlueprintRoom FindClosestRoomInPath(Path path, Vector3Int point)
        {
            if (path == null)
            {
                Debug.LogError("Blueprint Generator Error: Path was null.");
                return null;
            }

            BlueprintRoom closest = null;
            float distance = Mathf.Infinity;
            foreach (BlueprintRoom room in path.BlueprintRooms)
            {
                if (room.Available)
                {
                    float currentDistance = Vector3Int.Distance(point, room.Position);
                    if (currentDistance < distance)
                    {
                        closest = room;
                        distance = currentDistance;
                    }
                }
            }

            return closest;
        }
        #endregion

        #region Blueprint Pathfind
        public bool PathfindBlueprintFromPath(Path path, BoundsInt bounds, Vector3Int startPos, Vector3Int endPos, Heuristic heuristic = Heuristic.Euclidean)
        {
            SimpleAStar3D aStar = new SimpleAStar3D(bounds, bounds.position);

            // Add obstructions
            HashSet<Vector3Int> obstructions = new HashSet<Vector3Int>();
            foreach (BlueprintRoom room in path.BlueprintRooms)
            {
                // if the room is the start room or ending room of the edge then don't add to obstructions
                Vector3Int roomPos = room.Position;
                if (roomPos == startPos || roomPos == endPos)
                    continue;

                // Only make non-available blueprint rooms obstructions
                if (!room.Available)
                    obstructions.Add(roomPos);
            }

            // Find a sequence of points in room coordinates
            List<Vector3Int> sequence = aStar.FindPath(startPos, endPos, obstructions, heuristic);

            if (sequence == null)
            {
                Debug.LogError($"Blueprint Generator Error: Pathfinding failed for edge.");
                return false;
            }

            BlueprintRoom curRoom = null;
            BlueprintRoom prevRoom = null;
            foreach (Vector3Int pos in sequence)
            {
                if (_masterDictionaryReference.TryGetValue(pos, out var occupiedRoom))
                {
                    // Do not generate blueprint rooms if the space is already occupied
                    // Make the occupied blueprint room the currentRoom instead
                    curRoom = occupiedRoom;
                }
                else
                    curRoom = GenerateBlueprintRoom(path, pos);

                if (prevRoom == null)
                {
                    prevRoom = curRoom;
                    continue;
                }

                // Flag doorways of blueprint rooms
                Vector3Int difference = curRoom.Position - prevRoom.Position;
                FlagDoorways(curRoom, prevRoom, difference);

                prevRoom = curRoom;
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
        public bool BlueprintDrunkardWalk(Path path, BoundsInt bounds, BlueprintRoom startRoom)
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

            if (startRoom == null)
            {
                Debug.LogError($"Map Generator Error: Starting Room for Drunkard Walk cannot be null.");
                return false;
            }

            // Extend master path's end index
            _masterPathReference.endMasterIdx = path.endMasterIdx;

            if (_debugLogs) Debug.Log($"Map Generator: Starting cell for path {path.name} generated as {startRoom.RoomName}");

            // Attempt to place path
            if (!BlueprintDrunkardWalkRecursive(path, bounds, startRoom))
            {
                if (_debugLogs) Debug.LogWarning($"Map Generator Warning: Path generation failed recursive algorithm at {startRoom.RoomName}");
                return false;
            }

            return true;
        }

        /* OLD RECURSIVE ALG (DEPRICATED)
        int _stackFrames = 0;
        private BlueprintRoom BlueprintDrunkardWalkRecursive(Path path, BoundsInt bounds, BlueprintRoom prevRoom)
        {
            // Failsafe ****************
            _stackFrames++;
            if (_stackFrames >= 1000)
            {
                Debug.LogError("Too many stack frames, stopping");
                return null;
            }
            // *****************************

            if (path.BlueprintCount() > path.PathLength)
            {
                return prevRoom;
            }

            // Attempt to place a new room
            BlueprintRoom newRoom = AttemptPlaceRoomRandom(path, bounds, prevRoom);

            if (newRoom != null)    // New room was placed -> place next room
            {
                BlueprintRoom nextRoom = BlueprintDrunkardWalkRecursive(path, bounds, newRoom);

                if (nextRoom == null)       // next room could not be placed? Continuation of path failed -> try prev room again
                {
                    _stackFrames--;
                    return BlueprintDrunkardWalkRecursive(path, bounds, prevRoom);          // Backtrack
                }
                else
                {
                    _stackFrames--;
                    return prevRoom;
                }
            }
            _stackFrames--;
            return null;    // No room could be placed
        }
        */

        private bool BlueprintDrunkardWalkRecursive(Path path, BoundsInt bounds, BlueprintRoom prevRoom)
        {
            if (path.BlueprintCount() > path.PathLength)
                return true;

            // Attempt to place a new room
            BlueprintRoom newRoom = PlaceBlueprintInRandomDirection(path, bounds, prevRoom);

            if (newRoom != null)    // New room was placed -> place next room
            {
                bool placed = BlueprintDrunkardWalkRecursive(path, bounds, newRoom);

                if (!placed)       // next room could not be placed? Continuation of path failed -> try prev room again
                    return BlueprintDrunkardWalkRecursive(path, bounds, prevRoom);          // Backtrack
                else
                    return true;
            }
            return false;    // No room could be placed
        }

        private BlueprintRoom PlaceBlueprintInRandomDirection(Path path, BoundsInt bounds, BlueprintRoom prevRoom)
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

                Vector3Int tempPos = prevRoom.Position + GetDirectionFromIndex(directionalIndex);

                // Debug.Log($"Attempting placement at {tempPos}");

                // Check position in hash map; if failed then flag face attempt and try choosing a new position 
                if (!CheckOutOfBounds(tempPos, bounds) && !CheckCollision(tempPos))     // If position is not out of bounds and not colliding with another room
                {
                    // Return new blueprint room
                    BlueprintRoom newBlueRoom = GenerateBlueprintRoom(path, tempPos);
                    FlagDoorways(newBlueRoom, prevRoom, directionalIndex);                    // Flag the face that touches the opposite room

                    return newBlueRoom;
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
        /// <param name="position">The desired position to spawn the new room at. Must be in world coords</param>
        /// <returns>Blueprint room created in room coords.</returns>
        public BlueprintRoom GenerateBlueprintRoom(Path path, Vector3Int position, bool available = true)
        {
            string blueName = $"BlueprintRoom ({_masterPathReference.BlueprintCount()})";
            BlueprintRoom newRoom = new BlueprintRoom(position, blueName);
            newRoom.Available = available;

            if (_debugLogs) Debug.Log($"Generated blueprint room {blueName}");

            // Update paths and masters with new blueprint room
            path?.Add(newRoom);
            _masterPathReference?.Add(newRoom);                    // Add to Master List (required)
            _masterDictionaryReference?.Add(position, newRoom);    // Add to Master Dictionary (required)
            return newRoom;
        }

        /// <summary>
        /// Generates a variety of blueprint rooms based on a given dimension starting at a point
        /// If a collision occurs then the method returns a null list.
        /// position and dimensions must be in room coordinates!
        /// </summary>
        /// <param name="path">Path to add blueprint rooms to</param>
        /// <param name="position">Start position</param>
        /// <param name="roomDimensions"></param>
        /// <returns>Blueprint rooms generated in a list if needed.</returns>
        public List<BlueprintRoom> GenerateBlueprintsFromDimensions(Path path, Vector3Int position, Vector3Int roomDimensions, bool available = true)
        {
            List<BlueprintRoom> rooms = new List<BlueprintRoom>();
            List<Vector3Int> roomOrigins = new List<Vector3Int>();

            for (int x = position.x; x < (position.x + roomDimensions.x); x++)      // traverse x dimensions
            {
                for (int y = position.y; y < (position.y + roomDimensions.y); y++)      // traverse y dimensions
                {
                    for (int z = position.z; z < (position.z + roomDimensions.z); z++)      // traverse z dimensions
                    {
                        Vector3Int origin = new Vector3Int(x, y, z);

                        if (CheckCollision(origin, out BlueprintRoom collidedRoom))
                        {
                            if (_debugLogs) 
                                Debug.LogWarning($"Map Generator Warning: Failed to generate blueprint room due to collision with {collidedRoom.RoomName}");
                            return null;
                        }

                        roomOrigins.Add(origin);
                    }
                }
            }

            // If no errors then generate blueprint rooms from dimensions
            foreach (Vector3Int spawnPosition in roomOrigins)
                rooms.Add(GenerateBlueprintRoom(path, spawnPosition, available));      // Call to method above

            return rooms;
        }

        public void FlagDoorways(BlueprintRoom roomA, BlueprintRoom roomB, Vector3Int difference)
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

            FlagDoorways(roomA, roomB, entrFlagIdx);
        }

        /// <summary>
        /// Pass in two rooms and link their entrancways together. 
        /// </summary>
        /// <param name="roomA">First blueprint room</param>
        /// <param name="roomB">Second blueprint room</param>
        /// <param name="entrFlagIdx">The index of the choosen face of the *first* room.</param>
        public void FlagDoorways(BlueprintRoom roomA, BlueprintRoom roomB, int entrFlagIdx) // Flag the entranceways to be activated in each room
        {
            if (entrFlagIdx < 0)
            {
                Debug.LogError("Map Generator Error: Two rooms are invalid for entrance connection");
                return;
            }

            // Flag the fact of the next room facing the prev. room
            if (Math.IsEven(entrFlagIdx))                                   // If choosen an even numbered side then set opposite to true (Ex. F4 -> F3 = true)
                roomA.entrancewayFlags[entrFlagIdx + 1] = true;
            else                                                            // If choosen an odd numbered side then set opposite to true (Ex. F3 -> F4 = true)
                roomA.entrancewayFlags[entrFlagIdx - 1] = true;

            // Flag the face of the prev. room facing the next room
            roomB.entrancewayFlags[entrFlagIdx] = true;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Choose a random room in a path. If endIndex = -1 => endIndex = path's last room.
        /// </summary>
        /// <param name="path">The path to choose the starting room from</param>
        /// <param name="startIndex">Index to start from</param>
        /// <returns>The Choosen Blueprint Room.</returns>
        public BlueprintRoom ChooseRandomRoomInPath(Path path, int startIndex = 0, int endIndex = -1)
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
            int randomRoomIndex = Random.Range(startIndex, endIndex);
            BlueprintRoom room = path.BlueprintRooms[randomRoomIndex];

            // TODO: Make a circular array handle this
            if (!room.Available)
            {
                //Debug.LogWarning("Map Generator Warning: unavailable room choosen for path start. Choosing a new room...");
                room = ChooseRandomRoomInPath(path, startIndex, endIndex);
            }

            if (_debugLogs) Debug.Log($"Map Generator: Random room choosen from {path.Name} at index {randomRoomIndex}");

            return room;
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

            /* OLD CODE 
            Vector3Int lowerDiff = bounds.min - lowerPoint;
            Vector3Int upperDiff = bounds.max - upperPoint;

            if (lowerDiff.x > 0 || lowerDiff.y > 0 || lowerDiff.z > 0)      // Invalid Space (Top Right)
            {
                return new Vector3Int(
                   lowerDiff.x > 0 ? lowerDiff.x : 0,      // Return only the positive components of the lowerDiff
                   lowerDiff.y > 0 ? lowerDiff.y : 0,
                   lowerDiff.z > 0 ? lowerDiff.z : 0);
            }

            if (upperDiff.x < 0 || upperDiff.y < 0 || upperDiff.z < 0)      // Invalid Space (Bot Left)
            {
                return new Vector3Int(
                   upperDiff.x < 0 ? upperDiff.x : 0,      // Return only the negative components of the lowerDiff
                   upperDiff.y < 0 ? upperDiff.y : 0,
                   upperDiff.z < 0 ? upperDiff.z : 0);
            }

            return Vector3Int.zero;        // Valid Space
            */
        }

        /// <summary>
        /// Checks for a collision with another blueprint cell
        /// </summary>
        /// <param name="position">The position of the blueprint room</param>
        /// <param name="collidedRoom">If any room was found to collide then return the room otherwise will be null</param>
        /// <returns>Collided or not collided</returns>
        public bool CheckCollision(Vector3Int position, out BlueprintRoom collidedRoom)
        {
            return _masterDictionaryReference.TryGetValue(position, out collidedRoom);
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
