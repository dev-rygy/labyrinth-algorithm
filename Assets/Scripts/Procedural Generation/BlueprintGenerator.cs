/*
 * Created By:      Ryan Carpenter
 * Date Created:    05/11/2025
 * Last Modified:   05/12/2025 (Ryan)
 * Notes:           Blueprint Generator
*/
using System;
using System.Collections.Generic;
using UnityEngine;

using RyansLibrary.Graphs;
using RyansLibrary.AI;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Holds the properties of a suedo room that does not actually exist in the world.
    /// Is meant to be replaced by actual rooms later on.
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
        const int STANDARD_ROOM_FACE_COUNT = 6;

        // ***** Path Containers *****
        // The Master Path holds a reference to all bluprint rooms in an zone
        public Path MasterPath { get; private set; }

        // Dictionary used for quick access like checking locations for conflicts and checking locations for room shape conditions
        // Keys are in room coords
        public Dictionary<Vector3Int, BlueprintRoom> MasterDictionary { get; private set; }

        // TODO: Remove later
        [SerializeField] private int _numOfPlacementAttempsBeforeRegen = 10;    // If this number is exceeded then the generator will refresh its entire generation attempt

        private bool _debugGizmos = true;
        private bool _debugLogs = false;

        public BlueprintGenerator(Path masterPath, Dictionary<Vector3Int, BlueprintRoom> masterDictionary)
        {
            MasterPath = masterPath;
            MasterDictionary = masterDictionary;
        }

        #region Unique/Divergent Room Blueprints
        public bool PlaceUniqueRooms(Zone zone)
        {
            // 1.) Spawn Fixed Rooms
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                if (entry.PlacementType == RoomPlacementType.Fixed)
                {
                    bool hasPlaced = PlaceFixedUniqueRoomBlueprints(entry, zone.MainPath, zone.Bounds);

                    if (!hasPlaced)
                    {
                        // Fixed room failed to generate, stop all operations
                        Debug.LogError($"Map Generator Error: Fixed Room was outside of bounds and could not be placed.");
                        return false;
                    }
                }
            }

            // 2.) Spawn Constrained Rooms
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                bool hasPlaced = false;
                int attempts = 0;

                if (entry.PlacementType == RoomPlacementType.Constrained)
                {
                    // Attempt to place the constrained room in it's bounded zone; if not then break the function
                    // and return false
                    while (!hasPlaced)
                    {
                        if (attempts++ > _numOfPlacementAttempsBeforeRegen)
                        {
                            // TODO: Clear all data and regenerate the map
                            Debug.LogWarning("Map Generator Warning: Constrained Room has exceeded the maximum number of placement attempts.");
                            return false;
                        }

                        hasPlaced = PlaceBoundedUniqueRoomBlueprints(entry, zone.MainPath, entry.Bounds);
                    }
                }
            }

            // 3.) Spawn Free Rooms
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                bool hasPlaced = false;
                int attempts = 0;

                if (entry.PlacementType == RoomPlacementType.Free)
                {
                    // Place Free Rooms
                    // Attempt to place the constrained room in it's bounded zone; if not then break the function
                    // and return false
                    while (!hasPlaced)
                    {
                        if (attempts++ > _numOfPlacementAttempsBeforeRegen)
                        {
                            // TODO: Clear all data and regenerate the map
                            Debug.LogWarning("Map Generator Warning: Constrained Room has exceeded the maximum number of placement attempts.");
                            return false;
                        }

                        hasPlaced = PlaceBoundedUniqueRoomBlueprints(entry, zone.MainPath, zone.Bounds);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Place fixed room within the bounds of an zone; returns false if the
        /// room could not be placed correctly
        /// </summary>
        /// <param name="room"></param>
        /// <param name="upperBound"></param>
        /// <param name="lowerBound"></param>
        /// <param name="placementPoint"></param>
        /// <returns></returns>
        public bool PlaceFixedUniqueRoomBlueprints(RoomEntry entry, Path path, BoundsInt bounds)
        {
            if (entry.Prefab.TryGetComponent<Room>(out Room room))      // Prefab in entry does not have a Room Component
            {
                // Adjust parameters to fit the zone's actual position
                Vector3Int zoneOffset = bounds.position;
                Vector3Int adjustedSpawnPos = entry.SpawnPosition + zoneOffset;

                // Check Collision with the zone's bounds
                Vector3 difference = CheckOutOfBounds(adjustedSpawnPos, room.RoomDimensions, bounds);

                if (difference != Vector3.zero)     // Room was outside the bounds of the zone
                {
                    Debug.LogError($"Map Generator Error: Unique Room \"{room.name}\" was outside of bounds and could not be placed.\n" +
                        $"It was {difference} units outside the bounds of the zone.");
                    return false;
                }

                // TODO: Use hash map instead of List for faster lookup maybe?
                // Check Collision with other rooms
                List<BlueprintRoom> rooms;
                rooms = GenerateBlueprintsFromDimensions(path, adjustedSpawnPos, room.RoomDimensions, false);      // Fill room space with blueprint rooms

                if (rooms == null)     // Room was outside the bounds of the zone
                {
                    Debug.LogError($"Map Generator Error: Unique Room \"{room.name}\" was obstructed and could not be placed");
                    return false;
                }

                // Set cells that are supposed to be available to available
                foreach (Vector3Int cell in entry.AvailableCells)
                {
                    Vector3Int cellPosition = adjustedSpawnPos + cell;      // Find the actual position in room space of the cell

                    if (MasterDictionary.TryGetValue(cellPosition, out BlueprintRoom r))
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
                    Debug.LogWarning($"Map Generator Error: Constrained Room {entry.Prefab.name} collided with another room and could not be placed");
                    return false;
                }

                entry.SpawnPosition = spawnPosition;

                // Set cells that are supposed to be available to available
                foreach (Vector3Int cell in entry.AvailableCells)
                {
                    Vector3Int actualPos = spawnPosition + cell;      // Find the actual position in room space of the cell

                    if (MasterDictionary.TryGetValue(actualPos, out BlueprintRoom r))
                        r.Available = true;
                    else
                        GenerateBlueprintRoom(path, actualPos, true);
                }
                return true;
            }
            Debug.LogError($"Map Generator Error: {entry.Prefab.name} does not have a Room script!");
            return false;
        }

        public bool PlaceDivergentRooms(Zone zone)
        {
            Path mainPath = zone.MainPath;
            int occupancy = zone.DivergentRoomsCellOccupancy;
            int indexOffset = 1;

            for (int i = 0; i < occupancy; i += indexOffset)
            {
                bool result = PlaceBoundedBlueprints(zone.MainPath, zone.Bounds, Vector3Int.one, out Vector3Int spawnPos);

                if (result)
                    indexOffset = 1;
                else
                    indexOffset = 0;
            }

            return true;
        }

        /// <summary>
        /// Will place rooms randomly in an zone but will pull rooms randomly from the main path
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
                UnityEngine.Random.Range(bounds.xMin, adjUpperBound.x + 1),
                UnityEngine.Random.Range(bounds.yMin, adjUpperBound.y + 1),
                UnityEngine.Random.Range(bounds.zMin, adjUpperBound.z + 1)
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
        #endregion

        #region Blueprint Pathfind
        public void PathfindBlueprint(Path path, BoundsInt bounds, Vector3Int startPos, Vector3Int endPos)
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

                obstructions.Add(roomPos);
            }

            // Find a sequence of points in room coordinates
            List<Vector3Int> sequence = aStar.FindPath(startPos, endPos, obstructions, Heuristic.Manhattan);

            if (sequence == null)
            {
                Debug.LogError($"Blueprint Generator Error: Pathfinding failed for edge.");
                return;
            }

            BlueprintRoom curRoom = null;
            BlueprintRoom prevRoom = null;
            foreach (Vector3Int pos in sequence)
            {
                if (pos != startPos && pos != endPos)
                {
                    curRoom = GenerateBlueprintRoom(path, pos);
                }
                else
                {
                    curRoom = MasterDictionary[pos];
                }

                if (prevRoom == null)
                {
                    prevRoom = curRoom;
                    continue;
                }

                // TODO: figure out a better way this is really jank
                Vector3Int difference = curRoom.Position - prevRoom.Position;

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

                FlagDoorways(curRoom, prevRoom, entrFlagIdx);

                prevRoom = curRoom;
            }
        }
        #endregion

        #region Blueprint Random
        /// <summary>
        /// Drunkard Walk Algorithm, will walk a specified length and store it into a newly created path. The algorithm
        /// has been modified to handle collisions and create pseudo paths where rooms can potentially spawn later.
        /// </summary>
        /// <param name="path">A path with a length of atleast one.</param>
        /// <param name="startRoom">The starting room for the path. If null will create it's own start room</param>
        public void BlueprintDrunkardWalk(Path path, BoundsInt bounds, BlueprintRoom startRoom = null)
        {
            // *** TODO: REMOVE ***
            int fail = 1000;

            // Make sure the path has atleast one room cell that can spawn
            if (path.PathLength <= 0)
            {
                Debug.LogWarning($"Map Generator Error: Path {path.Name} has a length of 0 or is negative");
                return;
            }

            MasterPath.endMasterIdx = path.endMasterIdx;                     // Extend master path's end index

            Vector3Int curPos = Vector3Int.zero;
            BlueprintRoom curRoom = null;

            // Prime loop with starting room
            if (startRoom == null)          // Generate Start Room if a start room was not passed in, generate a start room at position (0,0,0); TODO: Make the start position a desired position if we plan on having places where the player can teleport to.
            {
                //**** TODO: REMOVE!!! ****
                Vector3Int tempStartRoomPos = new Vector3Int(5, 0, 5);      // Temp start room position for testing

                curRoom = GenerateBlueprintRoom(path, tempStartRoomPos);
                curPos = tempStartRoomPos;
                startRoom = curRoom;
            }
            else                            // Start at the desired Start Room
            {
                curPos = startRoom.Position;
                curRoom = startRoom;
            }
            if (_debugLogs) Debug.Log($"Map Generator: Starting cell for path {path.name} generated as {startRoom.RoomName}");

            // Chose a position in a random cardinal direction and check for collisions
            bool[] attempts = new bool[STANDARD_ROOM_FACE_COUNT];
            int failedAttempts = 0;
            int entrFlagIdx = 0;
            while (path.BlueprintCount() < path.PathLength)
            {
                Vector3Int tempPos = curPos;

                // Choose a random direction to be the potential position for the next room.
                int faceIdx = UnityEngine.Random.Range(1, STANDARD_ROOM_FACE_COUNT);

                while (attempts[faceIdx])                               // Store attempt direction in circular array to aviod choosing the same direction twice.
                {                                                       // Loop though attempts to find a unique direction
                    faceIdx++;
                    if (faceIdx % STANDARD_ROOM_FACE_COUNT == 0)           // Circle back in array
                        faceIdx = 0;
                }

                // "Walk" in that direction from the current pos
                switch (faceIdx)
                {
                    // E0 - E5 is the face count for a unit room, this will be used later for entranceways
                    case 0:
                        tempPos += Vector3Int.right;    // F0 : (1, 0, 0); Wall Right
                        entrFlagIdx = 0;
                        break;
                    case 1:
                        tempPos += Vector3Int.left;     // F1 : (-1, 0, 0); Wall Left
                        entrFlagIdx = 1;
                        break;
                    case 2:
                        tempPos += Vector3Int.forward;  // F2 : (0, 0, 1); Wall Forward
                        entrFlagIdx = 2;
                        break;
                    case 3:
                        tempPos += Vector3Int.back;     // F3 : (0, 0, -1); Wall Back
                        entrFlagIdx = 3;
                        break;
                    case 4:
                        tempPos += Vector3Int.up;       // F4 : (0, 1, 0); Wall Top
                        entrFlagIdx = 4;
                        break;
                    case 5:
                        tempPos += Vector3Int.down;     // F5 : (0, 1, 0); Wall Bot
                        entrFlagIdx = 5;
                        break;
                    default:
                        Debug.LogError("Map Generator Error: Direction choosen by gen alg does not exist.");
                        entrFlagIdx = -1;
                        break;
                }

                // Check if the room is in the realm of the bounding box, if not then don't spawn
                if (CheckOutOfBounds(tempPos, bounds))
                {
                    // TODO: Enable the stuff below, we need a prev room in order to do this because you cannot set the collided room as the bound
                    // attempts[entrFlagIdx] = true;
                    // failedAttempts++;

                    // *** TODO: REMOVE ***
                    fail--;
                    if (fail < 0)
                    {
                        Debug.LogError("Failed");
                        return;
                    }

                    if (_debugLogs) Debug.Log("Map Generator: Blueprint room was out of bounds so it was not spawned.");
                    continue;
                }

                // Check Master Path for collisions (the temp pos ends up being inside another designated room space)
                BlueprintRoom collidedRoom = null;

                // Check position in hash map; if failed then flag face attempt and try choosing a new position 
                if (CheckCollision(tempPos, out collidedRoom))        // *** Test Failed; collision with another blueprintRoom
                {
                    attempts[entrFlagIdx] = true;
                    failedAttempts++;
                }
                else                                         // *** Test Passed; no collision
                {
                    curPos = tempPos; // Change Current Position to new position

                    BlueprintRoom newBlueRoom = GenerateBlueprintRoom(path, curPos);
                    FlagDoorways(newBlueRoom, curRoom, entrFlagIdx);                    // Flag the face that touches the opposite room

                    curRoom = newBlueRoom;

                    // Reset Attempts Array because we sucessfully spawned blueprint room
                    Array.Clear(attempts, 0, attempts.Length);
                    failedAttempts = 0;
                }

                // TODO: This is a bad way of handling collisions, implement backtracking later!
                // If failed too many times -> try another room (rare)
                if (failedAttempts >= STANDARD_ROOM_FACE_COUNT)        // All spaces adjacent to the current room are covered
                {
                    // Make the current room the collided room and try to gen again
                    curPos = tempPos;
                    curRoom = collidedRoom;

                    // Reset Array
                    Array.Clear(attempts, 0, attempts.Length);
                    failedAttempts = 0;

                    if (curRoom == null) Debug.LogError("Map Generator Error: No more availible spaces exist for a new bluprint room where a conflict does not occur.");
                }
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
            string blueName = $"BlueprintRoom ({MasterPath.BlueprintCount()})";
            BlueprintRoom newRoom = new BlueprintRoom(position, blueName);
            newRoom.Available = available;

            if (_debugLogs) Debug.Log($"Generated blueprint room {blueName}");

            // Update paths and masters with new blueprint room
            path?.Add(newRoom);
            MasterPath?.Add(newRoom);                    // Add to Master List (required)
            MasterDictionary?.Add(position, newRoom);    // Add to Master Dictionary (required)
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
            int randomRoomIndex = UnityEngine.Random.Range(startIndex, endIndex);
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
        /// <param name="desiredPos">The desired position to spawn the next room</param>
        /// <returns>Returns true if the space is out of bounds and false otherwise.</returns>
        public bool CheckOutOfBounds(Vector3Int desiredPos, Vector3Int upperBound, Vector3Int lowerBound)
        {
            Vector3Int differenceUpper = upperBound - desiredPos;
            Vector3Int differenceLower = lowerBound - desiredPos;
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

        public Vector3Int CheckOutOfBounds(Vector3Int origin, Vector3Int roomDimensions, BoundsInt bounds)
        {
            Vector3Int lowerPoint = origin;
            Vector3Int upperPoint = origin + (roomDimensions - Vector3Int.one);

            Vector3Int lowerDiff = bounds.min - lowerPoint;
            Vector3Int upperDiff = bounds.max - upperPoint;

            if (lowerDiff.x > 0 || lowerDiff.y > 0 || lowerDiff.z > 0)      // Invalid Space
            {
                return new Vector3Int(
                   lowerDiff.x > 0 ? lowerDiff.x : 0,      // Return only the positive components of the lowerDiff
                   lowerDiff.y > 0 ? lowerDiff.y : 0,
                   lowerDiff.z > 0 ? lowerDiff.z : 0);
            }

            if (upperDiff.x < 0 || upperDiff.y < 0 || upperDiff.z < 0)      // Invalid Space
            {
                return new Vector3Int(
                   upperDiff.x < 0 ? upperDiff.x : 0,      // Return only the negative components of the lowerDiff
                   upperDiff.y < 0 ? upperDiff.y : 0,
                   upperDiff.z < 0 ? upperDiff.z : 0);
            }

            return Vector3Int.zero;        // Valid Space
        }

        /// <summary>
        /// Check point for collision with a blueprint room; the collided room is 
        /// returned (optional use)
        /// </summary>
        /// <param name="position"></param>
        /// <param name="collidedRoom"></param>
        /// <returns></returns>
        public bool CheckCollision(Vector3Int position, out BlueprintRoom collidedRoom)
        {
            return MasterDictionary.TryGetValue(position, out collidedRoom);
        }
        #endregion
    }
}
