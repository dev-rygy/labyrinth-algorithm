/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   10/23/2024 
 * Notes:           Room Map Generator
*/
using System.Buffers.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RyansLibrary.Labyrinth
{
    #region Helper Objects
    public enum PathType
    {
        master,
        main,
        prize
    }

    /// <summary>
    /// Holds the properties of a suedo room that does not actually exist in the world.
    /// Is meant to be replaced by actual rooms later on.
    /// </summary>
    public class BlueprintRoom
    {
        public string RoomName { get; private set; }
        public Vector3 Position { get; private set; }
        public bool[] entrancewayFlags;

        // Constructor
        public BlueprintRoom(Vector3 postion, string roomName = "Blueprint Room")
        {
            RoomName = roomName;
            Position = postion;
            entrancewayFlags = new bool[6];       // A flag to mark which entrances should be open for a room
        }
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
    #endregion

    public class MapGenerator : MonoBehaviour
    {
        #region Variables
        // Amount of faces on a blueprint room
        const int STAND_ROOM_FACE_COUNT = 6;
        const string MASTER_PATH_NAME = "Master Path";
        const string MAIN_PATH_NAME = "Main Path";
        const string PRIZE_PATH_NAME = "Prize Path";

        // Singleton Reference
        public static MapGenerator Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float _roomGridCellSize = 13;     // The unit size of the room grid's cell
        [SerializeField] private Transform _blueprintRoomContainer;
        [SerializeField] private Transform _roomContainer;

        [Header("Path Settings")]
        [SerializeField] private int _mainPathLength;
        [SerializeField] private int _prizePathLength;
        [SerializeField] private int _amountOfPrizePaths = 1;

        [Header("Room Prefabs")]
        [SerializeField] private List<GameObject> rooms1x1x1;
        [SerializeField] private List<GameObject> rooms2x1x1;
        [SerializeField] private List<GameObject> rooms1x2x1;
        [SerializeField] private List<GameObject> rooms2x1x2;

        [Header("Room Chance")]
        [SerializeField] [Range(0, 1)] private float tallRoomChance = 0;
        [SerializeField] [Range(0, 1)] private float longRoomChance = 0;
        [SerializeField] [Range(0, 1)] private float bigRoomChance = 0;


        [Header("Debug")]
        [SerializeField] private bool _debugAll;
        [SerializeField] private bool _debugBlueprint;
        [SerializeField] private bool _debugRoomGen;
        [SerializeField] private GameObject _blueprintGizmoPrefab;
        [SerializeField] private Color _mainPathColor;
        [SerializeField] private Color _prizePathColor;

        private Path MasterPath;
        private Path MainPath;
        private List<Path> PrizePaths;
        #endregion

        #region Mono
        private void Awake()
        {
            // Handle singleton
            if (Instance && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        private void Start()
        {
            // Initialize Master Path
            MasterPath = new Path(MASTER_PATH_NAME, PathType.master, 0, 0);
            PrizePaths = new List<Path>();

            // If debug is active; step through procedures with UI buttons
            if (!_debugAll)
                LabyrinthAlg();
        }
        #endregion

        /// <summary>
        /// Labyrinth Algorithm, a wrapper algorithm that utalizes the classic drunken/random walker algorithm (RWA).
        /// Using the RWA the algorithm makes paths that can connect to each other into formint a 
        /// master path. 
        /// </summary>
        public void LabyrinthAlg()
        {
            // Generate blueprint map
            BlueprintProcedure();

            // Generate and Assign Rooms using the blueprint map
            RoomGenerationProcedure();

            // TODO: Implement Perlin Noise height and type Map

            // TODO: Generate Random Loot

            // TODO: Clean Up
            // ClearAllPaths();
        }

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
            GenerateMainPath();

            // Paths to prize rooms
            for (int i = 0; i < _amountOfPrizePaths; i++)
                PrizePaths.Add(GeneratePrizePath());

            // TODO: Add more paths
        }

        /// <summary>
        /// Helper function for generating the main path
        /// </summary>
        public void GenerateMainPath()
        {
            // Main Path to boss
            RandomWalker(_mainPathLength, out MainPath, PathType.main, MAIN_PATH_NAME);
            
            if (_debugAll || _debugBlueprint)
                Debug.Log($"Map Generator: {MainPath.name} generated with {MainPath.BlueprintCount()} rooms.");
        }

        /// <summary>
        /// Helper function for generating the prize path
        /// </summary>
        public Path GeneratePrizePath()
        {
            Path path;
            // Path to prize room; choose a random start room
            BlueprintRoom startRoom = ChooseRandomRoom(MasterPath, 1); // start at index 1 as to not choose the starting room of the game
            RandomWalker(_prizePathLength, out path, PathType.prize, PRIZE_PATH_NAME, startRoom);
            
            if (_debugAll || _debugBlueprint)
                Debug.Log($"Map Generator: {path.name} generated with {path.BlueprintCount()} rooms.");

            return path;
        }

        /// <summary>
        /// Choose a random room from a path. If endIndex = -1 then endIndex = path's end room
        /// </summary>
        /// <param name="pathToChooseFrom">The path to choose the starting room from</param>
        /// <param name="startIndex">Index to start from</param>
        /// <returns></returns>
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
            int randomRoomIndex = Random.Range(startIndex, endIndex);
            BlueprintRoom room = pathToChooseFrom.BlueprintRooms[randomRoomIndex];

            if (_debugAll || _debugBlueprint)
                Debug.Log($"Map Generator: Room choosen from {pathToChooseFrom.name} index {randomRoomIndex}");

            return room;
        }

        /// <summary>
        /// Random Walker Algorithm, will walk a specified length and store it into a path. The algorithm
        /// has been modified to handle collisions create pseudo paths where rooms can potentially
        /// spawn later.
        /// </summary>
        /// <param name="desiredLength">The amount of room units the algorithm will walk</param>
        /// <param name="path">A list of room unit positions in the order they were placed</param>
        /// <param name="startRoom">The starting room for the path. If null will create it's own start room</param>
        private void RandomWalker(int desiredLength, out Path path, PathType pathType, string pathName = "New Path", BlueprintRoom startRoom = null)
        {
            Vector3 curPos = Vector3.zero;
            BlueprintRoom curRoom = null;

            // Initialize a new path at starting room if not null
            int startMasterIdx = MasterPath.BlueprintCount() - 1;               // Start index in master path
            int endMasterIdx = startMasterIdx + desiredLength;
            path = new Path(pathName, pathType, startMasterIdx, endMasterIdx);    // End index in master path
            MasterPath.endMasterIdx = endMasterIdx;                     // Update the master path's end index

            // Prime loop with starting room
            if (startRoom == null)        // Generate Start Room if a start room was not passed in
            {
                string blueName = $"BlueprintRoom ({MasterPath.BlueprintCount()})";
                BlueprintRoom newRoom = new BlueprintRoom(curPos, blueName);

                if (_debugAll || _debugBlueprint)
                {
                    GenerateBlueprintGizmo(curPos, pathType, blueName);
                }

                // Update paths
                path.Add(newRoom);
                MasterPath.Add(newRoom);

                // Update current Room
                curRoom = newRoom;
            }
            else
            {
                Debug.Log($"Starting room for path {pathName} is {startRoom.RoomName}");
                curPos = startRoom.Position;
                curRoom = startRoom;
            }

            // Chose a position in a random cardinal direction and check for collisions
            int failedAttempts = 0;
            int entrFlagIdx = 0;
            Vector3 tempPos = curPos;
            while (path.BlueprintCount() < desiredLength)
            {
                // Choose a random direction to be the potential position for the next room.
                int randomDirection = Random.Range(1, 5); // STAND_ROOM_FACE_COUNT + 1);
                switch (randomDirection)        // "Walk" in that direction from the curerent pos
                {
                    // E0 - E5 is the face count for a unit room, this will be used later for entranceways
                    case 1:
                        tempPos += Vector3.right * _roomGridCellSize;    // F0 : (1, 0, 0) * Cell Unit Size; Wall Right
                        entrFlagIdx = 0;
                        break;
                    case 2:
                        tempPos += Vector3.left * _roomGridCellSize;     // F1 : (-1, 0, 0) * Cell Unit Size; Wall Left
                        entrFlagIdx = 1;
                        break;
                    case 3:
                        tempPos += Vector3.forward * _roomGridCellSize;  // F2 : (0, 0, 1) * Cell Unit Size; Wall Forward
                        entrFlagIdx = 2;
                        break;
                    case 4:
                        tempPos += Vector3.back * _roomGridCellSize;     // F3 : (0, 0, -1) * Cell Unit Size; Wall Back
                        entrFlagIdx = 3;
                        break;
                    case 5:
                        tempPos += Vector3.up * _roomGridCellSize;       // F4 : (0, 1, 0) * Cell Unit Size; Wall Top
                        entrFlagIdx = 4;
                        break;
                    case 6:
                        tempPos += Vector3.down * _roomGridCellSize;     // F5 : (0, 1, 0) * Cell Unit Size; Wall Bot
                        entrFlagIdx = 5;
                        break;
                    default:
                        Debug.LogError("Map Generator Error: Direction choosen by gen alg does not exist.");
                        break;
                }

                // Check Master Path for colliding rooms (the temp pos is inside another designated room space)
                bool inRoomList = false;
                BlueprintRoom collidedRoom = null;
                foreach (BlueprintRoom room in MasterPath.BlueprintRooms)      // Check all rooms in the Master Path
                {
                    bool hasCollided = Vector3.Equals(tempPos, room.Position);
                    if (hasCollided)                    // Test Failed; room collision
                    {
                        collidedRoom = room;
                        inRoomList = true;
                        failedAttempts++;
                        break;              // Break loop, no need to continue; better performance
                    }
                }

                if (!inRoomList)                        // Test Passed; no collision
                {
                    curPos = tempPos; // Change Current Position to new position

                    string blueName = $"BlueprintRoom ({MasterPath.BlueprintCount()})";
                    BlueprintRoom newBlueRoom = new BlueprintRoom(curPos, blueName);
                    FlagDoorways(newBlueRoom, curRoom, entrFlagIdx);            // Flag the face that touches the opposite room

                    if (_debugAll || _debugBlueprint)
                        GenerateBlueprintGizmo(curPos, pathType, blueName);

                    curRoom = newBlueRoom;
                    path.Add(newBlueRoom);
                    MasterPath.Add(newBlueRoom);

                    failedAttempts = 0;
                }

                // If failed too many times -> try another room (very rare)
                if (failedAttempts >= STAND_ROOM_FACE_COUNT)        // All spaces adjacent to the current room are covered
                {
                    // Make the current room the collided room and try to gen again
                    curPos = tempPos;
                    curRoom = collidedRoom;
                    failedAttempts = 0;
                }
            }
        }

        void FlagDoorways(BlueprintRoom newRoom, BlueprintRoom prevRoom, int entrFlagIdx) // Flag the entranceways to be activated in each room
        {
            // Flag the fact of the next room facing the prev. room
            if (entrFlagIdx % 2 == 0)                                   // If choosen an even numbered side (F4) then set opposite (F3) to true
                newRoom.entrancewayFlags[entrFlagIdx + 1] = true;
            else                                                        // If choosen an odd numbered side (F3) then set opposite (F4) to true
                newRoom.entrancewayFlags[entrFlagIdx - 1] = true;

            // Flag the face of the prev. room facing the next room
            prevRoom.entrancewayFlags[entrFlagIdx] = true;
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
                    if (roomRoll > tallRoomChance)     // If room failed roll then return and don't spawn room
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
                    if (roomRoll > longRoomChance)     // If room failed roll then return and don't spawn room
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
            int roomRoll = 0;

            switch (shape)
            {
                // ********* Small Room **************
                case RoomShape.smallRoom:
                    roomRoll = Random.Range(0, rooms1x1x1.Count);         // Choose a random 1x1x1-Room index from the prefab list of 1x1x1-Rooms
                    generatedRoom = Instantiate(rooms1x1x1[roomRoll], path.BlueprintRooms[i].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
                    generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i].entrancewayFlags, 0, rotation);   // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array
                    generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    break;
                // ********* Long Room **************
                case RoomShape.longRoom:
                    roomRoll = Random.Range(0, rooms2x1x1.Count);         // Choose a random 2x1x1-Room index from the prefab list of 2x1x1-Rooms
                    if (rDir == NextRoomDirection.PosX)
                    {
                        generatedRoom = Instantiate(rooms2x1x1[roomRoll], path.BlueprintRooms[i].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i].entrancewayFlags, 0, rotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i + 1].entrancewayFlags, 1, rotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                            // Activate new rooms entranceways
                    }
                    else if (rDir == NextRoomDirection.NegX)
                    {
                        generatedRoom = Instantiate(rooms2x1x1[roomRoll], path.BlueprintRooms[i + 1].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i].entrancewayFlags, 1, rotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i + 1].entrancewayFlags, 0, rotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                            // Activate new rooms entranceways
                    }
                    else if (rDir == NextRoomDirection.PosZ)
                    {
                        // TODO: PosZ case
                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        generatedRoom = Instantiate(rooms2x1x1[roomRoll], path.BlueprintRooms[i].Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == NextRoomDirection.NegZ)
                    {
                        // TODO: NegZ case
                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        generatedRoom = Instantiate(rooms2x1x1[roomRoll], path.BlueprintRooms[i + 1].Position, rotation, _roomContainer).GetComponent<Room>();
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
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i].entrancewayFlags, 0, rotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i + 1].entrancewayFlags, 1, rotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                             // Activate new rooms entranceways
                    }
                    else if (rDir == NextRoomDirection.NegY)
                    {
                        generatedRoom = Instantiate(rooms1x2x1[roomRoll], path.BlueprintRooms[i + 1].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i].entrancewayFlags, 1, rotation);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[i + 1].entrancewayFlags, 0, rotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
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
        #endregion

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
        #endregion
    }
}
