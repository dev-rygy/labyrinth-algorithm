/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   12/26/2024 
 * Notes:           Room Map Generator
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.WSA;

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

    /* OLD PATH CODE (Depricated)
    public enum PathType
    {
        master,
        main,
        prize
    }
    public class Path
    {
        public string name { get; private set; }
        public PathType Type { get; private set; }
        public List<BlueprintRoom> BlueprintRooms { get; private set; }
        public List<Room> Rooms { get; private set; }
        public int startMasterIdx;  // Start index in master path
        public int endMasterIdx;    // End index in master path

        // Constructor for path; gets it's start and end index in the master path
        public Path(string newName, PathType type, int startIdx, int endIdx)
        {
            name = newName;
            Type = type;

            BlueprintRooms = new List<BlueprintRoom>();
            Rooms = new List<Room>();

            startMasterIdx = startIdx;
            endMasterIdx = endIdx;
        }

        public int BlueprintCount()
        {
            return BlueprintRooms.Count;
        }

        public int RoomCount()
        {
            return Rooms.Count;
        }

        public void Add(BlueprintRoom room)
        {
            BlueprintRooms.Add(room);
        }

        public void Add(Room room)
        {
            Rooms.Add(room);
        }

        public void ClearBluePrintRooms()
        {
            BlueprintRooms.Clear();
        }
    }
    */
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
        // Dictionary used for quick access like checking locations for conflicts and checking locations for room shape conditions
        public Dictionary<Vector3, BlueprintRoom> MasterDictionary { get; private set; }
        
        // The Master Path holds a reference to all bluprint rooms in an area
        public Path MasterPath { get; private set; }

        // ***** Inspector Values *****
        // Enable the map generator
        [Tooltip("Enables map generation.")]
        [SerializeField] private bool _enabled = true;

        [Header("Settings")]
        [Tooltip("The size of a room unit or how large a 1x1 room is in Unity units.")]
        [SerializeField] private float _roomGridCellSize = 13;          // The unit size of the room grid's cell
        [SerializeField] private Transform _blueprintRoomContainer;     // GameObject that holds the spawned blueprint rooms if debug is on
        [SerializeField] private Transform _roomContainer;              // GameObject that holds the spawned rooms

        [Header("Bounding Box")]
        [Tooltip("No rooms can spawn past this coordinate point.")]
        [SerializeField] private Vector3 _lowerBound = new Vector3(-1000, -1000, -1000);    // Lower bound; no rooms can spawn beyond this point
        [Tooltip("No rooms can spawn past this coordinate point.")]
        [SerializeField] private Vector3 _upperBound = new Vector3(1000, 1000, 1000);       // Upper bound; no rooms can spawn beyond this point

        [Header("Paths")]
        [SerializeField] public Path MainPath;      // The Main Path is required; Leads to the boss room
        [SerializeField] public List<Path> Paths;   // Alternative paths that branch out from the main path

        [Header("Debuging")]
        [SerializeField] private bool _debugAll;
        [SerializeField] private bool _debugBlueprint;
        [SerializeField] private bool _debugRoomGen;
        [SerializeField] private GameObject _blueprintGizmoPrefab;
        [SerializeField] private Color _boundingBoxColor;
        [SerializeField] private Color _mainPathColor;
        [SerializeField] private Color _prizePathColor;
        #endregion

        #region Mono
        private void Awake()
        {
            // Handle Singleton
            if (Instance && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        private void Start()
        {
            if (!CheckBoundedVolume())
            {
                Debug.LogError("Map Generator Error: The amount of rooms to generate exceeds the bounding box's volume or the bounding box is inverted.");
                return;
            }

            // Update bounds to the actual size of the map in Unity Units
            _upperBound *= _roomGridCellSize;
            _lowerBound *= _roomGridCellSize;

            // If debug is active; step through procedures with UI buttons
            if (_debugAll)
                return;
            
            LabyrinthAlg();
        }
        #endregion

        #region Labyrinth Algorithm Sequence
        /// <summary>
        /// Labyrinth Algorithm, a wrapper algorithm that utalizes the classic drunkard/random walker algorithm (RWA).
        /// Using the RWA the algorithm makes paths that branch out into random directions that can connect to each other via the master path.
        /// Once a blueprint on the grid is made the algorithm then heads into a room check and generate procedure. It checks the shape that
        /// adjacent rooms made during the blueprint procedure and spawns a room if applicable.
        /// </summary>
        public void LabyrinthAlg()
        {
            // Return if the Map Generator is not enabled
            if (!_enabled) 
                return;

            if (MainPath == null)
            {
                Debug.LogError("Map Generator Error: The main path must not be missing.");
                return;
            }

            // Take the volume of the bounding cuboid and return an error if the amount of rooms to spawn is larger than that volume
            if (!CheckBoundedVolume())
            {
                Debug.LogError("Map Generator Error: The amount of rooms to generate exceeds the bounding box's volume or the bounding box is inverted.");
                return;
            }

            // Initialize Master Data Structures
            MasterDictionary = new Dictionary<Vector3, BlueprintRoom>();
            MasterPath = ScriptableObject.CreateInstance<Path>();
            MasterPath.Initialize(0, 0);
            MasterPath.Name = MASTER_PATH_NAME;

            // Generate blueprint map
            BlueprintProcedure();

            // Check room conditions and generate rooms using the blueprint map
            RoomGenerationProcedure();

            // TODO: Implement perlin noise height and type Map

            // Generate random loot when the room generation is complete through subscribing to this event
            OnGenerationDone?.Invoke();

            // TODO: Clean Up
            // ClearAllPaths();
        }
        #endregion

        #region Blueprint Procedure
        /// <summary>
        /// First procedure in the Labyrinth Algorithm that will make pseudo paths in different directions.
        /// These paths are basically just lists of positions on the room grid and will be used to generate
        /// the actual rooms later. It is called blueprint because it is a pre-map layout before placing the
        /// actual rooms.
        /// </summary>
        public void BlueprintProcedure()     // 1. Generate Blueprint Paths
        {
            // Main Path to boss
            GenerateMainPathBlueprint();

            // Alternative paths
            foreach (Path path in Paths)
                GeneratePathBlueprint(path);
        }

        /// <summary>
        /// Helper function for generating the main path
        /// </summary>
        public void GenerateMainPathBlueprint()
        {
            // Main Path to boss
            // Initialize a new path at starting room if not null
            int startIndex = MasterPath.BlueprintCount() - 1;               // Start index in master path
            int endIndex = startIndex + MainPath.PathLength;
            MainPath.Initialize(startIndex, endIndex);      // End index in master path

            RandomWalker(MainPath);
            
            if (_debugAll || _debugBlueprint) Debug.Log($"Map Generator: {MainPath.name} generated with {MainPath.BlueprintCount()} rooms.");
        }

        /// <summary>
        /// Helper function for generating the prize path
        /// </summary>
        public Path GeneratePathBlueprint(Path path)
        {
            // Path to prize room; choose a random start room
            // Initialize a new path at starting room if not null
            int startIndex = MainPath.BlueprintCount() - 1;               // Start index in master path
            int endIndex = startIndex + MainPath.PathLength;
            BlueprintRoom startRoom = ChooseRandomRoom(MasterPath, 1); // start at index 1 as to not choose the starting room of the game
            path.Initialize(startIndex, endIndex);

            RandomWalker(path, startRoom);
            
            if (_debugAll || _debugBlueprint) Debug.Log($"Map Generator: {path.name} generated with {path.BlueprintCount()} rooms.");

            return path;
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
                Debug.LogError("Map Generator Error: Path to choose from has no rooms.");
                return null;
            }

            // TODO: Make a enum/layer mask perameter that can choose a room from a specific type or types

            // Choose a random room respecting the constraints and return
            int randomRoomIndex = UnityEngine.Random.Range(startIndex, endIndex);
            BlueprintRoom room = pathToChooseFrom.BlueprintRooms[randomRoomIndex];

            if (_debugAll || _debugBlueprint) Debug.Log($"Map Generator: Room choosen from {pathToChooseFrom.name} index {randomRoomIndex}");

            return room;
        }

        /// <summary>
        /// Drunkard Walker Algorithm, will walk a specified length and store it into a newly created path. The algorithm
        /// has been modified to handle collisions create pseudo paths where rooms can potentially
        /// spawn later.
        /// </summary>
        /// <param name="path">A list of room unit positions in the order they were placed</param>
        /// <param name="startRoom">The starting room for the path. If null will create it's own start room</param>
        private void RandomWalker(Path path, BlueprintRoom startRoom = null)
        {
            MasterPath.endMasterIdx = path.endMasterIdx;                     // Update the master path's end index

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
            if (_debugAll || _debugBlueprint) Debug.Log($"Starting room for path {path.name} is {startRoom.RoomName}");

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

                // "Walk" in that direction from the curerent pos
                switch (faceIdx)
                {
                    // E0 - E5 is the face count for a unit room, this will be used later for entranceways
                    case 0:
                        tempPos += Vector3.right * _roomGridCellSize;    // F0 : (1, 0, 0) * Cell Unit Size; Wall Right
                        entrFlagIdx = 0;
                        break;
                    case 1:
                        tempPos += Vector3.left * _roomGridCellSize;     // F1 : (-1, 0, 0) * Cell Unit Size; Wall Left
                        entrFlagIdx = 1;
                        break;
                    case 2:
                        tempPos += Vector3.forward * _roomGridCellSize;  // F2 : (0, 0, 1) * Cell Unit Size; Wall Forward
                        entrFlagIdx = 2;
                        break;
                    case 3:
                        tempPos += Vector3.back * _roomGridCellSize;     // F3 : (0, 0, -1) * Cell Unit Size; Wall Back
                        entrFlagIdx = 3;
                        break;
                    case 4:
                        tempPos += Vector3.up * _roomGridCellSize;       // F4 : (0, 1, 0) * Cell Unit Size; Wall Top
                        entrFlagIdx = 4;
                        break;
                    case 5:
                        tempPos += Vector3.down * _roomGridCellSize;     // F5 : (0, 1, 0) * Cell Unit Size; Wall Bot
                        entrFlagIdx = 5;
                        break;
                    default:
                        Debug.LogError("Map Generator Error: Direction choosen by gen alg does not exist.");
                        break;
                }

                // Check if the room is in the realm of the bounding box
                if (!CheckBounds(tempPos))
                {
                    // TODO: Enable the stuff below, we need a prev room in order to do this because you cannot set the collided room as the bound
                    // attempts[entrFlagIdx] = true;
                    // failedAttempts++;

                    if (_debugAll || _debugBlueprint) Debug.Log("Map Generator: Blueprint room was out of bounds so it was not spawned.");
                    continue;
                }

                // Check Master Path for colliding rooms (the temp pos is inside another designated room space)
                BlueprintRoom collidedRoom = null;

                if (MasterDictionary.TryGetValue(tempPos, out collidedRoom))     // Check position in hash map; if failed then flag face attempt and try choosing a new position 
                {
                    attempts[entrFlagIdx] = true;
                    failedAttempts++;
                }
                else                                         // Test Passed; no collision
                {
                    curPos = tempPos; // Change Current Position to new position

                    BlueprintRoom newBlueRoom = GenerateBlueprintRoom(path, curPos);
                    FlagDoorways(newBlueRoom, curRoom, entrFlagIdx);                    // Flag the face that touches the opposite room
                    
                    curRoom = newBlueRoom;

                    // Reset Array
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
        /// Generate a new blueprint room at the desired location. Add it to the master path, main path, and
        /// the desired path. Generate a blueprint room gizmo if debug is enabled.
        /// </summary>
        /// <param name="path">The desired path to add the new blueprint room to.</param>
        /// <param name="position">The desired position to spawn the new room at.</param>
        /// <returns>The room generated.</returns>
        private BlueprintRoom GenerateBlueprintRoom(Path path, Vector3 position)
        {
            string blueName = $"BlueprintRoom ({MasterPath.BlueprintCount()})";
            BlueprintRoom newRoom = new BlueprintRoom(position, blueName);

            if (_debugAll || _debugBlueprint) GenerateBlueprintGizmo(position, path.Type, blueName);

            // Update paths
            path.Add(newRoom);
            MasterPath.Add(newRoom);              // Add to List
            MasterDictionary.Add(position, newRoom);    // Add to Dictionary

            return newRoom;
        }

        /// <summary>
        /// Check the bounding box to make sure the generator does not generate rooms out of the range.
        /// </summary>
        /// <param name="desiredPos">The desired position to spawn the next room</param>
        /// <returns>Returns true if the space is out of bounds and false otherwise.</returns>
        private bool CheckBounds(Vector3 desiredPos)
        {
            Vector3 differenceUpper = _upperBound - desiredPos;
            Vector3 differenceLower = _lowerBound - desiredPos;
            if (differenceUpper.x <= 0 || differenceUpper.y <= 0 || differenceUpper.z <= 0)        // Valid space
                return false;
            if (differenceLower.x > 0 || differenceLower.y > 0 || differenceLower.z > 0)        // Valid space
                return false;

            return true;           // Invalid space
        }

        /// <summary>
        /// Pass in two rooms and link them together 
        /// </summary>
        /// <param name="room1"></param>
        /// <param name="room2"></param>
        /// <param name="entrFlagIdx"></param>
        private void FlagDoorways(BlueprintRoom room1, BlueprintRoom room2, int entrFlagIdx) // Flag the entranceways to be activated in each room
        {
            // Flag the fact of the next room facing the prev. room
            if (entrFlagIdx % 2 == 0)                                   // If choosen an even numbered side (F4) then set opposite (F3) to true
                room1.entrancewayFlags[entrFlagIdx + 1] = true;
            else                                                        // If choosen an odd numbered side (F3) then set opposite (F4) to true
                room1.entrancewayFlags[entrFlagIdx - 1] = true;

            // Flag the face of the prev. room facing the next room
            room2.entrancewayFlags[entrFlagIdx] = true;
        }
        #endregion

        #region RoomGenerationProcedure
        /// <summary>
        /// Second procedure of the Labyrinth Algorithm. Will parse through all of the 
        /// paths and generate rooms based on conditions. These conditions are based on 
        /// room chance, if the room shape will align adiquately to the path, and what path
        /// the room is a part of. It will also activate the entranceways of rooms based on
        /// the path's trail.
        /// </summary>
        public void RoomGenerationProcedure()  // 2. Generate Rooms
        {
            // Generate Rooms along main path
            GenerateRooms(MainPath);

            // Generator Rooms along alt. paths
            foreach (Path path in Paths)
                GenerateRooms(path);
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

        private void GenerateRooms(Path path)
        {
            int indexOffset = 0;
            // If the path has starting room(s) then spawn the start room
            if (path.startingRooms.Count > 0)
            {
                path.Rooms.Add(GenerateRoom(RoomShape.smallRoom, RoomType.start, path, 0, 0));
                path.BlueprintRooms[0].Available = false;
                indexOffset = 1;
            }

            PathType pathType = path.Type;
            // *** Loop through all blueprint rooms ***
            for (int i = 0 + indexOffset; i < path.BlueprintCount(); i++)
            {
                if (path.BlueprintRooms[i].Available)
                {
                    path.BlueprintRooms[i].Available = false;

                    RoomDirection rDir = RoomDirection.PosX;        // Default Room Case
                    RoomType rType = RoomType.general;              // Default Room Type

                    if (RoomShapeCondition(path.BlueprintRooms[i], RoomShape.bigRoom, path, out rDir))  // if can spawn B-Room & passed B-Room spawn chance
                    {
                        // if the next room to be generated is the last room in the trail then make it the toBoss room
                        if ((i + 4) >= path.BlueprintCount())
                        {
                            if (pathType == PathType.main)
                                rType = RoomType.toBoss;
                            else if (pathType == PathType.prize)
                                rType = RoomType.prize;
                        }

                        // spawn B-Room
                        // Hook up blueprintRoom.entrancewayflags to new room
                        Room genRoom = GenerateRoom(RoomShape.bigRoom, rType, path, i, rDir); // Spawn T-Room
                        path.Add(genRoom);              // Add new room to paths
                        MasterPath.Add(genRoom);
                        if (_debugAll || _debugRoomGen) Debug.Log("Main Path Generated Big Room: " + genRoom.name);
                    }
                    // else if can spawn T-Room & passed T-Room spawn chance && extra space for a 1x2x1 at end of trail
                    else if (RoomShapeCondition(path.BlueprintRooms[i], RoomShape.tallRoom, path, out rDir))
                    {
                        // if the next room to be generated is the last room in the trail then make it the toBoss room
                        if ((i + 2) >= path.BlueprintCount())
                        {
                            if (pathType == PathType.main)
                                rType = RoomType.toBoss;
                            else if (pathType == PathType.prize)
                                rType = RoomType.prize;
                        }

                        Room genRoom = GenerateRoom(RoomShape.tallRoom, rType, path, i, rDir); // Spawn T-Room
                        path.Add(genRoom);              // Add new room to paths
                        MasterPath.Add(genRoom);
                        if (_debugAll || _debugRoomGen) Debug.Log("Main Path Generated Tall Room: " + genRoom.name);
                    }
                    // else if can spawn L-Room & passed L-Room spawn chance && extra space for a 2x1x1 at end of trail
                    else if (RoomShapeCondition(path.BlueprintRooms[i], RoomShape.longRoom, path, out rDir))
                    {
                        // if the next room to be generated is the last room in the trail then make it the toBoss room
                        if ((i + 2) >= path.BlueprintCount())
                        {
                            if (pathType == PathType.main)
                                rType = RoomType.toBoss;
                            else if (pathType == PathType.prize)
                                rType = RoomType.prize;
                        }

                        Room genRoom = GenerateRoom(RoomShape.longRoom, rType, path, i, rDir); // Spawn L-Room
                        path.Add(genRoom);              // Add new room to paths
                        MasterPath.Add(genRoom);
                        if (_debugAll || _debugRoomGen) Debug.Log("Main Path Generated Long Room: " + genRoom.name);
                    }
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

                        Room genRoom = GenerateRoom(RoomShape.smallRoom, rType, path, i, 0); // Spawn S-Room
                        path.Add(genRoom);
                        MasterPath.Add(genRoom);
                        if (_debugAll || _debugRoomGen) Debug.Log("Main Path Generated Small Room: " + genRoom.name);
                    }
                }
            }
        }

        /* OLD GENERATE ROOMS ALG (Depricated)
        /// <summary>
        /// Loop through all blueprint rooms in a path and generate rooms based on conditions.
        /// </summary>
        /// <param name="path"></param>
        private void GenerateRooms(Path path)
        {
            PathType pathType = path.Type;

            switch (pathType)
            {
                // ********** Master Path **********
                case PathType.master:
                    Debug.LogWarning("Map Generator Warning: Cannot generate rooms on the Master Path.");
                    break;

                // ********** Main Path **********
                case PathType.main:
                    path.Rooms.Add(GenerateRoom(RoomShape.smallRoom, RoomType.start, path, 0, 0, 0));
                    path.BlueprintRooms[0].Available = false;

                    // *** Loop through all blueprint rooms ***
                    for (int i = 1; i < path.BlueprintCount(); i++)
                    {
                        if (path.BlueprintRooms[i].Available)
                        {
                            path.BlueprintRooms[i].Available = false;

                            RoomDirection rDir = RoomDirection.PosX;        // Default Room Case
                            RoomType rType = RoomType.general;              // Default Room Type

                            if (RoomShapeCondition(path.BlueprintRooms[i], RoomShape.bigRoom, path, out rDir))  // if can spawn B-Room & passed B-Room spawn chance
                            {
                                // if the next room to be generated is the last room in the trail then make it the toBoss room
                                if ((i + 4) >= path.BlueprintCount())
                                    rType = RoomType.toBoss;

                                // spawn B-Room
                                // Hook up blueprintRoom.entrancewayflags to new room
                                Room genRoom = GenerateRoom(RoomShape.bigRoom, rType, path, i, rDir); // Spawn T-Room
                                path.Add(genRoom);              // Add new room to paths
                                MasterPath.Add(genRoom);
                                if (_debugAll || _debugRoomGen) Debug.Log("Main Path Generated Big Room: " + genRoom.name);
                            }
                            // else if can spawn T-Room & passed T-Room spawn chance && extra space for a 1x2x1 at end of trail
                            else if (RoomShapeCondition(path.BlueprintRooms[i], RoomShape.tallRoom, path, out rDir))
                            {
                                // if the next room to be generated is the last room in the trail then make it the toBoss room
                                if ((i + 2) >= path.BlueprintCount())
                                    rType = RoomType.toBoss;

                                Room genRoom = GenerateRoom(RoomShape.tallRoom, rType, path, i, rDir); // Spawn T-Room
                                path.Add(genRoom);              // Add new room to paths
                                MasterPath.Add(genRoom);
                                if (_debugAll || _debugRoomGen) Debug.Log("Main Path Generated Tall Room: " + genRoom.name);
                            }
                            // else if can spawn L-Room & passed L-Room spawn chance && extra space for a 2x1x1 at end of trail
                            else if (RoomShapeCondition(path.BlueprintRooms[i], RoomShape.longRoom, path, out rDir))
                            {
                                // if the next room to be generated is the last room in the trail then make it the toBoss room
                                if ((i + 2) >= path.BlueprintCount())
                                    rType = RoomType.toBoss;

                                Room genRoom = GenerateRoom(RoomShape.longRoom, rType, path, i, rDir); // Spawn L-Room
                                path.Add(genRoom);              // Add new room to paths
                                MasterPath.Add(genRoom);
                                if (_debugAll || _debugRoomGen) Debug.Log("Main Path Generated Long Room: " + genRoom.name);
                            }
                            else
                            {
                                // if the next room to be generated is the last room in the trail then make it the toBoss room
                                if ((i + 1) >= path.BlueprintCount())
                                    rType = RoomType.toBoss;

                                Room genRoom = GenerateRoom(RoomShape.smallRoom, rType, path, i, 0); // Spawn S-Room
                                path.Add(genRoom);
                                MasterPath.Add(genRoom);
                                if (_debugAll || _debugRoomGen) Debug.Log("Main Path Generated Small Room: " + genRoom.name);
                            }
                        }
                    }
                    break;

                // ********** Prize Path **********
                case PathType.prize:
                    // *** Loop through all blueprint rooms ***
                    for (int i = 0; i < path.BlueprintCount(); i++)
                    {
                        if (path.BlueprintRooms[i].Available)
                        {
                            path.BlueprintRooms[i].Available = false;

                            RoomDirection rDir = RoomDirection.PosX;        // Default Room Case
                            RoomType rType = RoomType.general;                      // Default Room Type

                            // Check and spawn B-Rooms
                            if (RoomShapeCondition(path.BlueprintRooms[i], RoomShape.bigRoom, path, out rDir))  // if can spawn B-Room & passed B-Room spawn chance
                            {
                                // if the next room to be generated is the last room in the trail then make it the prize room
                                if ((i + 4) >= path.BlueprintCount())
                                    rType = RoomType.prize;

                                // spawn B-Room
                                // Hook up blueprintRoom.entrancewayflags to new room
                                Room genRoom = GenerateRoom(RoomShape.bigRoom, rType, path, i, rDir); // Spawn T-Room
                                path.Add(genRoom);              // Add new room to paths
                                MasterPath.Add(genRoom);
                                if (_debugAll || _debugRoomGen) Debug.Log("Prize Path Generated Big Room: " + genRoom.name);
                            }
                            // else if can spawn T-Room & passed T-Room spawn chance && extra space for a 1x2x1 at end of trail; SpawnShapeCondition() -> Yes, you can spawn a T-Room there and here's the direction
                            else if (RoomShapeCondition(path.BlueprintRooms[i], RoomShape.tallRoom, path, out rDir))
                            {
                                // if the next room to be generated is the last room in the trail then make it the prize room
                                if ((i + 2) >= path.BlueprintCount())
                                    rType = RoomType.prize;

                                Room genRoom = GenerateRoom(RoomShape.tallRoom, rType, path, i, rDir); // Spawn T-Room
                                path.Add(genRoom);              // Add new room to paths
                                MasterPath.Add(genRoom);
                                if (_debugAll || _debugRoomGen) Debug.Log("Prize Path Generated Tall Room: " + genRoom.name);
                            }
                            // else if can spawn L-Room & passed L-Room spawn chance && extra space for a 2x1x1 at end of trail; SpawnShapeCondition() -> Yes, you can spawn a L-Room there and here's the direction
                            else if (RoomShapeCondition(path.BlueprintRooms[i], RoomShape.longRoom, path, out rDir))
                            {
                                // if the next room to be generated is the last room in the trail then make it the prize room
                                if ((i + 2) >= path.BlueprintCount())
                                    rType = RoomType.prize;

                                Room genRoom = GenerateRoom(RoomShape.longRoom, rType, path, i, rDir); // Spawn L-Room
                                path.Add(genRoom);              // Add new room to paths
                                MasterPath.Add(genRoom);
                                if (_debugAll || _debugRoomGen) Debug.Log("Prize Path Generated Long Room: " + genRoom.name);
                            }
                            else // If no condition holds then spawn a S-Room
                            {
                                // if the next room to be generated is the last room in the trail then make it the prize room
                                if ((i + 1) >= path.BlueprintCount())
                                    rType = RoomType.prize;

                                Room genRoom = GenerateRoom(RoomShape.smallRoom, rType, path, i, 0); // Spawn S-Room
                                path.Add(genRoom);
                                MasterPath.Add(genRoom);
                                if (_debugAll || _debugRoomGen) Debug.Log("Prize Path Generated Small Room: " + genRoom.name);
                            }
                        }
                    }
                    break;

                // ********** Error **********
                default:
                    Debug.LogError("Map Generator Error: Undefinded Path Type");
                    break;
            }
        }
        */

        /// <summary>
        /// Returns true of the room with shape roomShape can be spawned, otherwise returns false.
        /// it also passes out the potential direction of the room so that rotations can be handled acordingly
        /// </summary>
        /// <param name="roomPosition"></param>
        /// <returns></returns>
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
                                    availBlueRooms[1].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueRoomsLeft[3].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueRoomsBwd[0].Available = false;        // Lock room right so it's not used in other checks
                                    rDir = RoomDirection.NegZ;
                                    return true;
                                }
                            }
                        }

                        // If none of these conditions hold then return fail
                        return false;
                    }
                // *********** Tall Room Conditions ***********
                case RoomShape.tallRoom:
                    {
                        // If the path holds no tall room prefabs return false
                        if (path.rooms1x2x1.Count <= 0)
                            return false;

                        // Return fail if room fails roll chance
                        if (roomRoll > path.TallRoomSpawnChance)
                            return false;

                        // A blueprint room exists that's above the current room
                        if (availBlueRooms[4] != null)
                        {
                            availBlueRooms[4].Available = false;        // Lock room above so it's not used in other checks
                            rDir = RoomDirection.PosY;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }

                        // A blueprint room exists that's below the current room
                        if (availBlueRooms[5] != null)
                        {
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

                        // Return fail if room fails roll chance
                        if (roomRoll > path.LongRoomSpawnChance)
                            return false;

                        // A blueprint room exists that's right to the current room
                        if (availBlueRooms[0] != null)
                        {
                            availBlueRooms[0].Available = false;        // Lock room right so it's not used in other checks
                            rDir = RoomDirection.PosX;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }
                        // A blueprint room exists that's left to current room
                        if (availBlueRooms[1] != null)
                        {
                            availBlueRooms[1].Available = false;        // Lock room left so it's not used in other checks
                            rDir = RoomDirection.NegX;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }
                        // A blueprint room exists that's forward from the current room
                        if (availBlueRooms[2] != null)
                        {
                            availBlueRooms[2].Available = false;        // Lock room forward so it's not used in other checks
                            rDir = RoomDirection.PosZ;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                            return true;
                        }
                        // A blueprint room exists that's backward from the current room
                        if (availBlueRooms[3] != null)
                        {
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
        /// Test all spaces adjacent to the room being tested. If a room exists in that space then set 
        /// the return array to the BlueprintRoom Tied to that space.
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        private BlueprintRoom[] CheckAvailableAdjacentRooms(BlueprintRoom room, Path path)
        {
            // Store availRooms here and return. All possible avail rooms are up to the face count (F0 - F5)
            BlueprintRoom[] availBlueRooms = new BlueprintRoom[STANDARD_ROOM_FACE_COUNT];

            // Get the positions of potential adjacent rooms to the room
            Vector3 rightRoomPos = room.Position + (Vector3.right * _roomGridCellSize);     // F0: Right
            Vector3 leftRoomPos = room.Position + (Vector3.left * _roomGridCellSize);       // F1: Left
            Vector3 fwdRoomPos = room.Position + (Vector3.forward * _roomGridCellSize);     // F2: Forward
            Vector3 backRoomPos = room.Position + (Vector3.back * _roomGridCellSize);       // F3: Back
            Vector3 topRoomPos = room.Position + (Vector3.up * _roomGridCellSize);          // F4: Top
            Vector3 botRoomPos = room.Position + (Vector3.down * _roomGridCellSize);        // F5: Bot

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

        /* ***************************** OLD ROOM GENERATOR CODE THAT USED PATHTYPE (Depricated) *********************************************
        /// <summary>
        /// Returns true of the room with shape roomShape can be spawned, otherwise returns false.
        /// it also passes out the potential direction of the room so that rotations can be handled acordingly
        /// </summary>
        /// <param name="roomPosition"></param>
        /// <returns></returns>
        private bool RoomShapeCondition(BlueprintRoom currRoom, RoomShape roomShape, PathType pathType, out RoomDirection rDir)
        {
            float roomRoll = UnityEngine.Random.Range(0, 1.01f);        // Roll for room based on it's % chance of spawning

            BlueprintRoom[] availBlueRooms = CheckAvailableAdjacentRooms(currRoom, pathType);

            switch (roomShape)
            {
                // *********** Big Room Conditions ***********
                case RoomShape.bigRoom:
                {
                    if (roomRoll > _bigRoomSpawnChance)
                    {
                        rDir = 0;
                        return false;
                    }
                    if (availBlueRooms[0] != null)      // 1.) If there is a room on the right
                    {
                        BlueprintRoom[] availBlueRoomsRight = CheckAvailableAdjacentRooms(availBlueRooms[0], pathType);

                        if (availBlueRoomsRight[2] != null)     // a.) If there is a room forward
                        {
                            BlueprintRoom[] availBlueRoomsFwd = CheckAvailableAdjacentRooms(availBlueRoomsRight[2], pathType);

                            if (availBlueRoomsFwd[1] != null)       // I.) If there is a room on the left
                            {
                                    availBlueRooms[0].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueRoomsRight[2].Available = false;        // Lock room right so it's not used in other checks
                                    availBlueRoomsFwd[1].Available = false;        // Lock room right so it's not used in other checks
                                    rDir = RoomDirection.PosX;
                                    return true;
                            }
                        }
                        if (availBlueRoomsRight[3] != null)     // b.) If there is a room backward
                        {
                            BlueprintRoom[] availBlueRoomsBwd = CheckAvailableAdjacentRooms(availBlueRoomsRight[3], pathType);

                            if (availBlueRoomsBwd[1] != null)       // I.) If there is a room on the left
                            {
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
                        BlueprintRoom[] availBlueRoomsLeft = CheckAvailableAdjacentRooms(availBlueRooms[1], pathType);

                        if (availBlueRoomsLeft[2] != null)     // a.) If there is a room forward
                        {
                            BlueprintRoom[] availBlueRoomsFwd = CheckAvailableAdjacentRooms(availBlueRoomsLeft[2], pathType);

                            if (availBlueRoomsFwd[0] != null)       // I.) If there is a room on the right
                            {
                                availBlueRooms[1].Available = false;        // Lock room right so it's not used in other checks
                                availBlueRoomsLeft[2].Available = false;        // Lock room right so it's not used in other checks
                                availBlueRoomsFwd[0].Available = false;        // Lock room right so it's not used in other checks
                                rDir = RoomDirection.NegX;
                                return true;
                            }
                        }
                        if (availBlueRoomsLeft[3] != null)     // b.) If there is a room backward
                        {
                            BlueprintRoom[] availBlueRoomsBwd = CheckAvailableAdjacentRooms(availBlueRoomsLeft[3], pathType);

                            if (availBlueRoomsBwd[0] != null)       // I.) If there is a room on the right
                            {
                                availBlueRooms[1].Available = false;        // Lock room right so it's not used in other checks
                                availBlueRoomsLeft[3].Available = false;        // Lock room right so it's not used in other checks
                                availBlueRoomsBwd[0].Available = false;        // Lock room right so it's not used in other checks
                                rDir = RoomDirection.NegZ;
                                return true;
                            }
                        }
                    }

                    // If none of these conditions hold then return fail
                    rDir = 0;
                    return false;
                }
                // *********** Tall Room Conditions ***********
                case RoomShape.tallRoom:
                {
                    // Return fail if room fails roll chance
                    if (roomRoll > _tallRoomSpawnChance)
                    {
                        rDir = 0;
                        return false;
                    }
                    // A blueprint room exists that's above the current room
                    if (availBlueRooms[4] != null)
                    {
                        availBlueRooms[4].Available = false;        // Lock room above so it's not used in other checks
                        rDir = RoomDirection.PosY;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                        return true;
                    }

                    // A blueprint room exists that's below the current room
                    if (availBlueRooms[5] != null)
                    {
                        availBlueRooms[5].Available = false;        // Lock room below so it's not used in other checks
                        rDir = RoomDirection.NegY;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                        return true;
                    }

                    // If none of these conditions hold then return fail
                    rDir = 0;
                    return false;
                }
                // *********** Long Room Conditions ***********
                case RoomShape.longRoom:
                {
                    // Return fail if room fails roll chance
                    if (roomRoll > _longRoomSpawnChance)
                    {
                        rDir = 0;
                        return false;
                    }

                    // A blueprint room exists that's right to the current room
                    if (availBlueRooms[0] != null)
                    {
                        availBlueRooms[0].Available = false;        // Lock room right so it's not used in other checks
                        rDir = RoomDirection.PosX;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                        return true;
                    }
                    // A blueprint room exists that's left to current room
                    if (availBlueRooms[1] != null)
                    {
                        availBlueRooms[1].Available = false;        // Lock room left so it's not used in other checks
                        rDir = RoomDirection.NegX;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                        return true;
                    }
                    // A blueprint room exists that's forward from the current room
                    if (availBlueRooms[2] != null)
                    {
                        availBlueRooms[2].Available = false;        // Lock room forward so it's not used in other checks
                        rDir = RoomDirection.PosZ;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                        return true;
                    }
                    // A blueprint room exists that's backward from the current room
                    if (availBlueRooms[3] != null)
                    {
                        availBlueRooms[3].Available = false;        // Lock room backward so it's not used in other checks
                        rDir = RoomDirection.NegZ;              // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                        return true;
                    }

                    // If none of these conditions hold then return fail
                    rDir = 0;
                    return false;
                }
                default:
                {
                    Debug.LogError("Map Generator Error: Room condition checked wrong room shape.");
                    rDir = 0;
                    return false;
                }
            }
        }

        /// <summary>
        /// Test all spaces adjacent to the room being tested. If a room exists in that space then set 
        /// the return array to the BlueprintRoom Tied to that space.
        /// </summary>
        /// <param name="room"></param>
        /// <returns></returns>
        private BlueprintRoom[] CheckAvailableAdjacentRooms(BlueprintRoom room, PathType pathType)
        {
            // Store availRooms here and return. All possible avail rooms are up to the face count (F0 - F5)
            BlueprintRoom[] availBlueRooms = new BlueprintRoom[STANDARD_ROOM_FACE_COUNT];

            // Get the positions of potential adjacent rooms to the room
            Vector3 rightRoomPos = room.Position + (Vector3.right * _roomGridCellSize);     // F0: Right
            Vector3 leftRoomPos = room.Position + (Vector3.left * _roomGridCellSize);       // F1: Left
            Vector3 fwdRoomPos = room.Position + (Vector3.forward * _roomGridCellSize);     // F2: Forward
            Vector3 backRoomPos = room.Position + (Vector3.back * _roomGridCellSize);       // F3: Back
            Vector3 topRoomPos = room.Position + (Vector3.up * _roomGridCellSize);          // F4: Top
            Vector3 botRoomPos = room.Position + (Vector3.down * _roomGridCellSize);        // F5: Bot

            // Test each position; if the room does not exist the space is null, otherwise it's set to the Blueprint room tied to the position
            _masterDict.TryGetValue(rightRoomPos, out availBlueRooms[0]);        // F0
            _masterDict.TryGetValue(leftRoomPos, out availBlueRooms[1]);         // F1
            _masterDict.TryGetValue(fwdRoomPos, out availBlueRooms[2]);          // F2
            _masterDict.TryGetValue(backRoomPos, out availBlueRooms[3]);         // F3
            _masterDict.TryGetValue(topRoomPos, out availBlueRooms[4]);          // F4
            _masterDict.TryGetValue(botRoomPos, out availBlueRooms[5]);          // F5

            // Loop through available room spaces and eliminate spaces that have already been taken up by other generated rooms
            for (int i = 0; i < availBlueRooms.Length; i++)
            {
                // If the room is not available due to it being used by another generated room
                // OR if it is not a part of the path in question then remove it from the availBlueRooms list.
                if (availBlueRooms[i] != null && (!availBlueRooms[i].Available || availBlueRooms[i].pathType != pathType))
                    availBlueRooms[i] = null;
            }

            return availBlueRooms;
        }
        */

        private Room GenerateRoom(RoomShape shape, RoomType rType, Path path, int i, RoomDirection rDir, int prefabIndex = -1)      // prefabIndex = -1 means spawn random room
        {
            Room generatedRoom = null;
            Quaternion rotation = Quaternion.identity;      // Take the rotation of the room into account
            Vector3 eulerRotation = Vector3.zero;

            BlueprintRoom startingRoom = path.BlueprintRooms[i];    // x_--

            // The index of variant room in the respective room prefab index
            int roomIndex = 0;

            // If starting room then spawn starting room and return
            if (rType == RoomType.start)
            {
                if (prefabIndex < 0 || prefabIndex >= path.startingRooms.Count)      // if -1 -> spawn random room; if < 0 or over the array's count -> index is out of range so spawn random room
                    roomIndex = UnityEngine.Random.Range(0, path.startingRooms.Count); // Choose a random 1x2x1-Room index from the prefab list of 1x2x1-Rooms
                else
                    roomIndex = prefabIndex;

                // Generate Small Room; no direction condition needed
                generatedRoom = Instantiate(path.startingRooms[roomIndex].RoomPrefab, startingRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
                generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 0, eulerRotation);   // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array
                generatedRoom.Initialize(rType);
                return generatedRoom;
            }

            // If starting room then spawn starting room and return
            if (rType == RoomType.start)
            {
                if (prefabIndex < 0 || prefabIndex >= path.startingRooms.Count)      // if -1 -> spawn random room; if < 0 or over the array's count -> index is out of range so spawn random room
                    roomIndex = UnityEngine.Random.Range(0, path.rooms2x1x2.Count); // Choose a random 1x2x1-Room index from the prefab list of 1x2x1-Rooms
                else
                    roomIndex = prefabIndex;

                // Generate Small Room; no direction condition needed
                generatedRoom = Instantiate(path.rooms1x1x1[roomIndex].RoomPrefab, startingRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
                generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 0, eulerRotation);   // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array
                generatedRoom.Initialize(rType);
                return generatedRoom;
            }

            switch (shape)
            {
                // ********* Big Room **************
                case RoomShape.bigRoom:
                    // Choose a random 1x2x1-Room index from the prefab list of 2x1x2-Rooms if conditions met
                    if (prefabIndex < 0 || prefabIndex >= path.rooms2x1x2.Count)      // if -1 -> spawn random room; if < 0 or over the array's count -> index is out of range so spawn random room
                        roomIndex = UnityEngine.Random.Range(0, path.rooms2x1x2.Count); // Choose a random 1x2x1-Room index from the prefab list of 1x2x1-Rooms
                    else
                        roomIndex = prefabIndex;

                    // Generate Big Room based on it's direction
                    if (rDir == RoomDirection.PosX)     // Right, Forward, Left
                    {
                        BlueprintRoom rightRoom = MasterDictionary[startingRoom.Position + (Vector3.right * _roomGridCellSize)];      // _>--
                        BlueprintRoom fwdRoom = MasterDictionary[rightRoom.Position + (Vector3.forward * _roomGridCellSize)];         // __-^
                        BlueprintRoom leftRoom = MasterDictionary[fwdRoom.Position + (Vector3.left * _roomGridCellSize)];             // __<-

                        generatedRoom = Instantiate(path.rooms2x1x2[roomIndex].RoomPrefab, startingRoom.Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(rightRoom.entrancewayFlags, 1, eulerRotation);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(fwdRoom.entrancewayFlags, 2, eulerRotation);               // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(leftRoom.entrancewayFlags, 3, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 18 - 23)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.NegX)        // Left, Forward, Right
                    {
                        BlueprintRoom leftRoom = MasterDictionary[startingRoom.Position + (Vector3.left * _roomGridCellSize)];        // <_--
                        BlueprintRoom fwdRoom = MasterDictionary[leftRoom.Position + (Vector3.forward * _roomGridCellSize)];          // __^-
                        BlueprintRoom rightRoom = MasterDictionary[fwdRoom.Position + (Vector3.right * _roomGridCellSize)];           // __->
                        
                        generatedRoom = Instantiate(path.rooms2x1x2[roomIndex].RoomPrefab, leftRoom.Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(rightRoom.entrancewayFlags, 2, eulerRotation);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(fwdRoom.entrancewayFlags, 3, eulerRotation);               // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 18 - 23)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(leftRoom.entrancewayFlags, 0, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 0 - 5)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.PosZ)        // Right, Back, Left
                    {
                        BlueprintRoom rightRoom = MasterDictionary[startingRoom.Position + (Vector3.right * _roomGridCellSize)];      // __->
                        BlueprintRoom backRoom = MasterDictionary[rightRoom.Position + (Vector3.back * _roomGridCellSize)];           // _v--
                        BlueprintRoom leftRoom = MasterDictionary[backRoom.Position + (Vector3.left * _roomGridCellSize)];            // <_--

                        generatedRoom = Instantiate(path.rooms2x1x2[roomIndex].RoomPrefab, leftRoom.Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 3, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 18 - 23)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(rightRoom.entrancewayFlags, 2, eulerRotation);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(backRoom.entrancewayFlags, 1, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(leftRoom.entrancewayFlags, 0, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 0 - 5)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.NegZ)        // Left, Back, Right
                    {
                        BlueprintRoom leftRoom = MasterDictionary[startingRoom.Position + (Vector3.left * _roomGridCellSize)];        // __<-
                        BlueprintRoom backRoom = MasterDictionary[leftRoom.Position + (Vector3.back * _roomGridCellSize)];            // v_--
                        BlueprintRoom rightRoom = MasterDictionary[backRoom.Position + (Vector3.right * _roomGridCellSize)];          // _>--

                        generatedRoom = Instantiate(path.rooms2x1x2[roomIndex].RoomPrefab, backRoom.Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 2, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 12 - 17)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(rightRoom.entrancewayFlags, 1, eulerRotation);             // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(backRoom.entrancewayFlags, 0, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(leftRoom.entrancewayFlags, 3, eulerRotation);              // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 18 - 23)
                        generatedRoom.Initialize(rType);
                    }
                    else
                        Debug.LogError("Map Generator Error: Roomcase does not match any valid Tall-Room Cases.");
                    break;
                // ********* Tall Room **************
                case RoomShape.tallRoom:
                    // Choose a random 1x2x1-Room index from the prefab list of 1x2x1-Rooms if conditions met
                    if (prefabIndex < 0 || prefabIndex >= path.rooms1x2x1.Count)      // if -1 -> spawn random room; if < 0 or over the array's count -> index is out of range so spawn random room
                        roomIndex = UnityEngine.Random.Range(0, path.rooms1x2x1.Count); // Choose a random 1x2x1-Room index from the prefab list of 1x2x1-Rooms
                    else
                        roomIndex = prefabIndex;

                    // Generate Tall Room based on it's direction
                    if (rDir == RoomDirection.PosY)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[startingRoom.Position + (Vector3.up * _roomGridCellSize)];

                        generatedRoom = Instantiate(path.rooms1x2x1[roomIndex].RoomPrefab, startingRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 0, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(nextRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                             // Activate new rooms entranceways
                    }
                    else if (rDir == RoomDirection.NegY)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[startingRoom.Position + (Vector3.down * _roomGridCellSize)];

                        generatedRoom = Instantiate(path.rooms1x2x1[roomIndex].RoomPrefab, nextRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 1, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(nextRoom.entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                             // Activate new rooms entranceways
                    }
                    else
                    {
                        Debug.LogError("Map Generator Error: Roomcase does not match any valid Tall-Room Cases.");
                    }
                    break;
                // ********* Long Room **************
                case RoomShape.longRoom:
                    // Choose a random 2x1x1-Room index from the prefab list of 2x1x1-Rooms if conditions met
                    if (prefabIndex < 0 || prefabIndex >= path.rooms2x1x1.Count)      // if -1 -> spawn random room; if < 0 or over the array's count -> index is out of range so spawn random room
                        roomIndex = UnityEngine.Random.Range(0, path.rooms2x1x1.Count); // Choose a random 2x1x1-Room index from the prefab list of 2x1x1-Rooms
                    else
                        roomIndex = prefabIndex;

                    // Generate Long Room based on it's direction
                    if (rDir == RoomDirection.PosX)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[startingRoom.Position + (Vector3.right * _roomGridCellSize)];

                        generatedRoom = Instantiate(path.rooms2x1x1[roomIndex].RoomPrefab, startingRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 0, eulerRotation);                          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(nextRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                            // Activate new rooms entranceways
                    }
                    else if (rDir == RoomDirection.NegX)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[startingRoom.Position + (Vector3.left * _roomGridCellSize)];

                        generatedRoom = Instantiate(path.rooms2x1x1[roomIndex].RoomPrefab, nextRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 1, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(nextRoom.entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                            // Activate new rooms entranceways
                    }
                    else if (rDir == RoomDirection.PosZ)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[startingRoom.Position + (Vector3.forward * _roomGridCellSize)];

                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        eulerRotation = new Vector3(0, 90, 0);
                        generatedRoom = Instantiate(path.rooms2x1x1[roomIndex].RoomPrefab, startingRoom.Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 0, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(nextRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.NegZ)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[startingRoom.Position + (Vector3.back * _roomGridCellSize)];

                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        eulerRotation = new Vector3(0, 90, 0);
                        generatedRoom = Instantiate(path.rooms2x1x1[roomIndex].RoomPrefab, nextRoom.Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 1, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(nextRoom.entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);
                    }
                    else
                        Debug.LogError("Map Generator Error: Roomcase does not match any valid Long-Room Cases.");
                    break;
                // ********* Small Room **************
                case RoomShape.smallRoom:
                    if (path.rooms1x1x1.Count <= 0)
                    {
                        Debug.LogError("Map Generator Error: There must be atleast one 1x1x1 room in every path.");
                        return null;
                    }

                    // Choose a random 1x1x1-Room index from the prefab list of 1x1x1-Rooms if conditions met
                    if (prefabIndex < 0 || prefabIndex >= path.rooms1x1x1.Count)      // if -1 -> spawn random room; if < 0 or over the array's count -> index is out of range so spawn random room
                        roomIndex = UnityEngine.Random.Range(0, path.rooms1x1x1.Count); // Choose a random 1x1x1-Room index from the prefab list of 2x1x1-Rooms
                    else
                        roomIndex = prefabIndex;

                    // Generate Small Room; no direction condition needed
                    generatedRoom = Instantiate(path.rooms1x1x1[roomIndex].RoomPrefab, startingRoom.Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
                    generatedRoom.CopyBlueprintRoomEntranceFlags(startingRoom.entrancewayFlags, 0, eulerRotation);   // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array
                    generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    break;
                // ********* Error **************
                default:
                    Debug.LogError("Map Generator Error: Room Shape Invalid.");
                    break;
            }

            return generatedRoom;
        }

        /* ***************************** OLD ROOM GENERATOR CODE THAT USED NEXT ROOM CHECK (Depricated) *********************************************
         public void RoomGenerationProcedure()  // 2. Generate Rooms
        {
            // Generate Rooms along trails
            GenerateRooms(MainPath);

            for (int i = 0; i < _amountOfPrizePaths; i++)
                GenerateRooms(PrizePaths[i]);
        }

        //The room case based on the direction of the adjacent/next room.
        private enum NextRoomDirection
        {
            PosZ = 0,
            NegZ = 1,
            PosX = 2,
            NegX = 3,
            PosY = 4,
            NegY = 5
        }

        /// <summary>
        /// Loop through all blueprint rooms in a path and generate rooms based on conditions.
        /// </summary>
        /// <param name="path"></param>
        private void GenerateRooms(Path path)
        {
            PathType pathType = path.Type;
            int incAmt = 0;         // Amount to increment the loop by

            switch (pathType)
            {
                // ********** Master Path **********
                case PathType.master:
                    break;
                // ********** Main Path **********
                case PathType.main:
                    path.Rooms.Add(GenerateRoom(RoomShape.smallRoom, RoomType.start, path, 0, 0));

                    for (int i = 1; i < path.BlueprintCount(); i += incAmt)
                    {
                        NextRoomDirection rDir = NextRoomDirection.PosX;             // Default Room Case
                        RoomType rType = RoomType.general;          // Default Room Type

                        incAmt = 1;     // Reset incAmt on each iteration

                        if (false)  // if can spawn B-Room & passed B-Room spawn chance
                        {
                            // spawn B-Room
                            // Hook up blueprintRoom.entrancewayflags to new room
                            incAmt = 4; // jump index to next empty blueprint room
                        }
                        // else if can spawn T-Room & passed T-Room spawn chance && extra space for a 1x2x1 at end of trail
                        else if ((i < path.BlueprintCount() - 1) && SpawnShapeCondition(path.BlueprintRooms[i].Position, path.BlueprintRooms[i + 1].Position, RoomShape.tallRoom, out rDir))
                        {
                            //if (i + 2 >= path.Length()) // if the next room to be generated is the last room in the trail
                            //   rType = RoomType.boss;
                            Room genRoom = GenerateRoom(RoomShape.tallRoom, rType, path, i, rDir); // Spawn T-Room
                            path.Add(genRoom);              // Add new room to paths
                            MasterPath.Add(genRoom);
                            incAmt = 2;         // jump index to next empty blueprint room
                        }
                        // else if can spawn L-Room & passed L-Room spawn chance && extra space for a 2x1x1 at end of trail
                        else if ((i < path.BlueprintCount() - 1) && SpawnShapeCondition(path.BlueprintRooms[i].Position, path.BlueprintRooms[i + 1].Position, RoomShape.longRoom, out rDir))
                        {
                            //if (i + 2 >= path.Length()) // if the next room to be generated is the last room in the trail
                            //    rType = RoomType.ToBoss;
                            Room genRoom = GenerateRoom(RoomShape.longRoom, rType, path, i, rDir); // Spawn H-Room
                            path.Add(genRoom);              // Add new room to paths
                            MasterPath.Add(genRoom);
                            incAmt = 2;         // jump index to next empty blueprint room
                        }
                        else
                        {
                            //if (i + 1 >= path.Length()) // if the next room to be generated is the last room in the trail
                            //    rType = RoomType.ToBoss;
                            Room genRoom = GenerateRoom(RoomShape.smallRoom, rType, path, i, 0); // Spawn G-Room
                            path.Add(genRoom);
                            MasterPath.Add(genRoom);
                        }
                    }
                    break;
                // ********** Prize Path **********
                case PathType.prize:
                    for (int i = 0; i < path.BlueprintCount(); i += incAmt)
                    {
                        NextRoomDirection rDir = NextRoomDirection.PosX;             // Default Room Case
                        RoomType rType = RoomType.general;          // Default Room Type

                        incAmt = 1;     // Reset incAmt on each iteration

                        // TODO: Check and spawn B-Rooms
                        if (false)  // if can spawn B-Room & passed B-Room spawn chance
                        {
                            // spawn B-Room
                            // Hook up blueprintRoom.entrancewayflags to new room
                            incAmt = 4; // jump index to next empty blueprint room
                        }
                        // else if can spawn T-Room & passed T-Room spawn chance && extra space for a 1x2x1 at end of trail
                        else if ((i < path.BlueprintCount() - 1) && SpawnShapeCondition(path.BlueprintRooms[i].Position, path.BlueprintRooms[i + 1].Position, RoomShape.tallRoom, out rDir))
                        {
                            //if (i + 2 >= path.Length()) // if the next room to be generated is the last room in the trail
                            //   rType = RoomType.boss;
                            Room genRoom = GenerateRoom(RoomShape.tallRoom, rType, path, i, rDir); // Spawn T-Room
                            path.Add(genRoom);              // Add new room to paths
                            MasterPath.Add(genRoom);
                            incAmt = 2;         // jump index to next empty blueprint room
                        }
                        // else if can spawn L-Room & passed L-Room spawn chance && extra space for a 2x1x1 at end of trail
                        else if ((i < path.BlueprintCount() - 1) && SpawnShapeCondition(path.BlueprintRooms[i].Position, path.BlueprintRooms[i + 1].Position, RoomShape.longRoom, out rDir))
                        {
                            //if (i + 2 >= path.Length()) // if the next room to be generated is the last room in the trail
                            //    rType = RoomType.ToBoss;
                            Room genRoom = GenerateRoom(RoomShape.longRoom, rType, path, i, rDir); // Spawn H-Room
                            path.Add(genRoom);              // Add new room to paths
                            MasterPath.Add(genRoom);
                            incAmt = 2;         // jump index to next empty blueprint room
                        }
                        else
                        {
                            //if (i + 1 >= path.Length()) // if the next room to be generated is the last room in the trail
                            //    rType = RoomType.ToBoss;
                            Room genRoom = GenerateRoom(RoomShape.smallRoom, rType, path, i, 0); // Spawn G-Room
                            path.Add(genRoom);
                            MasterPath.Add(genRoom);
                        }
                    }
                    break;
                // ********** Error **********
                default:
                    Debug.LogError("Map Generator Error: Undefinded Path Type");
                    break;
            }
        }

        /// <summary>
        /// Test for the direction of the next room. If the direction justifies the spawning of the room shape then return true and the direction
        /// of the next room. Otherwise, return 0 and false. In other words make sure the shape aligns with the path.
        /// </summary>
        /// <param name="originRoomPos"></param>
        /// <param name="nextRoomPos"></param>
        /// <param name="roomShape"></param>
        /// <param name="rDir"></param>
        /// <returns></returns>
        private bool SpawnShapeCondition(Vector3 originRoomPos, Vector3 nextRoomPos, RoomShape roomShape, out NextRoomDirection rDir)
        {
            float roomRoll = Random.Range(0, 1.01f);        // Roll for room based on it's % chance of spawning

            float differenceY = originRoomPos.y - nextRoomPos.y;
            float differenceX = originRoomPos.x - nextRoomPos.x;
            float differenceZ = originRoomPos.z - nextRoomPos.z;

            switch(roomShape)
            {
                // *********** Tall Room Conditions ***********
                case RoomShape.tallRoom:
                    if (roomRoll > _tallRoomSpawnChance)     // If room failed roll then return and don't spawn room
                    {
                        rDir = 0;
                        return false;
                    }

                    if (TestCondition(differenceX, differenceZ, differenceY, true))
                    {
                        // Next Room is above the current room
                        rDir = NextRoomDirection.PosY; // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                        return true;
                    }
                    else if (TestCondition(differenceX, differenceZ, differenceY, false))
                    {
                        // Next Room is below the current room
                        rDir = NextRoomDirection.NegY;
                        return true;
                    }
                    else 
                    {
                        // If none of these conditions hold then return fail
                        rDir = 0;
                        return false;
                    }
                // *********** Long Room Conditions ***********
                case RoomShape.longRoom:
                    if (roomRoll > _longRoomSpawnChance)     // If room failed roll then return and don't spawn room
                    {
                        rDir = 0;
                        return false;
                    }

                    if (TestCondition(differenceZ, differenceY, differenceX, true))
                    {
                        // Next Room is on the right of the current room
                        rDir = NextRoomDirection.PosX;
                        return true;
                    }
                    else if (TestCondition(differenceZ, differenceY, differenceX, false))
                    {
                        // Next Room is on the left of the current room
                        rDir = NextRoomDirection.NegX;
                        return true;
                    }
                    else if (TestCondition(differenceX, differenceY, differenceZ, true))
                    {
                        // Next Room is in front of the current room
                        rDir = NextRoomDirection.PosZ;
                        return true;
                    }
                    else if (TestCondition(differenceX, differenceY, differenceZ, false))
                    {
                        // Next Room is behind the current room
                        rDir = NextRoomDirection.NegZ;
                        return true;
                    }
                    else
                    {
                        // If none of these conditions hold then return fail
                        rDir = 0;
                        return false;
                    }
                default:
                    Debug.LogError("Map Generator Error: Room condition checked wrong room shape.");
                    rDir = 0;
                    return false;
            }
        }

        /// <summary>
        /// SpawnShapeCondition helper function; Tests the coordinates of a given condition; where the next room is from the current room will determine what rCase is applied
        /// to that room.
        /// </summary>
        /// <param name="a">Difference a; Next room is on the same axis as a.</param>
        /// <param name="b">Difference b; Next room is on the same axis as a.</param>
        /// <param name="c">Differnece c; Next room differs on the c axis in the direction of toggle.</param>
        /// <param name="signToggle">The direction to test.</param>
        /// <returns></returns>
        private bool TestCondition(float a, float b, float c, bool signToggle)
        {
            if (signToggle)
            {
                if (a == 0 && b == 0            // if both blueprint rooms have same a value and if both blueprint rooms have same b value
                    && (c <= 0) && (Mathf.Abs(c) <= _roomGridCellSize))      // if difference of c <= 0 and room is direcly adjacent
                    return true;
                else
                    return false;
            }
            else
            {
                if (a == 0 && b == 0            // if both blueprint rooms have same a value and if both blueprint rooms have same b value
                    && (c > 0) && (Mathf.Abs(c) <= _roomGridCellSize))      // if difference of c > 0 and room is direcly adjacent
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Spawn a room given all of the information given from GenerateRooms(). Copy a blueprintRoom's flagged entranceways to the actual room's entranceways matrix.
        /// </summary>
        /// <param name="shape">The shape of the room. Determines how much grid space a room will ocupy.</param>
        /// <param name="rType">The type of room. Determines its gameplay and purpose.</param>
        /// <param name="path">The path the room is in.</param>
        /// <param name="i">The index of the room in the path.</param>
        /// <param name="rDir">The room case based on the direction of the adjacent/next room.</param>
        /// <returns></returns>
        private Room GenerateRoom(RoomShape shape, RoomType rType, Path path, int i, NextRoomDirection rDir)
        {
            Room generatedRoom = null;
            Quaternion rotation = Quaternion.identity;      // Take the rotation of the room into account
            Vector3 eulerRotation = Vector3.zero;

            int roomRoll = 0;

            switch (shape)
            {
                // ********* Small Room **************
                case RoomShape.smallRoom:
                    roomRoll = Random.Range(0, rooms1x1x1.Count);         // Choose a random 1x1x1-Room index from the prefab list of 1x1x1-Rooms
                    generatedRoom = Instantiate(rooms1x1x1[roomRoll], path.BlueprintRooms[i].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
                    generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i].entrancewayFlags, 0, eulerRotation);   // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array
                    generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    break;
                // ********* Long Room **************
                case RoomShape.longRoom:
                    roomRoll = Random.Range(0, rooms2x1x1.Count);         // Choose a random 2x1x1-Room index from the prefab list of 2x1x1-Rooms
                    if (rDir == NextRoomDirection.PosX)
                    {
                        generatedRoom = Instantiate(rooms2x1x1[roomRoll], path.BlueprintRooms[i].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i].entrancewayFlags, 0, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i + 1].entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                            // Activate new rooms entranceways
                    }
                    else if (rDir == NextRoomDirection.NegX)
                    {
                        generatedRoom = Instantiate(rooms2x1x1[roomRoll], path.BlueprintRooms[i + 1].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i].entrancewayFlags, 1, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i + 1].entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                            // Activate new rooms entranceways
                    }
                    else if (rDir == NextRoomDirection.PosZ)
                    {
                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        eulerRotation = new Vector3(0, 90, 0);
                        generatedRoom = Instantiate(rooms2x1x1[roomRoll], path.BlueprintRooms[i].Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i].entrancewayFlags, 0, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i + 1].entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == NextRoomDirection.NegZ)
                    {
                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        eulerRotation = new Vector3(0, 90, 0);
                        generatedRoom = Instantiate(rooms2x1x1[roomRoll], path.BlueprintRooms[i + 1].Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i].entrancewayFlags, 1, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i + 1].entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);
                    }
                    else
                        Debug.LogError("Map Generator Error: Roomcase does not match any valid Long-Room Cases.");
                    break;
                // ********* Tall Room **************
                case RoomShape.tallRoom:
                    roomRoll = Random.Range(0, rooms1x2x1.Count); // Choose a random 1x2x1-Room index from the prefab list of 1x2x1-Rooms
                    if (rDir == NextRoomDirection.PosY)
                    {
                        generatedRoom = Instantiate(rooms1x2x1[roomRoll], path.BlueprintRooms[i].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i].entrancewayFlags, 0, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i + 1].entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                             // Activate new rooms entranceways
                    }
                    else if (rDir == NextRoomDirection.NegY)
                    {
                        generatedRoom = Instantiate(rooms1x2x1[roomRoll], path.BlueprintRooms[i + 1].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i].entrancewayFlags, 1, eulerRotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i + 1].entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                             // Activate new rooms entranceways
                    }
                    else
                    {
                        Debug.LogError("Map Generator Error: Roomcase does not match any valid Tall-Room Cases.");
                    }
                    break;
                // ********* Big Room **************
                case RoomShape.bigRoom:
                    // TODO: Generate Big Room
                    break;
                // ********* Error **************
                default:
                    Debug.LogError("Map Generator Error: Room Shape Invalid.");
                    break;
            }

            return generatedRoom;
        }
        */
        #endregion

        #region Utility
        /// <summary>
        /// Checks if the total amount of rooms is valid in a bounded range.
        /// </summary>
        /// <returns>The test success or fail</returns>
        private bool CheckBoundedVolume()
        {
            float totalRooms = 0;
            // float totalRooms = _mainPathLength + (_prizePathLength * _amountOfPrizePaths);

            foreach(Path path in Paths)
            {
                totalRooms += path.PathLength;
            }

            float xSize = (_upperBound.x - _lowerBound.x);
            float ySize = (_upperBound.y - _lowerBound.y);
            float zSize = (_upperBound.z - _lowerBound.z);
            float volume = Math.RectangularVolume(xSize, ySize, zSize);

            if (volume < totalRooms)
                return false;

            return true;
        }

        /* UNUSED
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
        /// <summary>
        /// Gizmo to show the paths taken to generate the rooms
        /// </summary>
        /// <param name="roomPos">Center position of the room to be generated</param>
        /// <param name="name">The name of the room; can be blank</param>
        private void GenerateBlueprintGizmo(Vector3 roomPos, PathType type, string name = "BlueprintRoom")
        {
            // Set the Color of the gizmo
            Color color = Color.blue;
            switch(type)
            {
                case PathType.main:
                    color = _mainPathColor;
                    break;
                case PathType.prize:
                    color = _prizePathColor;
                    break;
            }

            GameObject gizmo = Instantiate(_blueprintGizmoPrefab, roomPos, Quaternion.identity, _blueprintRoomContainer);
            gizmo.GetComponent<Renderer>().material.color = color;
            gizmo.name = name;
        }

        /// <summary>
        /// Draw the bounding box of the generator
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!_debugAll)
                return;

            // Find the centerpoint of the box
            float xPos = (_lowerBound.x + _upperBound.x - _roomGridCellSize) / 2;
            float yPos = (_lowerBound.y + _upperBound.y - _roomGridCellSize) / 2;
            float zPos = (_lowerBound.z + _upperBound.z - _roomGridCellSize) / 2;
            Vector3 centerPoint = new Vector3(xPos, yPos, zPos);

            // Find the size of the box
            float xSize = (_upperBound.x - _lowerBound.x);
            float ySize = (_upperBound.y - _lowerBound.y);
            float zSize = (_upperBound.z - _lowerBound.z);
            Vector3 size = new Vector3(xSize, ySize, zSize);


            Gizmos.color = _boundingBoxColor;
            Gizmos.DrawWireCube(centerPoint, size);
        }
        #endregion
    }
}