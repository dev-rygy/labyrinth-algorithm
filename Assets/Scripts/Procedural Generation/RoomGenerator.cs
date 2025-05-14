/*
 * Created By:      Ryan Carpenter
 * Date Created:    05/12/2025
 * Last Modified:   05/12/2025 (Ryan)
 * Notes:           Room Generator
*/
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    //The room case based on the direction of the adjacent/next room.
    public enum RoomDirection
    {
        PosZ = 0,
        NegZ = 1,
        PosX = 2,
        NegX = 3,
        PosY = 4,
        NegY = 5
    }

    public class RoomGenerator
    {
        // Amount of faces on a blueprint room; This should never be changed unless unique shaped rooms are made in the future
        const int STANDARD_ROOM_FACE_COUNT = 6;

        // ***** Path Containers *****
        // The Master Path holds a reference to all bluprint rooms in an zone
        public Path MasterPath { get; private set; }

        // Dictionary used for quick access like checking locations for conflicts and checking locations for room shape conditions
        // Keys are in room coords
        public Dictionary<Vector3Int, BlueprintRoom> MasterDictionary { get; private set; }

        public RoomGenerator(Path masterPath, Dictionary<Vector3Int, BlueprintRoom> masterDictionary, int gridUnitSize, Transform roomContainer)
        {
            MasterPath = masterPath;
            MasterDictionary = masterDictionary;

            _gridUnitSize = gridUnitSize;
            _roomContainer = roomContainer;
        }

        private int _gridUnitSize;
        private Transform _roomContainer;
        private bool _debugGizmos = true;
        private bool _debugLogs = false;

        #region Unique Room Generator
        public void GenerateUniqueRooms(Zone zone)
        {
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                // Adjust parameters to fit the zone's actual position
                Vector3Int zoneOffset = zone.Bounds.position;
                Vector3Int adjustedSpawnPos = entry.SpawnPosition + zoneOffset;

                if (MasterPath == null || MasterDictionary == null)
                {
                    Debug.Log("One of these are null.");
                }

                Room generatedRoom = GenerateRoom(entry.Prefab, adjustedSpawnPos, zone.MainPath);

                // Unique rooms with available cells
                if (entry.AvailableCells != null)
                {
                    for (int i = 0; i < entry.AvailableCells.Count; i++)
                    {
                        if (MasterDictionary.TryGetValue(adjustedSpawnPos + entry.AvailableCells[i], out BlueprintRoom room))
                            generatedRoom.CopyBlueprintEntranceFlags(room.entrancewayFlags, i, Vector3.zero);
                        else
                            Debug.LogError($"Map Generator Error: Could not copy entranceway flags into unique room");
                    }
                }

                generatedRoom.Initialize();
            }
        }
        #endregion

        #region Path Room Generator
        public void GenerateRoomsOnPath(Path path)
        {
            if (path == null)      // Throw error if MainPath for zone does not exist
            {
                Debug.LogError($"Map Generator Error: The {path.Name} is not assigned for Room Generation.");
                return;
            }

            int indexOffset = 0;
            // If the path has starting room(s) then spawn the start room
            if (path.startingRooms.Count > 0)
            {
                path.Rooms.Add(GenerateRoom(RoomShape.smallRoom, RoomType.start, path, path.BlueprintRooms[0], 0));

                // Mark room space as unavailable
                path.BlueprintRooms[0].Available = false;
                indexOffset = 1;
            }

            PathType pathType = path.Type;
            // *** Loop through all blueprint rooms ***
            for (int i = 0 + indexOffset; i < path.BlueprintCount(); i++)
            {
                BlueprintRoom indexedRoom = path.BlueprintRooms[i];

                // Check if the indexed room is available; If not then skip iteration
                if (!indexedRoom.Available)
                    continue;

                RoomDirection rDir = RoomDirection.PosX;        // Default Room Case
                RoomType rType = RoomType.general;              // Default Room Type

                // Check conditions to spawn a Big Room starting at the indexed room's position
                if (RoomShapeCondition(indexedRoom, RoomShape.bigRoom, path, out rDir))
                {
                    // if the next room to be generated is the last room in the trail then make it the toBoss/prize room
                    if ((i + 4) >= path.BlueprintCount())
                    {
                        if (pathType == PathType.main)
                            rType = RoomType.toBoss;
                        else if (pathType == PathType.prize)
                            rType = RoomType.prize;
                    }

                    // spawn B-Room
                    // Hook up blueprintRoom.entrancewayflags to new room
                    Room genRoom = GenerateRoom(RoomShape.bigRoom, rType, path, indexedRoom, rDir);         // **** Spawn B-Room
                    path.Add(genRoom);              // Add new room to paths
                    MasterPath.Add(genRoom);
                    if (_debugLogs) Debug.Log($"{path.Name} Generated Big Room: {genRoom.name}");
                }

                // Check conditions to spawn a Tall Room starting at the indexed room's position
                else if (RoomShapeCondition(indexedRoom, RoomShape.tallRoom, path, out rDir))
                {
                    // if the next room to be generated is the last room in the trail then make it the toBoss/prize room
                    if ((i + 2) >= path.BlueprintCount())
                    {
                        if (pathType == PathType.main)
                            rType = RoomType.toBoss;
                        else if (pathType == PathType.prize)
                            rType = RoomType.prize;
                    }

                    Room genRoom = GenerateRoom(RoomShape.tallRoom, rType, path, indexedRoom, rDir);        // **** Spawn T-Room
                    path.Add(genRoom);              // Add new room to paths
                    MasterPath.Add(genRoom);
                    if (_debugLogs) Debug.Log($"{path.Name} Generated Tall Room: {genRoom.name}");
                }

                // Check conditions to spawn a Long Room starting at the indexed room's position
                else if (RoomShapeCondition(path.BlueprintRooms[i], RoomShape.longRoom, path, out rDir))
                {
                    // if the next room to be generated is the last room in the trail then make it the toBoss/prize room
                    if ((i + 2) >= path.BlueprintCount())
                    {
                        if (pathType == PathType.main)
                            rType = RoomType.toBoss;
                        else if (pathType == PathType.prize)
                            rType = RoomType.prize;
                    }

                    Room genRoom = GenerateRoom(RoomShape.longRoom, rType, path, indexedRoom, rDir);        // **** Spawn L-Room
                    path.Add(genRoom);              // Add new room to paths
                    MasterPath.Add(genRoom);
                    if (_debugLogs) Debug.Log($"{path.Name} Generated Long Room: {genRoom.name}");
                }

                // Default: Spawn a Small room at the indexed room's position
                else                                                                                        // **** Spawn S-Room
                {
                    // if the next room to be generated is the last room in the trail then make it the toBoss room
                    if ((i + 1) >= path.BlueprintCount())
                    {
                        if (pathType == PathType.main)
                            rType = RoomType.toBoss;
                        else if (pathType == PathType.prize)
                            rType = RoomType.prize;
                    }

                    // Make current blueprint space unavailable for future checks
                    path.BlueprintRooms[i].Available = false;

                    Room genRoom = GenerateRoom(RoomShape.smallRoom, rType, path, indexedRoom, 0); // Spawn S-Room
                    if (genRoom != null)
                    {
                        path.Add(genRoom);              // Add new room to paths
                        MasterPath.Add(genRoom);
                        if (_debugLogs) Debug.Log($"{path.Name} Generated Small Room: {genRoom.name}");
                    }
                    else
                    {
                        Debug.LogError($"Map Generator Error: Path {path.Name} attempted to spawn a Small Room but failed.");
                    }
                }
            }
        }

        /// <summary>
        /// Helper function; Returns true of the room with shape roomShape can be spawned, otherwise returns false.
        /// it also passes out the potential direction of the room so that rotations can be handled acordingly.
        /// If a room can be spawned the method will mark all rooms that take up the potential room's space.
        /// </summary>
        /// <param name="currRoom">The current blueprint room to be checked.</param>
        /// <param name="roomShape">The desired room shape to attempt to spawn.</param>
        /// <param name="path">The path to spawn the room in.</param>
        /// <param name="rDir">THe directional code of the spawned room, if succeeded.</param>
        /// <returns>true if the room can spawn, false otherwise.</returns>
        private bool RoomShapeCondition(BlueprintRoom currRoom, RoomShape roomShape, Path path, out RoomDirection rDir)
        {
            rDir = 0;               // Initialize the direction as default
            float roomRoll = UnityEngine.Random.Range(0, 1.01f);        // Roll for room based on it's % chance of spawning

            BlueprintRoom[] availBlueRooms = CheckAvailableAdjacentRooms(currRoom, path);

            switch (roomShape)
            {
                // *********** Big Room Conditions ***********
                case RoomShape.bigRoom:
                    {
                        // If the path holds no big room prefabs return false
                        if (path.rooms2x1x2.Count <= 0)
                            return false;

                        // Roll for room spawn probability 
                        if (roomRoll > path.BigRoomSpawnChance)
                            return false;

                        if (availBlueRooms[0] != null)      // 1.) If there is a room on the right
                        {
                            BlueprintRoom[] availBlueRoomsRight = CheckAvailableAdjacentRooms(availBlueRooms[0], path);

                            if (availBlueRoomsRight[2] != null)     // a.) If there is a room forward
                            {
                                BlueprintRoom[] availBlueRoomsFwd = CheckAvailableAdjacentRooms(availBlueRoomsRight[2], path);

                                if (availBlueRoomsFwd[1] != null)       // I.) If there is a room on the left
                                {
                                    currRoom.Available = false;                 // Lock the current room so it's not used in other checks
                                    availBlueRooms[0].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueRoomsRight[2].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueRoomsFwd[1].Available = false;        // Lock room right so it's not used in other checks
                                    rDir = RoomDirection.PosX;
                                    return true;
                                }
                            }
                            if (availBlueRoomsRight[3] != null)     // b.) If there is a room backward
                            {
                                BlueprintRoom[] availBlueRoomsBwd = CheckAvailableAdjacentRooms(availBlueRoomsRight[3], path);

                                if (availBlueRoomsBwd[1] != null)       // I.) If there is a room on the left
                                {
                                    currRoom.Available = false;                 // Lock the current room so it's not used in other checks
                                    availBlueRooms[0].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueRoomsRight[3].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueRoomsBwd[1].Available = false;        // Lock room right so it's not used in other checks
                                    rDir = RoomDirection.PosZ;
                                    return true;
                                }
                            }
                        }

                        if (availBlueRooms[1] != null)      // 2.) If there is a room on the left
                        {
                            BlueprintRoom[] availBlueRoomsLeft = CheckAvailableAdjacentRooms(availBlueRooms[1], path);

                            if (availBlueRoomsLeft[2] != null)     // a.) If there is a room forward
                            {
                                BlueprintRoom[] availBlueRoomsFwd = CheckAvailableAdjacentRooms(availBlueRoomsLeft[2], path);

                                if (availBlueRoomsFwd[0] != null)       // I.) If there is a room on the right
                                {
                                    currRoom.Available = false;                 // Lock the current room so it's not used in other checks
                                    availBlueRooms[1].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueRoomsLeft[2].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueRoomsFwd[0].Available = false;        // Lock room right so it's not used in other checks
                                    rDir = RoomDirection.NegX;
                                    return true;
                                }
                            }
                            if (availBlueRoomsLeft[3] != null)     // b.) If there is a room backward
                            {
                                BlueprintRoom[] availBlueRoomsBwd = CheckAvailableAdjacentRooms(availBlueRoomsLeft[3], path);

                                if (availBlueRoomsBwd[0] != null)       // I.) If there is a room on the right
                                {
                                    currRoom.Available = false;                 // Lock the current room so it's not used in other checks
                                    availBlueRooms[1].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueRoomsLeft[3].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueRoomsBwd[0].Available = false;        // Lock room right so it's not used in other checks
                                    rDir = RoomDirection.NegZ;
                                    return true;
                                }
                            }
                        }

                        // If none of these conditions hold then return false
                        return false;
                    }
                // *********** Tall Room Conditions ***********
                case RoomShape.tallRoom:
                    {
                        // If the path holds no tall room prefabs return false
                        if (path.rooms1x2x1.Count <= 0)
                            return false;

                        // Roll for room spawn probability 
                        if (roomRoll > path.TallRoomSpawnChance)
                            return false;

                        // A blueprint room exists that's above the current room
                        if (availBlueRooms[4] != null)
                        {
                            currRoom.Available = false;                 // Lock the current room so it's not used in other checks
                            availBlueRooms[4].Available = false;        // Lock room above so it's not used in other checks
                            rDir = RoomDirection.PosY;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }

                        // A blueprint room exists that's below the current room
                        if (availBlueRooms[5] != null)
                        {
                            currRoom.Available = false;                 // Lock the current room so it's not used in other checks
                            availBlueRooms[5].Available = false;        // Lock room below so it's not used in other checks
                            rDir = RoomDirection.NegY;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }

                        // If none of these conditions hold then return fail
                        return false;
                    }
                // *********** Long Room Conditions ***********
                case RoomShape.longRoom:
                    {
                        // If the path holds no long room prefabs return false
                        if (path.rooms2x1x1.Count <= 0)
                            return false;

                        // Roll for room spawn probability 
                        if (roomRoll > path.LongRoomSpawnChance)
                            return false;

                        // A blueprint room exists that's right to the current room
                        if (availBlueRooms[0] != null)
                        {
                            currRoom.Available = false;                 // Lock the current room so it's not used in other checks
                            availBlueRooms[0].Available = false;        // Lock room right so it's not used in other checks
                            rDir = RoomDirection.PosX;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }
                        // A blueprint room exists that's left to current room
                        if (availBlueRooms[1] != null)
                        {
                            currRoom.Available = false;                 // Lock the current room so it's not used in other checks
                            availBlueRooms[1].Available = false;        // Lock room left so it's not used in other checks
                            rDir = RoomDirection.NegX;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }
                        // A blueprint room exists that's forward from the current room
                        if (availBlueRooms[2] != null)
                        {
                            currRoom.Available = false;                 // Lock the current room so it's not used in other checks
                            availBlueRooms[2].Available = false;        // Lock room forward so it's not used in other checks
                            rDir = RoomDirection.PosZ;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }
                        // A blueprint room exists that's backward from the current room
                        if (availBlueRooms[3] != null)
                        {
                            currRoom.Available = false;                 // Lock the current room so it's not used in other checks
                            availBlueRooms[3].Available = false;        // Lock room backward so it's not used in other checks
                            rDir = RoomDirection.NegZ;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }

                        // If none of these conditions hold then return fail
                        return false;
                    }
                default:
                    {
                        Debug.LogError("Map Generator Error: Room condition checked wrong room shape.");
                        return false;
                    }
            }
        }

        /// <summary>
        /// Helper Function Test all spaces adjacent to the room being tested. If a room exists in that space then set 
        /// the return array to the BlueprintRoom Tied to that space.
        /// </summary>
        /// <param name="room">The current blueprint room to check around.</param>
        /// <param name="path">The path to loop through.</param>
        /// <returns>A set of blueprint rooms that are adjacent to the room and available</returns>
        private BlueprintRoom[] CheckAvailableAdjacentRooms(BlueprintRoom room, Path path)
        {
            // Store availRooms here and return. All possible avail rooms are up to the face count (F0 - F5)
            BlueprintRoom[] availBlueRooms = new BlueprintRoom[STANDARD_ROOM_FACE_COUNT];

            // Get the positions of potential adjacent rooms to the room
            Vector3Int rightRoomPos = room.Position + Vector3Int.right;     // F0: Right
            Vector3Int leftRoomPos = room.Position + Vector3Int.left;       // F1: Left
            Vector3Int fwdRoomPos = room.Position + Vector3Int.forward;     // F2: Forward
            Vector3Int backRoomPos = room.Position + Vector3Int.back;       // F3: Back
            Vector3Int topRoomPos = room.Position + Vector3Int.up;          // F4: Top
            Vector3Int botRoomPos = room.Position + Vector3Int.down;        // F5: Bot

            // Test each position; if the room does not exist the space is null, otherwise it's set to the Blueprint room tied to the position
            MasterDictionary.TryGetValue(rightRoomPos, out availBlueRooms[0]);        // F0: Right
            MasterDictionary.TryGetValue(leftRoomPos, out availBlueRooms[1]);         // F1: Left
            MasterDictionary.TryGetValue(fwdRoomPos, out availBlueRooms[2]);          // F2: Forward
            MasterDictionary.TryGetValue(backRoomPos, out availBlueRooms[3]);         // F3: Back 
            MasterDictionary.TryGetValue(topRoomPos, out availBlueRooms[4]);          // F4: Top
            MasterDictionary.TryGetValue(botRoomPos, out availBlueRooms[5]);          // F5: Bot

            // Loop through available room spaces and eliminate spaces that have already been taken up by other generated rooms
            for (int i = 0; i < availBlueRooms.Length; i++)
            {
                // If the room is not available due to it being used by another generated room
                // OR if it is not a part of the path in question then remove it from the availBlueRooms list.
                if (availBlueRooms[i] != null && (!availBlueRooms[i].Available || !path.BlueprintRooms.Contains(availBlueRooms[i])))
                    availBlueRooms[i] = null;
            }

            return availBlueRooms;
        }
        #endregion

        #region Generate Room
        /// <summary>
        /// Spawn a room given a position and direction; room type not passed as room is expected to
        /// already know it's type if unique (FOR NOW BUT MAYBE NOT LATER)
        /// For random room placement algorithm to use.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="placementPosition"></param>
        /// <param name="rDir"></param>
        /// <returns></returns>
        public Room GenerateRoom(GameObject prefab, Vector3Int placementPosition, Path path, RoomDirection rDir = 0)
        {
            Quaternion rotation = Quaternion.identity;      // TODO: set rotation
            Room generatedRoom = Object.Instantiate(prefab, ConvertToWorldCoords(placementPosition), rotation, _roomContainer).GetComponent<Room>();

            path.Add(generatedRoom);
            MasterPath.Add(generatedRoom);
            return generatedRoom;
        }

        /// <summary>
        /// Generate a room based on a path and all information given. Information must be decided beforhand, this function
        /// is very dependant!
        /// For path algorithm to use
        /// </summary>
        /// <param name="shape">The shape type of the room</param>
        /// <param name="rType">The special type of the room</param>
        /// <param name="path">The path the room will be a part of</param>
        /// <param name="originRoom">The room's origin blueprint room</param>
        /// <param name="rDir">The direction code of the room.</param>
        /// <param name="prefabIndex">The prefab index in the room array; set to -1 to spawn a random room.</param>
        /// <returns></returns>
        public Room GenerateRoom(RoomShape shape, RoomType rType, Path path, BlueprintRoom originRoom, RoomDirection rDir = 0, int prefabIndex = -1)      // prefabIndex = -1 means spawn random room
        {
            Room generatedRoom = null;
            Quaternion rotation = Quaternion.identity;      // Take the rotation of the room into account
            Vector3 eulerRotation = Vector3.zero;

            // If starting room then spawn starting room and return
            if (rType == RoomType.start)
            {
                // Generate Small Room; no direction condition needed
                generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.startingRooms), ConvertToWorldCoords(originRoom.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
                generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);   // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array
                generatedRoom.Initialize(rType);
                return generatedRoom;
            }

            switch (shape)
            {
                // ********* Generate Big Room (2x1x2) **************
                case RoomShape.bigRoom:
                    if (path.rooms2x1x2.Count <= 0)     // Check if the path's big room list is empty
                        return null;

                    // Generate Big Room based on it's direction
                    if (rDir == RoomDirection.PosX)     // Right, Forward, Left
                    {
                        BlueprintRoom rightRoom = MasterDictionary[originRoom.Position + Vector3Int.right];         // _>--
                        BlueprintRoom fwdRoom = MasterDictionary[rightRoom.Position + Vector3Int.forward];          // __-^
                        BlueprintRoom leftRoom = MasterDictionary[fwdRoom.Position + Vector3Int.left];              // __<-

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), ConvertToWorldCoords(originRoom.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(rightRoom.entrancewayFlags, 1, eulerRotation);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(fwdRoom.entrancewayFlags, 2, eulerRotation);               // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintEntranceFlags(leftRoom.entrancewayFlags, 3, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 18 - 23)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.NegX)        // Left, Forward, Right
                    {
                        BlueprintRoom leftRoom = MasterDictionary[originRoom.Position + Vector3Int.left];           // <_--
                        BlueprintRoom fwdRoom = MasterDictionary[leftRoom.Position + Vector3Int.forward];           // __^-
                        BlueprintRoom rightRoom = MasterDictionary[fwdRoom.Position + Vector3Int.right];            // __->

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), ConvertToWorldCoords(leftRoom.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(rightRoom.entrancewayFlags, 2, eulerRotation);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintEntranceFlags(fwdRoom.entrancewayFlags, 3, eulerRotation);               // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 18 - 23)
                        generatedRoom.CopyBlueprintEntranceFlags(leftRoom.entrancewayFlags, 0, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 0 - 5)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.PosZ)        // Right, Back, Left
                    {
                        BlueprintRoom rightRoom = MasterDictionary[originRoom.Position + Vector3Int.right];         // __->
                        BlueprintRoom backRoom = MasterDictionary[rightRoom.Position + Vector3Int.back];            // _v--
                        BlueprintRoom leftRoom = MasterDictionary[backRoom.Position + Vector3Int.left];             // <_--

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), ConvertToWorldCoords(leftRoom.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 3, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 18 - 23)
                        generatedRoom.CopyBlueprintEntranceFlags(rightRoom.entrancewayFlags, 2, eulerRotation);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintEntranceFlags(backRoom.entrancewayFlags, 1, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(leftRoom.entrancewayFlags, 0, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 0 - 5)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.NegZ)        // Left, Back, Right
                    {
                        BlueprintRoom leftRoom = MasterDictionary[originRoom.Position + Vector3Int.left];           // __<-
                        BlueprintRoom backRoom = MasterDictionary[leftRoom.Position + Vector3Int.back];             // v_--
                        BlueprintRoom rightRoom = MasterDictionary[backRoom.Position + Vector3Int.right];           // _>--

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), ConvertToWorldCoords(backRoom.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 2, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintEntranceFlags(rightRoom.entrancewayFlags, 1, eulerRotation);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(backRoom.entrancewayFlags, 0, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(leftRoom.entrancewayFlags, 3, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 18 - 23)
                        generatedRoom.Initialize(rType);
                    }
                    else
                        Debug.LogError("Map Generator Error: Roomcase does not match any valid Tall-Room Cases.");
                    break;

                // ********* Generate Tall Room (1x2x1) **************
                case RoomShape.tallRoom:
                    if (path.rooms1x2x1.Count <= 0)     // Check if the path's tall room list is empty
                        return null;

                    // Generate Tall Room based on it's direction
                    if (rDir == RoomDirection.PosY)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + Vector3Int.up];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms1x2x1), ConvertToWorldCoords(originRoom.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    }
                    else if (rDir == RoomDirection.NegY)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + Vector3Int.down];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms1x2x1), ConvertToWorldCoords(nextRoom.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 1, eulerRotation);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    }
                    else
                    {
                        Debug.LogError("Map Generator Error: Roomcase does not match any valid Tall-Room Cases.");
                    }
                    break;

                // ********* Generate Long Room (2x1x1) **************
                case RoomShape.longRoom:
                    if (path.rooms2x1x1.Count <= 0)     // Check if the path's long room list is empty
                        return null;

                    // Generate Long Room based on it's direction
                    if (rDir == RoomDirection.PosX)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + Vector3Int.right];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), ConvertToWorldCoords(originRoom.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    }
                    else if (rDir == RoomDirection.NegX)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + Vector3Int.left];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), ConvertToWorldCoords(nextRoom.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 1, eulerRotation);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    }
                    else if (rDir == RoomDirection.PosZ)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + Vector3Int.forward];

                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        eulerRotation = new Vector3(0, 90, 0);
                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), ConvertToWorldCoords(originRoom.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.NegZ)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + Vector3Int.back];

                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        eulerRotation = new Vector3(0, 90, 0);
                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), ConvertToWorldCoords(nextRoom.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 1, eulerRotation);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);
                    }
                    else
                        Debug.LogError("Map Generator Error: Roomcase does not match any valid Long-Room Cases.");
                    break;

                // ********* Generate Small Room (1x1x1) **************
                case RoomShape.smallRoom:
                    if (path.rooms1x1x1.Count <= 0)     // Check if the path's small room list is empty
                        return null;

                    // Generate Small Room; no direction condition neededd
                    generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms1x1x1), ConvertToWorldCoords(originRoom.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
                    generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array
                    generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    break;

                // ********* Error **************
                default:
                    Debug.LogError("Map Generator Error: Room Shape Invalid.");
                    break;
            }

            return generatedRoom;
        }
        #endregion

        #region Utility
        // Vector based conversion from room -> world coords
        private Vector3 ConvertToWorldCoords(Vector3Int roomCoords)
        {
            int xComp = roomCoords.x * _gridUnitSize;
            int yComp = roomCoords.y * _gridUnitSize;
            int zComp = roomCoords.z * _gridUnitSize;
            return new Vector3(xComp, yComp, zComp);
        }

        /// <summary>
        /// Choose a random room in a list based on the weights that are applied to that room.
        /// Takes the absolute probability meaning the function chooses a random position in the realm
        /// of all room possibilities.
        /// </summary>
        /// <param name="pathEntrys">The path entry list of a particular room shape in the path object.</param>
        /// <returns></returns>
        private GameObject ChooseRandomRoomFromWeights(List<PathEntry> pathEntrys)
        {
            // If the path's room entry list contains no room return null
            if (pathEntrys.Count == 0)
            {
                Debug.LogError("Map Generator Error: Probability of Room Weights Failed, room list empty.");
                return null;
            }

            // If the path's room entry list contains one room return that room's prefab
            if (pathEntrys.Count == 1)
                return pathEntrys[0].Prefab;

            // Choose a random room prefab based on probability
            int totalWeight = 0;
            foreach (PathEntry pathEntry in pathEntrys)
            {
                totalWeight += pathEntry.Probability;
            }

            int roll = UnityEngine.Random.Range(0, totalWeight + 1);        // roll 1 - 101; max exclusive
            int runningTotal = 0;
            for (int i = 0; i < pathEntrys.Count; i++)
            {
                runningTotal += pathEntrys[i].Probability;
                if (roll <= runningTotal)
                    return pathEntrys[i].Prefab;
            }

            Debug.LogError("Map Generator Error: Probability of Room Weights Failed, unknown error.");
            return null;
        }
        #endregion
    }
}
