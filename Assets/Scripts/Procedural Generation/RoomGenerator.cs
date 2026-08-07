/*
 * Created By:      Ryan Carpenter
 * Date Created:    05/12/2025
 * Last Modified:   08/06/2026 (Ryan)
 * Notes:           Room Generator
*/
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;  // Use Unity Engine's Random not System.Collection's Random

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Second-pass generator that turns the finished 1x1x1 Blueprint grid (built by BlueprintGenerator/the
    /// BlueprintOperation graph) into actual room GameObjects. Where the blueprint pass only knows about single
    /// grid cells and simple adjacency, this pass optionally *merges* several adjacent, still-available cells into
    /// one larger room prefab (e.g. a 2x1x2 "big room" spanning 4 cells) when the path's room-shape settings and a
    /// random roll allow it - see RoomShapeCondition for the shape-matching rules and GenerateRoom(RoomShape, ...)
    /// for how a merged room's single set of doorway flags is stitched together from each of the individual cells
    /// it replaces.
    /// </summary>
    public class RoomGenerator
    {
        // Amount of faces on a blueprint room; This should never be changed unless unique shaped rooms are made in the future
        const int STANDARD_FACE_COUNT = 6;

        // ***** Master References *****
        // Dictionary used for quick access like checking locations for conflicts and checking locations for room shape conditions
        // Keys are in room coords
        private readonly Dictionary<Vector3Int, Blueprint> _masterDictionaryReference;

        private int _gridUnitSize;          // Conventional size of a 1:1 room
        private Transform _roomContainer;   // GameObject that will hold rooms

        // Debugging
        private bool _debugLogs;

        public RoomGenerator(MapGenerationContext context, int gridUnitSize, Transform roomContainer)
        {
            _masterDictionaryReference = context.BlueprintDictionary;

            _gridUnitSize = gridUnitSize;
            _roomContainer = roomContainer;
        }

        #region Room Parser
        /// <summary>
        /// Will parse through all available rooms in a path. Will generate rooms
        /// with a specific size based on conditions.
        /// </summary>
        /// <param name="path">Path to generate rooms on</param>
        public bool ParsePathAndGenerateRooms(Path path)
        {
            if (path == null)      // Throw error if path does not exist
            {
                Debug.LogError($"The {path.Name} is not assigned for Room Generation.");
                return false;
            }

            int indexOffset = 0;
            // If the path has starting room(s) then spawn the start room
            if (path.startingRooms.Count > 0)
            {
                path.Rooms.Add(GenerateRoom(RoomShape.smallRoom, RoomType.start, path, path.BlueprintList[0], 0));

                // Mark room space as unavailable
                path.BlueprintList[0].Available = false;
                indexOffset = 1;
            }

            PathType pathType = path.Type;
            // *** Loop through all blueprint rooms ***
            for (int i = 0 + indexOffset; i < path.BlueprintCount(); i++)
            {
                // Initialize current room and blueprint room at start of each iteration
                Blueprint indexedBlueprint = path.BlueprintList[i];
                Room genRoom = null;

                // Check if the indexed room is available; If not then skip iteration
                if (!indexedBlueprint.Available)
                    continue;

                RoomShift rDir = RoomShift.E;        // Default Room Case
                RoomType rType = RoomType.general;              // Default Room Type

                // TODO: Remove this and do it another way. This is a bad way of getting the last room
                switch (pathType)
                {
                    case PathType.prize:
                        if (i == path.BlueprintCount() - 1)     // Final room in prize path is marked as prize
                        {
                            rType = RoomType.prize;
                        }
                        break;
                }

                // Check conditions to spawn a Big Room starting at the indexed room's position
                if (RoomShapeCondition(indexedBlueprint, RoomShape.bigRoom, path, out rDir))
                {
                    // spawn B-Room
                    // Hook up blueprintRoom.entrancewayflags to new room
                    genRoom = GenerateRoom(RoomShape.bigRoom, rType, path, indexedBlueprint, rDir);         // **** Spawn B-Room
                    path.Add(genRoom);              // Add new room to paths

                    if (genRoom == null)
                    {
                        Debug.LogError($"Path {path.Name} attempted to spawn a Small Room but failed.");
                        return false;
                    }

                    if (_debugLogs)
                        Debug.Log($"Path {path.Name} Generated Big Room: {genRoom.name}");
                }

                // Check conditions to spawn a Big Room starting at the indexed room's position
                else if (RoomShapeCondition(indexedBlueprint, RoomShape.lRoom, path, out rDir))
                {
                    // spawn L-Room
                    // Hook up blueprintRoom.entrancewayflags to new room
                    genRoom = GenerateRoom(RoomShape.lRoom, rType, path, indexedBlueprint, rDir);         // **** Spawn L-Room
                    path.Add(genRoom);              // Add new room to paths

                    if (genRoom == null)
                    {
                        Debug.LogError($"Path {path.Name} attempted to spawn a Small Room but failed.");
                        return false;
                    }

                    if (_debugLogs)
                        Debug.Log($"Path {path.Name} Generated L-Room: {genRoom.name}");
                }

                // Check conditions to spawn a Tall Room starting at the indexed room's position
                else if (RoomShapeCondition(indexedBlueprint, RoomShape.tallRoom, path, out rDir))
                {
                    genRoom = GenerateRoom(RoomShape.tallRoom, rType, path, indexedBlueprint, rDir);        // **** Spawn T-Room
                    path.Add(genRoom);              // Add new room to paths

                    if (genRoom == null)
                    {
                        Debug.LogError($"Path {path.Name} attempted to spawn a Small Room but failed.");
                        return false;
                    }

                    if (_debugLogs) 
                        Debug.Log($"Path {path.Name} Generated Tall Room: {genRoom.name}");
                }

                // Check conditions to spawn a Long Room starting at the indexed room's position
                else if (RoomShapeCondition(path.BlueprintList[i], RoomShape.longRoom, path, out rDir))
                {
                    genRoom = GenerateRoom(RoomShape.longRoom, rType, path, indexedBlueprint, rDir);        // **** Spawn L-Room

                    if (genRoom == null)
                    {
                        Debug.LogError($"Path {path.Name} attempted to spawn a Small Room but failed.");
                        return false;
                    }

                    path.Add(genRoom);              // Add new room to paths

                    if (_debugLogs) 
                        Debug.Log($"Path {path.Name} Generated Long Room: {genRoom.name}");
                }

                // Default: Spawn a Small room at the indexed room's position
                else                                                                                        // **** Spawn S-Room
                {
                    // Make current blueprint space unavailable for future checks
                    path.BlueprintList[i].Available = false;

                    genRoom = GenerateRoom(RoomShape.smallRoom, rType, path, indexedBlueprint, 0); // Spawn S-Room
                    if (genRoom == null)
                    {
                        Debug.LogError($"Path {path.Name} attempted to spawn a Small Room but failed.");
                        return false;
                    }

                    path.Add(genRoom);              // Add new room to paths

                    if (_debugLogs) 
                        Debug.Log($"Path {path.Name} Generated Small Room: {genRoom.name}");
                }
            }

            return true;
        }

        /// <summary>
        /// Helper function; Returns true of the room with shape roomShape can be spawned, otherwise returns false.
        /// it also passes out the potential direction of the room so that rotations can be handled acordingly.
        /// If a room can be spawned the method will mark all rooms that take up the potential room's space.
        /// </summary>
        /// <param name="currentBlueprint">The current blueprint room to be checked.</param>
        /// <param name="roomShape">The desired room shape to attempt to spawn.</param>
        /// <param name="path">The path to spawn the room in.</param>
        /// <param name="rDir">THe directional code of the spawned room, if succeeded.</param>
        /// <returns>true if the room can spawn, false otherwise.</returns>
        // Greedy shape matching: ParsePathAndGenerateRooms tries bigRoom, then tallRoom, then longRoom, and falls
        // back to a plain 1x1x1 smallRoom if none fit - so the largest possible merged room wins whenever the dice
        // roll and the neighboring cells (checked below via CheckAvailableAdjacentBlueprints) allow it. Every
        // successful match immediately flips the participating cells' Available flag to false so they can't be
        // double-claimed by a later cell's shape check as the loop continues down the path.
        private bool RoomShapeCondition(Blueprint currentBlueprint, RoomShape roomShape, Path path, out RoomShift rDir)
        {
            rDir = 0;               // Initialize the direction as default
            float roomRoll = Random.Range(0, 1.01f);        // Roll for room based on it's % chance of spawning

            Blueprint[] availBlueRooms = CheckAvailableAdjacentBlueprints(currentBlueprint, path);

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

                        // 1.) If there is a room on the right
                        if (availBlueRooms[0] != null)
                        {
                            Blueprint[] availBlueprintsRight = CheckAvailableAdjacentBlueprints(availBlueRooms[0], path);

                            if (availBlueprintsRight[2] != null)     // a.) If there is a room forward
                            {
                                Blueprint[] availBlueprintsFwd = CheckAvailableAdjacentBlueprints(availBlueprintsRight[2], path);

                                if (availBlueprintsFwd[1] != null)       // I.) If there is a room on the left
                                {
                                    currentBlueprint.Available = false;                 // Lock the current room so it's not used in other checks
                                    availBlueRooms[0].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueprintsRight[2].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueprintsFwd[1].Available = false;        // Lock room right so it's not used in other checks
                                    rDir = RoomShift.E;
                                    return true;
                                }
                            }
                            if (availBlueprintsRight[3] != null)     // b.) If there is a room backward
                            {
                                Blueprint[] availBlueprintsBwd = CheckAvailableAdjacentBlueprints(availBlueprintsRight[3], path);

                                if (availBlueprintsBwd[1] != null)       // I.) If there is a room on the left
                                {
                                    currentBlueprint.Available = false;                 // Lock the current room so it's not used in other checks
                                    availBlueRooms[0].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueprintsRight[3].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueprintsBwd[1].Available = false;        // Lock room right so it's not used in other checks
                                    rDir = RoomShift.N;
                                    return true;
                                }
                            }
                        }

                        // 2.) If there is a room on the left
                        if (availBlueRooms[1] != null)
                        {
                            Blueprint[] availBlueprintsLeft = CheckAvailableAdjacentBlueprints(availBlueRooms[1], path);

                            if (availBlueprintsLeft[2] != null)     // a.) If there is a room forward
                            {
                                Blueprint[] availBlueprintsFwd = CheckAvailableAdjacentBlueprints(availBlueprintsLeft[2], path);

                                if (availBlueprintsFwd[0] != null)       // I.) If there is a room on the right
                                {
                                    currentBlueprint.Available = false;             // Lock the current room so it's not used in other checks
                                    availBlueRooms[1].Available = false;            // Lock room left so it's not used in other checks
                                    availBlueprintsLeft[2].Available = false;       // Lock room forward so it's not used in other checks
                                    availBlueprintsFwd[0].Available = false;        // Lock room right so it's not used in other checks
                                    rDir = RoomShift.W;
                                    return true;
                                }
                            }
                            if (availBlueprintsLeft[3] != null)     // b.) If there is a room backward
                            {
                                Blueprint[] availBlueprintsBwd = CheckAvailableAdjacentBlueprints(availBlueprintsLeft[3], path);

                                if (availBlueprintsBwd[0] != null)       // I.) If there is a room on the right
                                {
                                    currentBlueprint.Available = false;             // Lock the current room so it's not used in other checks
                                    availBlueRooms[1].Available = false;            // Lock room left so it's not used in other checks
                                    availBlueprintsLeft[3].Available = false;       // Lock room backward so it's not used in other checks
                                    availBlueprintsBwd[0].Available = false;        // Lock room right so it's not used in other checks
                                    rDir = RoomShift.S;
                                    return true;
                                }
                            }
                        }

                        // If none of these conditions hold then return false
                        return false;
                    }

                case RoomShape.lRoom:
                    {
                        // If the path holds no big room prefabs return false
                        if (path.rooms2x1x2l.Count <= 0)
                            return false;

                        // Roll for room spawn probability 
                        if (roomRoll > path.LRoomSpawnChance)
                            return false;

                        // 1.) If there is a room on the right
                        if (availBlueRooms[0] != null)
                        {
                            Blueprint[] availBlueprintsRight = CheckAvailableAdjacentBlueprints(availBlueRooms[0], path);

                            if (availBlueprintsRight[2] != null)     // a.) If there is a room forward
                            {
                                currentBlueprint.Available = false;               // Lock the current room so it's not used in other checks
                                availBlueRooms[0].Available = false;              // Lock room right so it's not used in other checks
                                availBlueprintsRight[2].Available = false;        // Lock room forward so it's not used in other checks
                                rDir = RoomShift.E;
                                return true;
                            }
                        }

                        // 2.) If there is a room forward
                        if (availBlueRooms[2] != null)
                        {
                            Blueprint[] availBlueprintsFwd = CheckAvailableAdjacentBlueprints(availBlueRooms[2], path);

                            if (availBlueprintsFwd[1] != null)     // b.) If there is a room left
                            {
                                currentBlueprint.Available = false;             // Lock the current room so it's not used in other checks
                                availBlueRooms[2].Available = false;            // Lock room forward so it's not used in other checks
                                availBlueprintsFwd[1].Available = false;        // Lock room left so it's not used in other checks
                                rDir = RoomShift.W;
                                return true;
                            }
                        }

                        // 3.) If there is a room on the left
                        if (availBlueRooms[1] != null)
                        {
                            Blueprint[] availBlueprintsLeft = CheckAvailableAdjacentBlueprints(availBlueRooms[1], path);

                            if (availBlueprintsLeft[3] != null)     // b.) If there is a room backward
                            {
                                currentBlueprint.Available = false;             // Lock the current room so it's not used in other checks
                                availBlueRooms[1].Available = false;            // Lock room left so it's not used in other checks
                                availBlueprintsLeft[3].Available = false;       // Lock room backward so it's not used in other checks
                                rDir = RoomShift.S;
                                return true;
                            }
                        }

                        // 4.) If there is a room on the back
                        if (availBlueRooms[3] != null)
                        {
                            Blueprint[] availBlueprintsRight = CheckAvailableAdjacentBlueprints(availBlueRooms[3], path);

                            if (availBlueprintsRight[0] != null)     // b.) If there is a room on the right
                            {
                                currentBlueprint.Available = false;             // Lock the current room so it's not used in other checks
                                availBlueRooms[3].Available = false;            // Lock room back so it's not used in other checks
                                availBlueprintsRight[0].Available = false;       // Lock room right so it's not used in other checks
                                rDir = RoomShift.N;
                                return true;
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
                            currentBlueprint.Available = false;         // Lock the current room so it's not used in other checks
                            availBlueRooms[4].Available = false;        // Lock room above so it's not used in other checks
                            rDir = RoomShift.Up;                  // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }

                        // A blueprint room exists that's below the current room
                        if (availBlueRooms[5] != null)
                        {
                            currentBlueprint.Available = false;         // Lock the current room so it's not used in other checks
                            availBlueRooms[5].Available = false;        // Lock room below so it's not used in other checks
                            rDir = RoomShift.Down;                  // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
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
                            currentBlueprint.Available = false;         // Lock the current room so it's not used in other checks
                            availBlueRooms[0].Available = false;        // Lock room right so it's not used in other checks
                            rDir = RoomShift.E;                  // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }
                        // A blueprint room exists that's left to current room
                        if (availBlueRooms[1] != null)
                        {
                            currentBlueprint.Available = false;         // Lock the current room so it's not used in other checks
                            availBlueRooms[1].Available = false;        // Lock room left so it's not used in other checks
                            rDir = RoomShift.W;                  // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }
                        // A blueprint room exists that's forward from the current room
                        if (availBlueRooms[2] != null)
                        {
                            currentBlueprint.Available = false;         // Lock the current room so it's not used in other checks
                            availBlueRooms[2].Available = false;        // Lock room forward so it's not used in other checks
                            rDir = RoomShift.N;                  // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }
                        // A blueprint room exists that's backward from the current room
                        if (availBlueRooms[3] != null)
                        {
                            currentBlueprint.Available = false;         // Lock the current room so it's not used in other checks
                            availBlueRooms[3].Available = false;        // Lock room backward so it's not used in other checks
                            rDir = RoomShift.S;                  // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }

                        // If none of these conditions hold then return fail
                        return false;
                    }
                default:
                    {
                        Debug.LogError($"Room condition checked wrong room shape.");
                        return false;
                    }
            }
        }

        /// <summary>
        /// Helper Function Test all spaces adjacent to the room being tested. If a room exists in that space then set 
        /// the return array to the BlueprintRoom Tied to that space.
        /// </summary>
        /// <param name="blueprint">The current blueprint room to check around.</param>
        /// <param name="path">The path to loop through.</param>
        /// <returns>A set of blueprint rooms that are adjacent to the room and available</returns>
        private Blueprint[] CheckAvailableAdjacentBlueprints(Blueprint blueprint, Path path)
        {
            // Store availRooms here and return. All possible avail rooms are up to the face count (F0 - F5)
            Blueprint[] availableBlueprints = new Blueprint[STANDARD_FACE_COUNT];

            // Get the positions of potential adjacent rooms to the room
            Vector3Int rightRoomPos = blueprint.Position + Vector3Int.right;     // F0: Right
            Vector3Int leftRoomPos = blueprint.Position + Vector3Int.left;       // F1: Left
            Vector3Int fwdRoomPos = blueprint.Position + Vector3Int.forward;     // F2: Forward
            Vector3Int backRoomPos = blueprint.Position + Vector3Int.back;       // F3: Back
            Vector3Int topRoomPos = blueprint.Position + Vector3Int.up;          // F4: Top
            Vector3Int botRoomPos = blueprint.Position + Vector3Int.down;        // F5: Bot

            // Test each position; if the room does not exist the space is null, otherwise it's set to the Blueprint room tied to the position
            _masterDictionaryReference.TryGetValue(rightRoomPos, out availableBlueprints[0]);        // F0: Right
            _masterDictionaryReference.TryGetValue(leftRoomPos, out availableBlueprints[1]);         // F1: Left
            _masterDictionaryReference.TryGetValue(fwdRoomPos, out availableBlueprints[2]);          // F2: Forward
            _masterDictionaryReference.TryGetValue(backRoomPos, out availableBlueprints[3]);         // F3: Back 
            _masterDictionaryReference.TryGetValue(topRoomPos, out availableBlueprints[4]);          // F4: Top
            _masterDictionaryReference.TryGetValue(botRoomPos, out availableBlueprints[5]);          // F5: Bot

            // Loop through available room spaces and eliminate spaces that have already been taken up by other generated rooms
            for (int i = 0; i < availableBlueprints.Length; i++)
            {
                // If the room is not available due to it being used by another generated room
                // OR if it is not a part of the path in question then remove it from the availBlueRooms list.
                if (availableBlueprints[i] != null && (!availableBlueprints[i].Available || !path.BlueprintList.Contains(availableBlueprints[i])))
                    availableBlueprints[i] = null;
            }

            return availableBlueprints;
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
        public Room GenerateRoom(GameObject prefab, Vector3Int placementPosition, Path path, RoomShift rDir = 0)
        {
            Quaternion rotation = Quaternion.identity;      // TODO: set rotation
            Room generatedRoom = Object.Instantiate(prefab, ConvertToWorldCoords(placementPosition), rotation, _roomContainer).GetComponent<Room>();

            path.Add(generatedRoom);
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
        /// <param name="originBlueprint">The room's origin blueprint room</param>
        /// <param name="rDir">The direction code of the room.</param>
        /// <param name="prefabIndex">The prefab index in the room array; set to -1 to spawn a random room.</param>
        /// <returns></returns>
        // Every branch below follows the same pattern: look up the blueprint cell(s) this merged room is replacing
        // (relative to originBlueprint, in the direction rDir), instantiate the multi-cell prefab at the correct
        // corner, then call CopyBlueprintEntranceFlags once per replaced cell so the single prefab ends up with a
        // doorway wherever *any* of the individual cells it absorbed had one flagged open. The index passed to
        // CopyBlueprintEntranceFlags (0,1,2,3) is the room's own "which cell within me is this door data for" slot,
        // not the world direction - it has to be reassigned per rDir since the same prefab can be approached from
        // multiple directions along the path.
        public Room GenerateRoom(RoomShape shape, RoomType rType, Path path, Blueprint originBlueprint, RoomShift rDir = 0, int prefabIndex = -1)      // prefabIndex = -1 means spawn random room
        {
            Room generatedRoom = null;
            Quaternion rotation = Quaternion.identity;      // Take the rotation of the room into account
            Vector3 eulerRotation = Vector3.zero;

            // If starting room then spawn starting room and return
            if (rType == RoomType.start)
            {
                // Generate Small Room; no direction condition needed
                // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
                generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.startingRooms), 
                    ConvertToWorldCoords(originBlueprint.Position), rotation, _roomContainer).GetComponent<Room>();
                // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array
                generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 0);
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
                    if (rDir == RoomShift.E)     // Right, Forward, Left
                    {
                        // TODO: Change these Blueprint positions to use a for loop 
                        // TODO: _masterDictionaryReference[originBlueprint.Position + availableCellPosition];
                        Blueprint rightBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.right];
                        Blueprint fwdBlueprint = _masterDictionaryReference[rightBlueprint.Position + Vector3Int.forward];
                        Blueprint leftBlueprint = _masterDictionaryReference[fwdBlueprint.Position + Vector3Int.left];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), ConvertToWorldCoords(originBlueprint.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 0);            // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(rightBlueprint.EntryPointFlags, 1);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(fwdBlueprint.EntryPointFlags, 2);               // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintEntranceFlags(leftBlueprint.EntryPointFlags, 3);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 18 - 23)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomShift.W)        // Left, Forward, Right
                    {
                        Blueprint leftBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.left];
                        Blueprint fwdBlueprint = _masterDictionaryReference[leftBlueprint.Position + Vector3Int.forward];
                        Blueprint rightBlueprint = _masterDictionaryReference[fwdBlueprint.Position + Vector3Int.right];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), ConvertToWorldCoords(leftBlueprint.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 1);           // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(rightBlueprint.EntryPointFlags, 2);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintEntranceFlags(fwdBlueprint.EntryPointFlags, 3);               // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 18 - 23)
                        generatedRoom.CopyBlueprintEntranceFlags(leftBlueprint.EntryPointFlags, 0);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 0 - 5)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomShift.N)        // Right, Back, Left
                    {
                        Blueprint rightBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.right];
                        Blueprint backBlueprint = _masterDictionaryReference[rightBlueprint.Position + Vector3Int.back];
                        Blueprint leftBlueprint = _masterDictionaryReference[backBlueprint.Position + Vector3Int.left];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), ConvertToWorldCoords(leftBlueprint.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 3);           // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 18 - 23)
                        generatedRoom.CopyBlueprintEntranceFlags(rightBlueprint.EntryPointFlags, 2);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintEntranceFlags(backBlueprint.EntryPointFlags, 1);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(leftBlueprint.EntryPointFlags, 0);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 0 - 5)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomShift.S)        // Left, Back, Right
                    {
                        Blueprint leftBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.left];
                        Blueprint backBlueprint = _masterDictionaryReference[leftBlueprint.Position + Vector3Int.back];
                        Blueprint rightBlueprint = _masterDictionaryReference[backBlueprint.Position + Vector3Int.right];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), ConvertToWorldCoords(backBlueprint.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 2);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintEntranceFlags(rightBlueprint.EntryPointFlags, 1);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(backBlueprint.EntryPointFlags, 0);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(leftBlueprint.EntryPointFlags, 3);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 18 - 23)
                        generatedRoom.Initialize(rType);
                    }
                    else
                        Debug.LogError($"Roomcase does not match any valid Tall-Room Cases.");
                    break;

                // ********* Generate L Room (2x1x2) **************
                case RoomShape.lRoom:
                    if (path.rooms2x1x2l.Count <= 0)     // Check if the path's big room list is empty
                        return null;

                    // Generate L Room based on it's direction
                    if (rDir == RoomShift.E)     // Right, Forward
                    {
                        // TODO: Change these Blueprint positions to use a for loop 
                        // TODO: _masterDictionaryReference[originBlueprint.Position + availableCellPosition];
                        Blueprint rightBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.right];
                        Blueprint fwdBlueprint = _masterDictionaryReference[rightBlueprint.Position + Vector3Int.forward];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2l), ConvertToWorldCoords(originBlueprint.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 0);           // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(rightBlueprint.EntryPointFlags, 1);            // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(fwdBlueprint.EntryPointFlags, 2);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 12 - 17)
                        generatedRoom.Initialize(rType);
                    }

                    else if (rDir == RoomShift.W)        // Fwd, Left
                    {
                        Blueprint fwdBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.forward];
                        Blueprint leftBlueprint = _masterDictionaryReference[fwdBlueprint.Position + Vector3Int.left];

                        rotation = Quaternion.Euler(0f, -90f, 0f);
                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2l), ConvertToWorldCoords(originBlueprint.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 0, RoomRotation.Deg90);           // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(fwdBlueprint.EntryPointFlags, 1, RoomRotation.Deg90);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(leftBlueprint.EntryPointFlags, 2, RoomRotation.Deg90);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 12 - 17)
                        generatedRoom.Initialize(rType);
                    }

                    else if (rDir == RoomShift.S)        // Left, Back
                    {
                        Blueprint leftBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.left];
                        Blueprint backBlueprint = _masterDictionaryReference[leftBlueprint.Position + Vector3Int.back];

                        rotation = Quaternion.Euler(0f, 180f, 0f);
                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2l), ConvertToWorldCoords(originBlueprint.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 0, RoomRotation.Deg180);            // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(leftBlueprint.EntryPointFlags, 1, RoomRotation.Deg180);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(backBlueprint.EntryPointFlags, 2, RoomRotation.Deg180);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 12 - 17)
                        generatedRoom.Initialize(rType);
                    }

                    else if (rDir == RoomShift.N)        // Back, Right
                    {
                        Blueprint backBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.back];
                        Blueprint rightBlueprint = _masterDictionaryReference[backBlueprint.Position + Vector3Int.right];

                        rotation = Quaternion.Euler(0f, -270f, 0f);
                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2l), ConvertToWorldCoords(originBlueprint.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 0, RoomRotation.Deg270);            // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(backBlueprint.EntryPointFlags, 1, RoomRotation.Deg270);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(rightBlueprint.EntryPointFlags, 2, RoomRotation.Deg270);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 12 - 17)
                        generatedRoom.Initialize(rType);
                    }

                    else
                        Debug.LogError($"Roomcase does not match any valid L-Room Cases.");
                    break;

                // ********* Generate Tall Room (1x2x1) **************
                case RoomShape.tallRoom:
                    if (path.rooms1x2x1.Count <= 0)     // Check if the path's tall room list is empty
                        return null;

                    // Generate Tall Room based on it's direction
                    if (rDir == RoomShift.Up)
                    {
                        Blueprint upBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.up];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms1x2x1), ConvertToWorldCoords(originBlueprint.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 0);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(upBlueprint.EntryPointFlags, 1);           // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                    // Activate new rooms entranceways
                    }
                    else if (rDir == RoomShift.Down)
                    {
                        Blueprint downBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.down];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms1x2x1), ConvertToWorldCoords(downBlueprint.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 1);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(downBlueprint.EntryPointFlags, 0);         // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                    // Activate new rooms entranceways
                    }
                    else
                    {
                        Debug.LogError("Roomcase does not match any valid Tall-Room Cases.");
                    }
                    break;

                // ********* Generate Long Room (2x1x1) **************
                case RoomShape.longRoom:
                    if (path.rooms2x1x1.Count <= 0)     // Check if the path's long room list is empty
                        return null;

                    // Generate Long Room based on it's direction
                    if (rDir == RoomShift.E)
                    {
                        Blueprint rightBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.right];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), ConvertToWorldCoords(originBlueprint.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 0);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(rightBlueprint.EntryPointFlags, 1);         // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                     // Activate new rooms entranceways
                    }
                    else if (rDir == RoomShift.W)
                    {
                        Blueprint leftBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.left];

                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), ConvertToWorldCoords(leftBlueprint.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 1);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(leftBlueprint.EntryPointFlags, 0);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                     // Activate new rooms entranceways
                    }
                    else if (rDir == RoomShift.N)
                    {
                        Blueprint fwdBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.forward];

                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), ConvertToWorldCoords(originBlueprint.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 0, RoomRotation.Deg90);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(fwdBlueprint.EntryPointFlags, 1, RoomRotation.Deg90);           // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomShift.S)
                    {
                        Blueprint backBlueprint = _masterDictionaryReference[originBlueprint.Position + Vector3Int.back];

                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), ConvertToWorldCoords(backBlueprint.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 1, RoomRotation.Deg90);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(backBlueprint.EntryPointFlags, 0, RoomRotation.Deg90);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);
                    }
                    else
                        Debug.LogError("Roomcase does not match any valid Long-Room Cases.");
                    break;

                // ********* Generate Small Room (1x1x1) **************
                case RoomShape.smallRoom:
                    if (path.rooms1x1x1.Count <= 0)     // Check if the path's small room list is empty
                        return null;

                    // Generate Small Room; no direction condition neededd
                    generatedRoom = Object.Instantiate(ChooseRandomRoomFromWeights(path.rooms1x1x1), ConvertToWorldCoords(originBlueprint.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
                    generatedRoom.CopyBlueprintEntranceFlags(originBlueprint.EntryPointFlags, 0);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array
                    generatedRoom.Initialize(rType);                                                    // Activate new rooms entranceways
                    break;

                // ********* Error **************
                default:
                    Debug.LogError("Room Shape Invalid.");
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
        /// <param name="pathEntries">The path entry list of a particular room shape in the path object.</param>
        /// <returns></returns>
        private GameObject ChooseRandomRoomFromWeights(List<PathEntry> pathEntries)
        {
            // If the path's room entry list contains no room return null
            if (pathEntries.Count == 0)
            {
                Debug.LogError("Probability of room weights failed, room list empty.");
                return null;
            }

            // If the path's room entry list contains one room return that room's prefab
            if (pathEntries.Count == 1)
                return pathEntries[0].Prefab;

            // Choose a random room prefab based on probability
            int totalWeight = 0;
            foreach (PathEntry pathEntry in pathEntries)
            {
                totalWeight += pathEntry.Probability;
            }

            int roll = Random.Range(0, totalWeight + 1);        // roll 1 - 101; max exclusive
            int runningTotal = 0;
            for (int i = 0; i < pathEntries.Count; i++)
            {
                runningTotal += pathEntries[i].Probability;
                if (roll <= runningTotal)
                    return pathEntries[i].Prefab;
            }

            Debug.LogError("Probability of room weights failed, unknown error.");
            return null;
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
