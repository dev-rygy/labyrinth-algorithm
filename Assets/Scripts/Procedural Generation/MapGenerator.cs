/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   03/10/2025 (Ryan)
 * Notes:           Map Generator
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    #region Helper Objects
    /// <summary>
    /// Holds the properties of a suedo room that does not actually exist in the world.
    /// Is meant to be replaced by actual rooms later on.
    /// </summary>
    public class BlueprintRoom
    {
        public string RoomName { get; private set; }
        public Vector3 Position { get; private set; }
        public bool Available { get; set; }
        public bool[] entrancewayFlags;

        // Constructor
        public BlueprintRoom(Vector3 postion, string roomName = "Blueprint Room")
        {
            Available = true;
            RoomName = roomName;
            Position = postion;
            entrancewayFlags = new bool[6];       // A flag to mark which entrances should be open for a room
        }
    }
    #endregion

    public class MapGenerator : MonoBehaviour
    {
        #region Variables
        // ***** CONSTANTS *****
        const int STANDARD_ROOM_FACE_COUNT = 6;                // Amount of faces on a blueprint room; This should never be changed unless unique shapes are made in the future
        const string MASTER_PATH_NAME = "Master Path";

        // ***** Singleton Reference *****
        public static MapGenerator Instance { get; private set; }

        // ***** Events *****
        public static event Action OnGenerationDone;
        public static event Action OnGenerationStarted;

        // ***** Path Containers *****
        // The Master Path holds a reference to all bluprint rooms in an area
        public Path MasterPath { get; private set; }
        // Dictionary used for quick access like checking locations for conflicts and checking locations for room shape conditions
        public Dictionary<Vector3, BlueprintRoom> MasterDictionary { get; private set; }
        
        // ***** Inspector Values *****
        // Enable the map generator
        [Tooltip("Enables map generation.")]
        [SerializeField] private bool _enabled = true;

        [Header("Settings")]
        [Tooltip("The size of a room unit or how large a 1x1 room is in Unity units.")]
        [SerializeField] private float _gridUnitSize = 13;          // The unit size of the room grid's cell
        [SerializeField] private Transform _blueprintRoomContainer;     // Parent transform that contains all the spawned blueprint rooms if debug is on
        [SerializeField] private Transform _roomContainer;              // Parent transform that contains all the spawned rooms

        [Header("Areas")]
        [SerializeField] private Area _castleArea;

        [Header("Debuging")]
        [SerializeField] private bool _debug = false;
        [SerializeField] private GameObject _blueprintGizmoPrefab;
        [SerializeField] private Color _boundingBoxColor;
        [SerializeField] private Color _mainPathColor;
        [SerializeField] private Color _altPathColor;

        // ***** Private Values *****
        private enum DebugState
        {
            Start = 0,
            Initialize,
            GenCriticalRooms,
            GenDivergentRooms,
            GenMainPath,
            GenAltPath,
            GenRooms,
            NotifyListeners,
            Done,
            Failed
        }
        private DebugState _debugState = DebugState.Start;
        private bool _debugGizmos = false;
        private bool _debugLogs = false;

        private Vector3 _currentUpperBound;     // The upper bound of the current area being generated
        private Vector3 _currentLowerBound;     // The lower bound of the current area being generated
        #endregion

        #region Mono
        private void Awake()
        {
            // Handle Singleton
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Another instance of MapGenerator already exists. Deleting Object...");
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
        }

        private void Start()
        {
            // Return if the Map Generator is not enabled
            if (!_enabled)
                return;

            // If debug is active; step through procedures with UI buttons
            if (_debug)
            {
                _debugState = DebugState.Start;        // Jump to next step
                return;
            }

            try
            {
                GenerateLabyrinth();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to generate labyrinth: {e.Message}");
            }

        }
        #endregion

        #region Labyrinth Algorithm Sequence
        /// <summary>
        /// Labyrinth Algorithm, a wrapper algorithm that utalizes the well known drunkard/random walker algorithm (RWA).
        /// Using the RWA the algorithm makes paths that branch out into random directions that can connect to each other via the master path.
        /// Once a blueprint on the grid is made the algorithm then heads into a room check and generate procedure. It checks the shape that
        /// adjacent rooms made during the blueprint procedure and spawns a room if applicable.
        /// </summary>
        public void GenerateLabyrinth()
        {
            // Initialize Master Data Structures
            InitializeMasterPath();

            // TODO: Implement a foreach loop to loop over all areas and generate blueprints

            // Generate blueprint map for area
            GenerateBlueprints(_castleArea);

            // Check room conditions and generate rooms using the blueprint map of the area
            GenerateRooms(_castleArea);

            // TODO: Implement perlin noise height and type Map

            // Generate random loot when the room generation is complete through subscribing to this event
            OnGenerationDone?.Invoke();

            // TODO: Clean Up
            // ClearAllPaths();
        }
        #endregion

        #region Blueprint Procedure
        private void InitializeMasterPath()
        {
            // Initialize Master Data Structures
            MasterDictionary = new Dictionary<Vector3, BlueprintRoom>();
            MasterPath = ScriptableObject.CreateInstance<Path>();
            MasterPath.Initialize(0, 0);
            MasterPath.Name = MASTER_PATH_NAME;
        }

        /// <summary>
        /// First procedure in the Labyrinth Algorithm that will make pseudo paths in different directions.
        /// These paths are basically just lists of positions on the room grid and will be used to generate
        /// the actual rooms later. It is called blueprint because it is a pre-map layout before placing the
        /// actual rooms.
        /// </summary>
        public void GenerateBlueprints(Area area)
        {
            // Must have a area to generate anything
            if (area == null)
            {
                Debug.LogError("Map Generator Error: Area Entry Missing for blueprint procedure.");
                return;
            }

            // Take the volume of the bounding cubic space and return an error if the amount of rooms to spawn is larger than that volume; make sure we have space for needed rooms
            if (!CheckBoundedVolume(area))
            {
                Debug.LogError($"Map Generator Error: The amount of blueprint rooms for area {area.Name} exceeds the bounding box's volume or the bounding box is inverted.");
                return;
            }

            // Update current area bounds to the actual size of the map in Unity Units
            _currentUpperBound = area.UpperBound * _gridUnitSize;
            _currentLowerBound = area.LowerBound * _gridUnitSize;

            // ******* Generate Area Blueprint *******
            // Generate Main Path to boss; TODO: Implement
            // GenerateMainPathBlueprint(area);

            GenerateMainPathBlueprint(area);

            // Ganerate Alternative paths
            GenerateAltPathBlueprints(area);
        }

        /// <summary>
        /// Helper function for generating the main path.
        /// The main path is the path to the area boss and to traversal rooms to other areas
        /// </summary>
        public void GenerateMainPathBlueprint(Area area)
        {
            _debugState = DebugState.GenMainPath;

            if (area.MainPath == null)      // Throw error if MainPath for area does not exist
            {
                Debug.LogError($"Map Generator Error: The Main Path for area {area.name} is not assigned.");
                return;
            }

            // Initialize a new path at starting room if not null
            int startIndex = MasterPath.BlueprintCount() - 1;               // Start index in master path
            int endIndex = startIndex + area.MainPath.PathLength;
            area.MainPath.Initialize(startIndex, endIndex);      // End index in master path

            DrunkardWalk(area.MainPath);
            
            if (_debugLogs) Debug.Log($"Map Generator: {area.Name} generated path {area.MainPath.name} with {area.MainPath.BlueprintCount()} rooms.");
        }

        /* WIP: RANDOM ROOM PLACEMENT ALG
        public void GenerateMainPathBlueprintNew(Area area)
        {
            if (area.MainPath == null)      // Throw error if MainPath for area does not exist
            {
                Debug.LogError($"Map Generator Error: The Main Path for area {area.name} is not assigned.");
                return;
            }

            // Initialize a new path at starting room if not null
            int startIndex = MasterPath.BlueprintCount() - 1;               // Start index in master path
            int endIndex = startIndex + area.MainPath.PathLength;
            area.MainPath.Initialize(startIndex, endIndex);      // End index in master path

            // TODO: Use Simple Room Placement and Bowyer–Watson Algorithm
            // UniqueRoomPlacement(area);
            // RandomRoomPlacement(area.MainPath)
        }

        private void UniqueRoomPlacement(Area area)
        {
            // 1.) Spawn Static Rooms
            foreach (RoomEntry entry in area.UniqueRooms)
            {
                if (entry.PlacementType == RoomPlacementType.Static)
                {
                    Vector3 worldCoordinates = entry.SpawnPosition * _gridUnitSize;
                    // Update current area bounds to the actual size of the map in Unity Units
                    Room newRoom = GenerateSpecificRoom(entry.Prefab, worldCoordinates);

                    //ScanAndShift(newRoom, entry.PlacementType);
                }
            }

            // 2.) Spawn Kinematic Rooms
            foreach (RoomEntry entry in area.UniqueRooms)
            {
                if (entry.PlacementType == RoomPlacementType.Kinematic)
                {

                }
            }

            // 3.) Spawn Dynamic Rooms
            foreach (RoomEntry entry in area.UniqueRooms)
            {
                if (entry.PlacementType == RoomPlacementType.Dynamic)
                {

                }
            }
        }

        private void RandomRoomPlacement(Path path, int numOfUnitSpaces)
        {
            // TODO: random room placement
        }

        /// <summary>
        /// Scan Area for collisions with other rooms and bounds and shift rooms if nessessary
        /// This function is recursive, it will continue to shift rooms like a domino effect until
        /// all rooms are in equalibrium.
        /// </summary>
        /// <param name="room"></param>
        /// <param name="type"></param>
        private void ScanAndShift(Room room, RoomPlacementType type)
        {
            BlueprintRoom collidedRoom = null;

            // TODO: if kinematic room then shift from it's own bounds
            ShiftRoomFromBounds(room);

            if (CheckCollision(room, out collidedRoom))     // Check if the room overlaps another placed room
            {
                switch (type)
                {
                    case RoomPlacementType.Static:      // If a static room collides with another room while being placed it's an automatic error on the developer's part
                        Debug.LogWarning("Map Generator Warning: A static room can not be moved, collision detected.");
                        return;
                    case RoomPlacementType.Dynamic:
                        // TODO: Move Room
                        // ScanAndShift(collidedRoom, collidedRoom.PlacementType);
                        break;
                    case RoomPlacementType.Kinematic:
                        // TODO: Move Room inside bounds
                        // ScanAndShift(collidedRoom, collidedRoom.PlacementType);
                        break;
                    default:
                        Debug.LogError("Map Generator Error: Room Placement Type is invalid.");
                        break;
                }
            }
        }

        // THIS METHOD IS WRONG!!!
        private void ShiftRoomFromBounds(Room room)
        {
            Vector3 pl = room.gameObject.transform.position;
            Vector3 pu = pl + ((room.RoomDimensions - Vector3.one) * _gridUnitSize);

            Vector3 bl = _currentLowerBound;
            Vector3 bu = _currentUpperBound;

            Vector3 upperDiff = bu - pu;
            if (upperDiff.x <= 0)
                upperDiff.x = 0;
            if (upperDiff.y <= 0)
                upperDiff.y = 0;
            if (upperDiff.z <= 0)
                upperDiff.z = 0;

            Vector3 lowerDiff = bl - pl;
            if (lowerDiff.x >= 0)
                lowerDiff.x = 0;
            if (lowerDiff.y >= 0)
                lowerDiff.y = 0;
            if (lowerDiff.z >= 0)
                lowerDiff.z = 0;

            Vector3 shiftAmt = upperDiff + lowerDiff;

            room.gameObject.transform.position += shiftAmt;
        }
        */

        /// <summary>
        /// Helper function for generating the prize path
        /// </summary>
        public void GenerateAltPathBlueprints(Area area)
        {
            if (area.MainPath == null)      // Throw error if MainPath for area does not exist
            {
                Debug.LogError($"Map Generator Error: The Main Path for area {area.name} is not assigned.");
                return;
            }

            // Path to prize room; choose a random start room
            // Initialize a new path at starting room if not null
            int startIndex = area.MainPath.BlueprintCount() - 1;               // Start index in master path
            int endIndex = startIndex + area.MainPath.PathLength;

            foreach (Path path in area.Paths)
            {
                if (path == null)
                {
                    Debug.LogError($"Map Generator Error: A path {path.Name} for area {area.name} is not assigned.");
                    return;
                }

                BlueprintRoom startRoom = ChooseRandomRoom(area.MainPath, 1); // start at index 1 as to not choose the starting room of the game
                path.Initialize(startIndex, endIndex);

                DrunkardWalk(path, startRoom);

                if (_debugLogs) Debug.Log($"Map Generator: {path.name} generated with {path.BlueprintCount()} rooms.");
            }
        }

        /// <summary>
        /// Choose a random room in a path. If endIndex = -1 => endIndex = path's last room.
        /// </summary>
        /// <param name="pathToChooseFrom">The path to choose the starting room from</param>
        /// <param name="startIndex">Index to start from</param>
        /// <returns>The Choosen Blueprint Room.</returns>
        private BlueprintRoom ChooseRandomRoom(Path pathToChooseFrom, int startIndex = 0, int endIndex = -1)
        {
            // Default the endIndex to the path's end index
            if (endIndex == -1)
                endIndex = pathToChooseFrom.BlueprintCount() - 1;

            // Check if range is valid
            if ((startIndex < 0) || (startIndex > endIndex) || (endIndex > (pathToChooseFrom.BlueprintCount() - 1)))
            {
                Debug.LogError("Map Generator Error: Path index out of range or set incorrectly.");
                return null;
            }

            // Check if path to choose from is valid
            if (pathToChooseFrom.BlueprintCount() <= 0)
            {
                Debug.LogError($"Map Generator Error: A starting room could not be choosen because {pathToChooseFrom.Name} has no rooms.");
                return null;
            }

            // TODO: Make a enum/layer mask perameter that can choose a room from a specific type or types

            // Choose a random room respecting the constraints and return
            int randomRoomIndex = UnityEngine.Random.Range(startIndex, endIndex);
            BlueprintRoom room = pathToChooseFrom.BlueprintRooms[randomRoomIndex];

            if (_debugLogs) Debug.Log($"Map Generator: Random room choosen from {pathToChooseFrom.Name} at index {randomRoomIndex}");

            return room;
        }

        /// <summary>
        /// Drunkard Walk Algorithm, will walk a specified length and store it into a newly created path. The algorithm
        /// has been modified to handle collisions and create pseudo paths where rooms can potentially spawn later.
        /// </summary>
        /// <param name="path">A path with a length of atleast one.</param>
        /// <param name="startRoom">The starting room for the path. If null will create it's own start room</param>
        private void DrunkardWalk(Path path, BlueprintRoom startRoom = null)
        {
            // Make sure the path has atleast one room cell that can spawn
            if (path.PathLength <= 0)
            {
                Debug.LogWarning($"Map Generator Error: Path {path.Name} has a length of 0 or is negative");
                return;
            }

            MasterPath.endMasterIdx = path.endMasterIdx;                     // Extend master path's end index

            Vector3 curPos = Vector3.zero;
            BlueprintRoom curRoom = null;

            // Prime loop with starting room
            if (startRoom == null)          // Generate Start Room if a start room was not passed in, generate a start room at position (0,0,0); TODO: Make the start position a desired position if we plan on having places where the player can teleport to.
            {
                curRoom = GenerateBlueprintRoom(path, curPos);
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
                Vector3 tempPos = curPos;

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
                        tempPos += Vector3.right * _gridUnitSize;    // F0 : (1, 0, 0) * Cell Unit Size; Wall Right
                        entrFlagIdx = 0;
                        break;
                    case 1:
                        tempPos += Vector3.left * _gridUnitSize;     // F1 : (-1, 0, 0) * Cell Unit Size; Wall Left
                        entrFlagIdx = 1;
                        break;
                    case 2:
                        tempPos += Vector3.forward * _gridUnitSize;  // F2 : (0, 0, 1) * Cell Unit Size; Wall Forward
                        entrFlagIdx = 2;
                        break;
                    case 3:
                        tempPos += Vector3.back * _gridUnitSize;     // F3 : (0, 0, -1) * Cell Unit Size; Wall Back
                        entrFlagIdx = 3;
                        break;
                    case 4:
                        tempPos += Vector3.up * _gridUnitSize;       // F4 : (0, 1, 0) * Cell Unit Size; Wall Top
                        entrFlagIdx = 4;
                        break;
                    case 5:
                        tempPos += Vector3.down * _gridUnitSize;     // F5 : (0, 1, 0) * Cell Unit Size; Wall Bot
                        entrFlagIdx = 5;
                        break;
                    default:
                        Debug.LogError("Map Generator Error: Direction choosen by gen alg does not exist.");
                        break;
                }

                // Check if the room is in the realm of the bounding box, if not then don't spawn
                if (!CheckBounds(tempPos))
                {
                    // TODO: Enable the stuff below, we need a prev room in order to do this because you cannot set the collided room as the bound
                    // attempts[entrFlagIdx] = true;
                    // failedAttempts++;

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

                // If failed too many times -> try another room (very rare)
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

        /// <summary>
        /// Generate a new blueprint room at the desired location. Add it to the master path and
        /// desired path passed in as an arguement. Generate a blueprint room gizmo if debug is enabled.
        /// </summary>
        /// <param name="path">The desired path to add the new blueprint room to.</param>
        /// <param name="position">The desired position to spawn the new room at.</param>
        /// <returns>The room generated.</returns>
        private BlueprintRoom GenerateBlueprintRoom(Path path, Vector3 position)
        {
            string blueName = $"BlueprintRoom ({MasterPath.BlueprintCount()})";
            BlueprintRoom newRoom = new BlueprintRoom(position, blueName);

            // Visual transparent gizmo around room; (DEPRICATED)
            //if (_debugGizmos) GenerateBlueprintGizmo(position, path.Type, blueName);

            // Update paths with new blueprint room
            path?.Add(newRoom);
            MasterPath?.Add(newRoom);                    // Add to Master List (required)
            MasterDictionary?.Add(position, newRoom);    // Add to Master Dictionary (required)

            return newRoom;
        }

        /// <summary>
        /// Check the bounding box to make sure the generator does not generate blueprint rooms out of the range.
        /// </summary>
        /// <param name="desiredPos">The desired position to spawn the next room</param>
        /// <returns>Returns true if the space is out of bounds and false otherwise.</returns>
        private bool CheckBounds(Vector3 desiredPos)
        {
            Vector3 differenceUpper = _currentUpperBound - desiredPos;
            Vector3 differenceLower = _currentLowerBound - desiredPos;
            if (differenceUpper.x <= 0 || differenceUpper.y <= 0 || differenceUpper.z <= 0)        // Valid space
                return false;
            if (differenceLower.x > 0 || differenceLower.y > 0 || differenceLower.z > 0)        // Valid space
                return false;

            return true;           // Invalid space
        }

        /// <summary>
        /// Check blueprint room for collision with another blueprint room in the Master Dictionary
        /// </summary>
        /// <param name="position"></param>
        /// <param name="collidedRoom"></param>
        /// <returns></returns>
        private bool CheckCollision(Vector3 position, out BlueprintRoom collidedRoom)
        {
            return MasterDictionary.TryGetValue(position, out collidedRoom);
        }

        /// <summary>
        /// Overloaded CheckCollision() function that will check a room with collision with a blueprint room
        /// from the Master Dictionary
        /// </summary>
        /// <param name="room"></param>
        /// <param name="collidedRoom"></param>
        /// <returns></returns>
        private bool CheckCollision(Room room, out BlueprintRoom collidedRoom)
        {
            Vector3 roomPosition = room.gameObject.transform.position;
            collidedRoom = null;

            // TODO: Add padding to condition (x < room.RoomDimensions.x + roomPadding)
            for (int x = 0; x < room.RoomDimensions.x; x++)
            {
                for (int y = 0; y < room.RoomDimensions.y; y++)
                {
                    for (int z = 0; z < room.RoomDimensions.z; z++)
                    {
                        Vector3 currentPos = (new Vector3(x, y, z) * _gridUnitSize) + roomPosition;
                        
                        if (CheckCollision(currentPos, out collidedRoom))       // The room has collided with another room
                        {
                            return true;
                        }
                    }
                }
            }

            // The room has not collided with another room 
            return false;
        }

        /// <summary>
        /// Pass in two rooms and link their entrancways together. 
        /// </summary>
        /// <param name="room1">First blueprint room</param>
        /// <param name="room2">Second blueprint room</param>
        /// <param name="entrFlagIdx">The index of the choosen face of the *first* room.</param>
        private void FlagDoorways(BlueprintRoom room1, BlueprintRoom room2, int entrFlagIdx) // Flag the entranceways to be activated in each room
        {
            // Flag the fact of the next room facing the prev. room
            if (Math.IsEven(entrFlagIdx))                                   // If choosen an even numbered side then set opposite to true (Ex. F4 -> F3 = true)
                room1.entrancewayFlags[entrFlagIdx + 1] = true;
            else                                                        // If choosen an odd numbered side then set opposite to true (Ex. F3 -> F4 = true)
                room1.entrancewayFlags[entrFlagIdx - 1] = true;

            // Flag the face of the prev. room facing the next room
            room2.entrancewayFlags[entrFlagIdx] = true;
        }
        #endregion

        #region RoomGenerationProcedure
        /// <summary>
        /// Second procedure of the Labyrinth Algorithm. Will parse through all of the 
        /// paths and generate rooms based on conditions. These conditions are based on 
        /// room shape chance, room prefab chance, if the room shape will align adiquately to the path, and what path
        /// the room is a part of. It will also activate the entranceways of rooms based on the path's sequence.
        /// </summary>
        public void GenerateRooms(Area area) 
        {
            // Must have an area to generate anything
            if (area == null)
            {
                Debug.LogError($"Map Generator Error: Area Entry Missing for room generation procedure.");
                return;
            }

            // Generate Rooms along main path
            GenerateRoomsOnPath(area.MainPath);

            // Generator Rooms along alt. paths
            foreach (Path path in area.Paths)
                GenerateRoomsOnPath(path);
        }

        //The room case based on the direction of the adjacent/next room.
        private enum RoomDirection
        {
            PosZ = 0,
            NegZ = 1,
            PosX = 2,
            NegX = 3,
            PosY = 4,
            NegY = 5
        }

        private void GenerateRoomsOnPath(Path path)
        {
            if (path == null)      // Throw error if MainPath for area does not exist
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
                if (RoomShapeCondition(indexedRoom, RoomShape.bigRoom, path, out rDir))  // if can spawn B-Room & passed B-Room spawn chance
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
                    Room genRoom = GenerateRoom(RoomShape.bigRoom, rType, path, indexedRoom, rDir); // Spawn B-Room
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

                    Room genRoom = GenerateRoom(RoomShape.tallRoom, rType, path, indexedRoom, rDir); // Spawn T-Room
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

                    Room genRoom = GenerateRoom(RoomShape.longRoom, rType, path, indexedRoom, rDir); // Spawn L-Room
                    path.Add(genRoom);              // Add new room to paths
                    MasterPath.Add(genRoom);
                    if (_debugLogs) Debug.Log($"{path.Name} Generated Long Room: {genRoom.name}");
                }

                // Default: Spawn a Small room at the indexed room's position
                else
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
            Vector3 rightRoomPos = room.Position + (Vector3.right * _gridUnitSize);     // F0: Right
            Vector3 leftRoomPos = room.Position + (Vector3.left * _gridUnitSize);       // F1: Left
            Vector3 fwdRoomPos = room.Position + (Vector3.forward * _gridUnitSize);     // F2: Forward
            Vector3 backRoomPos = room.Position + (Vector3.back * _gridUnitSize);       // F3: Back
            Vector3 topRoomPos = room.Position + (Vector3.up * _gridUnitSize);          // F4: Top
            Vector3 botRoomPos = room.Position + (Vector3.down * _gridUnitSize);        // F5: Bot

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

        /// <summary>
        /// Generate a room based on a path and all information given. Information must be decided beforhand, this function
        /// is very dependant!
        /// </summary>
        /// <param name="shape">The shape type of the room</param>
        /// <param name="rType">The special type of the room</param>
        /// <param name="path">The path the room will be a part of</param>
        /// <param name="originRoom">The room's origin blueprint room</param>
        /// <param name="rDir">The direction code of the room.</param>
        /// <param name="prefabIndex">The prefab index in the room array; set to -1 to spawn a random room.</param>
        /// <returns></returns>
        private Room GenerateRoom(RoomShape shape, RoomType rType, Path path, BlueprintRoom originRoom, RoomDirection rDir = 0, int prefabIndex = -1)      // prefabIndex = -1 means spawn random room
        {
            Room generatedRoom = null;
            Quaternion rotation = Quaternion.identity;      // Take the rotation of the room into account
            Vector3 eulerRotation = Vector3.zero;

            // If starting room then spawn starting room and return
            if (rType == RoomType.start)
            {
                // Generate Small Room; no direction condition needed
                generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.startingRooms), originRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
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
                        BlueprintRoom rightRoom = MasterDictionary[originRoom.Position + (Vector3.right * _gridUnitSize)];      // _>--
                        BlueprintRoom fwdRoom = MasterDictionary[rightRoom.Position + (Vector3.forward * _gridUnitSize)];         // __-^
                        BlueprintRoom leftRoom = MasterDictionary[fwdRoom.Position + (Vector3.left * _gridUnitSize)];             // __<-

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), originRoom.Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(rightRoom.entrancewayFlags, 1, eulerRotation);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(fwdRoom.entrancewayFlags, 2, eulerRotation);               // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintEntranceFlags(leftRoom.entrancewayFlags, 3, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 18 - 23)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.NegX)        // Left, Forward, Right
                    {
                        BlueprintRoom leftRoom = MasterDictionary[originRoom.Position + (Vector3.left * _gridUnitSize)];        // <_--
                        BlueprintRoom fwdRoom = MasterDictionary[leftRoom.Position + (Vector3.forward * _gridUnitSize)];          // __^-
                        BlueprintRoom rightRoom = MasterDictionary[fwdRoom.Position + (Vector3.right * _gridUnitSize)];           // __->
                        
                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), leftRoom.Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(rightRoom.entrancewayFlags, 2, eulerRotation);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintEntranceFlags(fwdRoom.entrancewayFlags, 3, eulerRotation);               // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 18 - 23)
                        generatedRoom.CopyBlueprintEntranceFlags(leftRoom.entrancewayFlags, 0, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 0 - 5)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.PosZ)        // Right, Back, Left
                    {
                        BlueprintRoom rightRoom = MasterDictionary[originRoom.Position + (Vector3.right * _gridUnitSize)];      // __->
                        BlueprintRoom backRoom = MasterDictionary[rightRoom.Position + (Vector3.back * _gridUnitSize)];           // _v--
                        BlueprintRoom leftRoom = MasterDictionary[backRoom.Position + (Vector3.left * _gridUnitSize)];            // <_--

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), leftRoom.Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 3, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 18 - 23)
                        generatedRoom.CopyBlueprintEntranceFlags(rightRoom.entrancewayFlags, 2, eulerRotation);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintEntranceFlags(backRoom.entrancewayFlags, 1, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintEntranceFlags(leftRoom.entrancewayFlags, 0, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 0 - 5)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.NegZ)        // Left, Back, Right
                    {
                        BlueprintRoom leftRoom = MasterDictionary[originRoom.Position + (Vector3.left * _gridUnitSize)];        // __<-
                        BlueprintRoom backRoom = MasterDictionary[leftRoom.Position + (Vector3.back * _gridUnitSize)];            // v_--
                        BlueprintRoom rightRoom = MasterDictionary[backRoom.Position + (Vector3.right * _gridUnitSize)];          // _>--

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), backRoom.Position, rotation, _roomContainer).GetComponent<Room>();
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
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + (Vector3.up * _gridUnitSize)];

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms1x2x1), originRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                             // Activate new rooms entranceways
                    }
                    else if (rDir == RoomDirection.NegY)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + (Vector3.down * _gridUnitSize)];

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms1x2x1), nextRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 1, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                             // Activate new rooms entranceways
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
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + (Vector3.right * _gridUnitSize)];

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), originRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);                          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                            // Activate new rooms entranceways
                    }
                    else if (rDir == RoomDirection.NegX)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + (Vector3.left * _gridUnitSize)];

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), nextRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 1, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                            // Activate new rooms entranceways
                    }
                    else if (rDir == RoomDirection.PosZ)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + (Vector3.forward * _gridUnitSize)];

                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        eulerRotation = new Vector3(0, 90, 0);
                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), originRoom.Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.NegZ)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + (Vector3.back * _gridUnitSize)];

                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        eulerRotation = new Vector3(0, 90, 0);
                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), nextRoom.Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 1, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
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
                    generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms1x1x1), originRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
                    generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);   // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array
                    generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    break;

                // ********* Error **************
                default:
                    Debug.LogError("Map Generator Error: Room Shape Invalid.");
                    break;
            }

            return generatedRoom;
        }

        private Room GenerateSpecificRoom(GameObject prefab, Vector3 placementPosition, RoomDirection rDir = 0)
        {
            Quaternion rotation = Quaternion.identity;      // TODO: set rotation
            Room generatedRoom = Instantiate(prefab, placementPosition, rotation, _roomContainer).GetComponent<Room>();
            generatedRoom.Initialize(generatedRoom.RoomType);
            return generatedRoom;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Checks if the total amount of rooms is valid in a bounded range.
        /// </summary>
        /// <returns>The test success or fail</returns>
        private bool CheckBoundedVolume(Area area)
        {
            // Initialize the total to the MainPath's length first
            float totalCellOcupancy = area.MainPath.PathLength;
            // float totalRooms = _mainPathLength + (_prizePathLength * _amountOfPrizePaths);

            // Add alt. paths
            foreach(Path path in area.Paths)
                totalCellOcupancy += path.PathLength;

            // Calculate the bounded volume and check if amount of room cells taken up exceeds that amount
            float xSize = area.UpperBound.x - area.LowerBound.x;
            float ySize = area.UpperBound.y - area.LowerBound.y;
            float zSize = area.UpperBound.z - area.LowerBound.z;
            float volume = Math.RectangularVolume(xSize, ySize, zSize);

            if (volume < totalCellOcupancy)
                return false;

            return true;
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

        /* CLEAR PATHS (Unused)
        /// <summary>
        /// Clean up path lists to free up memory
        /// </summary>
        void ClearAllPaths()
        {
            MasterPath.ClearBluePrintRooms(); // All paths combined
            MainPath.ClearBluePrintRooms();   // path to Boss Room
            for (int i = 0; i < _amountOfPrizePaths; i++)
                PrizePaths[i].ClearBluePrintRooms();  // paths to prize rooms
            PrizePaths.Clear();
        }
        */
        #endregion

        #region Debug
        private void OnGUI()
        {
            if (!_debug)
                return;

            DebugProcedure();
        }

        /// <summary>
        /// Draw Debug Buttons
        /// </summary>
        private void DebugProcedure()
        {
            if (_debugState == DebugState.Failed)
                return;

            if (_debugState == DebugState.Start)
                _debugState = DebugState.Initialize;

            if (_debugState == DebugState.Initialize)
            {
                // Initialize Master Data Structures
                InitializeMasterPath();

                if (_castleArea == null)
                {
                    Debug.LogError("Map Generator Error: Area Entry Missing.");
                    _debugState = DebugState.Failed;
                    return;
                }

                // Take the volume of the bounding cubic space and return an error if the amount of rooms to spawn is larger than that volume; make sure we have space for needed rooms
                if (!CheckBoundedVolume(_castleArea))
                {
                    Debug.LogError($"Map Generator Error: The amount of blueprint rooms for area {_castleArea.Name} exceeds the bounding box's volume or the bounding box is inverted.");
                    _debugState = DebugState.Failed;
                    return;
                }

                // Update current area bounds to the actual size of the map in Unity Units
                _currentUpperBound = _castleArea.UpperBound * _gridUnitSize;
                _currentLowerBound = _castleArea.LowerBound * _gridUnitSize;

                _debugState = DebugState.GenCriticalRooms;
            }

            if (_debugState == DebugState.GenCriticalRooms)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Critical Rooms"))       // Generates Critical Rooms
                {
                    // Generate Critical Rooms
                    // TODO: Add Critical Room Procedure
                    _debugState = DebugState.GenDivergentRooms;
                }
            }

            if (_debugState == DebugState.GenDivergentRooms)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Divergent Rooms"))        // Generates Divergent Rooms
                {
                    // Generate Critical Rooms
                    // TODO: Add Divergent Room Procedure
                    _debugState = DebugState.GenMainPath;
                }
            }

            if (_debugState == DebugState.GenMainPath)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Main Blueprint Path"))        // Generates main path
                {
                    GenerateMainPathBlueprint(_castleArea);
                    _debugState = DebugState.GenAltPath;

                }
            }

            if (_debugState == DebugState.GenAltPath)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Alt Blueprint Paths"))        // Generates alt paths that diverge from the main path
                {
                    GenerateAltPathBlueprints(_castleArea);
                    _debugState = DebugState.GenRooms;
                }
            }

            if (_debugState == DebugState.GenRooms)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Rooms From Paths"))        // Generates alt paths that diverge from the main path
                {
                    GenerateRooms(_castleArea);
                    _debugState = DebugState.NotifyListeners;
                }
            }

            if (_debugState == DebugState.NotifyListeners)
            {
                // Generate random loot when the room generation is complete through subscribing to this event
                OnGenerationDone?.Invoke();
                _debugState = DebugState.Done;
            }

            if (GUI.Button(new Rect(10, 50, 200, 30), "Show Gizmos"))       // Enables Gizmos
            {
                _debugGizmos = !_debugGizmos;
            }

            if (GUI.Button(new Rect(10, 90, 200, 30), "Show Logs"))       // Enables Logs
            {
                _debugLogs = !_debugLogs;
            }

            if (GUI.Button(new Rect(10, 130, 200, 30), "Reload Scene"))        // Reload the scene
            {
                ScenesManager.Instance.ReloadScene();
            }

            if (_debugState != DebugState.Done)
            {
                if (GUI.Button(new Rect(10, 170, 200, 30), "Do All"))        // Generates everything no matter what state the process is in
                {
                    if (_debugState == DebugState.GenCriticalRooms)
                    {
                        // Generate Critical Rooms
                        // TODO: Add Critical Room Procedure
                        _debugState = DebugState.GenDivergentRooms;
                    }
                    if (_debugState == DebugState.GenDivergentRooms)
                    {
                        // Generate Critical Rooms
                        // TODO: Add Divergent Room Procedure
                        _debugState = DebugState.GenMainPath;
                    }
                    if (_debugState == DebugState.GenMainPath)
                    {
                        GenerateMainPathBlueprint(_castleArea);
                        _debugState = DebugState.GenAltPath;
                    }
                    if (_debugState == DebugState.GenAltPath)
                    {
                        GenerateAltPathBlueprints(_castleArea);
                        _debugState = DebugState.GenRooms;
                    }
                    if (_debugState == DebugState.GenRooms)
                    {
                        GenerateRooms(_castleArea);
                        _debugState = DebugState.NotifyListeners;
                    }
                    if (_debugState == DebugState.NotifyListeners)
                    {
                        // Generate random loot when the room generation is complete through subscribing to this event
                        OnGenerationDone?.Invoke();
                        _debugState = DebugState.Done;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!_debugGizmos)
                return;

            DrawBoundingBox();
            DrawBluePrintGizmos();
        }

        /// <summary>
        /// Draw the bounding box of the generator
        /// </summary>
        private void DrawBoundingBox()
        {
            // Find the centerpoint of the box
            float xPos = (_currentLowerBound.x + _currentUpperBound.x - _gridUnitSize) / 2;
            float yPos = (_currentLowerBound.y + _currentUpperBound.y - _gridUnitSize) / 2;
            float zPos = (_currentLowerBound.z + _currentUpperBound.z - _gridUnitSize) / 2;
            Vector3 centerPoint = new Vector3(xPos, yPos, zPos);

            // Find the size of the box
            float xSize = (_currentUpperBound.x - _currentLowerBound.x);
            float ySize = (_currentUpperBound.y - _currentLowerBound.y);
            float zSize = (_currentUpperBound.z - _currentLowerBound.z);
            Vector3 size = new Vector3(xSize, ySize, zSize);


            Gizmos.color = _boundingBoxColor;
            Gizmos.DrawWireCube(centerPoint, size);
        }

        private void DrawBluePrintGizmos()
        {
            if (_castleArea.MainPath.BlueprintRooms == null)
                return;

            // Draw Gizmos for main path
            foreach (BlueprintRoom room in _castleArea.MainPath.BlueprintRooms)
            {
                Gizmos.color = _castleArea.MainPath.PathGizmoColor;
                Gizmos.DrawCube(room.Position, Vector3.one * _gridUnitSize);
            }

            foreach (Path path in _castleArea.Paths)
            {
                if (path.BlueprintRooms == null)
                    return;

                // Draw Gizmos for alt paths
                foreach (BlueprintRoom room in path.BlueprintRooms)
                {
                    Gizmos.color = path.PathGizmoColor;
                    Gizmos.DrawCube(room.Position, Vector3.one * _gridUnitSize);
                }
            }
        }

        /*  OLD DEBUGGING BLUEPRINT GIZMOS (DEPRICATED)
        /// <summary>
        /// Gizmo to show the paths taken to generate the rooms
        /// </summary>
        /// <param name="roomPos">Center position of the room to be generated</param>
        /// <param name="name">The name of the room; can be blank</param>
        private void GenerateBlueprintGizmo(Vector3 roomPos, PathType type, string name = "BlueprintRoom")
        {
            if (!_debugGizmos) 
                return;

            // Set the Color of the gizmo
            Color color = GetColorForPathType(type);
            GameObject gizmo = Instantiate(_blueprintGizmoPrefab, roomPos, Quaternion.identity, _blueprintRoomContainer);
            gizmo.GetComponent<Renderer>().material.color = color;
            gizmo.name = name;
        }

        /// <summary>
        /// Color of blueprint gizmo depending on path type
        /// </summary>
        private Color GetColorForPathType(PathType type)
        {
            switch (type)
            {
                case PathType.main:
                    return _mainPathColor;
                case PathType.prize:
                    return _altPathColor;
                default:
                    return Color.blue;  // Default color if none matched
            }
        }
        */
        #endregion
    }
}