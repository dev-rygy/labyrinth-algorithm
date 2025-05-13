/*
 * Created By:      Ryan Carpenter
 * Date Created:    05/11/2025
 * Last Modified:   05/11/2025 (Ryan)
 * Notes:           Blueprint Generator
*/
using System;
using System.Collections.Generic;
using UnityEngine;

using RyansLibrary.Graphs;
using RyansLibrary.Geometry;
using RyansLibrary.AI;

namespace RyansLibrary.Labyrinth
{
    public class BlueprintGenerator
    {
        // Amount of faces on a blueprint room; This should never be changed unless unique shaped rooms are made in the future
        const int STANDARD_ROOM_FACE_COUNT = 6;
        const string MASTER_PATH_NAME = "Master Path";

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

        public void InitializeMasters()     // NOTE: This must be done before generating anything!
        {
            // Initialize Master Data Structures
            MasterDictionary = new Dictionary<Vector3Int, BlueprintRoom>();
            MasterPath = ScriptableObject.CreateInstance<Path>();
            MasterPath.Initialize();
            MasterPath.Name = MASTER_PATH_NAME;
        }

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
                    {
                        r.Available = true;
                    }
                    else
                    {
                        GenerateBlueprintRoom(path, cellPosition, true);
                    }
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
                bool result = PlaceBoundedBlueprints(path, bounds, room.RoomDimensions, out Vector3Int spawnPosition);

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
                    {
                        r.Available = true;
                    }
                    else
                    {
                        GenerateBlueprintRoom(path, actualPos, true);
                    }
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
        public bool PlaceBoundedBlueprints(Path path, BoundsInt bounds, Vector3Int dimensions, out Vector3Int spawnPosition)
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
            List<BlueprintRoom> newRooms = GenerateBlueprintsFromDimensions(path, randomSpawnPos, dimensions);

            spawnPosition = randomSpawnPos;

            // do not advance iteration if nothing was spawned
            if (newRooms == null)
                return false;

            return true;
        }

        #region Blueprint Room Generation
        /// <summary>
        /// Generate a new blueprint room at the desired location. Add it to the master path and
        /// desired path passed in as an arguement. Generate a blueprint room gizmo if debug is enabled.
        /// NOTE: Position must be in room coords
        /// </summary>
        /// <param name="path">The desired path to add the new blueprint room to.</param>
        /// <param name="position">The desired position to spawn the new room at. Must be in world coords</param>
        /// <returns>Blueprint room created in room coords.</returns>
        private BlueprintRoom GenerateBlueprintRoom(Path path, Vector3Int position, bool available = true)
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
