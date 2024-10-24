/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   10/13/2024 
 * Notes:           Room Map Generator
*/
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
        public string roomName;
        public Vector3 Position { get; private set; }
        public bool[] activeEntranceways;

        // Constructor
        public BlueprintRoom(Vector3 postion)
        {
            Position = postion;
            activeEntranceways = new bool[6];
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

        [Header("Path Lengths")]
        [SerializeField] private int _mainPathLength;
        [SerializeField] private int _prizePathLength;

        [Header("Room Prefabs")]
        [SerializeField] private List<GameObject> rooms1x1x1;
        [SerializeField] private List<GameObject> rooms2x1x1;
        [SerializeField] private List<GameObject> rooms1x2x1;
        [SerializeField] private List<GameObject> rooms2x1x2;

        //[Header("Direction Chance")]        // TODO: Maybe implement
        //[SerializeField] [Range(0, 1)] private float upChance;
        //[SerializeField] [Range(0, 1)] private float downChance;
        //[SerializeField] [Range(0, 1)] private float rightChance;
        //[SerializeField] [Range(0, 1)] private float leftChance;
        //[SerializeField] [Range(0, 1)] private float forwardChance;
        //[SerializeField] [Range(0, 1)] private float backChance;


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
        private Path PrizePath;
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

            // TODO: Generate and Assign Rooms using the blueprint
            RoomGenerationProcedure();

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

            // Path to prize room
            GeneratePrizePath();

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
        public void GeneratePrizePath()
        {
            // Path to prize room; choose a random start room
            BlueprintRoom startRoom = ChooseRandomRoom(MasterPath, 1); // start at index 1 as to not choose the starting room of the game
            RandomWalker(_prizePathLength, out PrizePath, PathType.prize, PRIZE_PATH_NAME, startRoom);
            
            if (_debugAll || _debugBlueprint)
                Debug.Log($"Map Generator: {PrizePath.name} generated with {PrizePath.BlueprintCount()} rooms.");
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
            int randomRoomIndex = UnityEngine.Random.Range(startIndex, endIndex);
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
            Vector3 tempPos = Vector3.zero;
            BlueprintRoom curRoom = null;

            // Initialize a new path
            int startMasterIdx = MasterPath.BlueprintCount() - 1;               // Start index in master path
            int endMasterIdx = startMasterIdx + desiredLength;
            path = new Path(pathName, pathType, startMasterIdx, endMasterIdx);    // End index in master path
            MasterPath.endMasterIdx = endMasterIdx;                     // Update the master path's end index

            // Prime loop with starting room
            if (startRoom == null)        // Generate Start Room if a start room was not passed in
            {
                BlueprintRoom newRoom = new BlueprintRoom(curPos);

                if (_debugAll || _debugBlueprint)
                {
                    string blueName = $"BlueprintRoom ({MasterPath.BlueprintCount()})";
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
                curPos = startRoom.Position;
                curRoom = startRoom;
            }

            int failedAttempts = 0;

            while (path.BlueprintCount() < desiredLength)
            {
                // Choose a random direction to be the potential position for the next room.
                int randomDirection = UnityEngine.Random.Range(1, STAND_ROOM_FACE_COUNT + 1);
                switch (randomDirection)        // "Walk" in that direction from the curerent pos
                {
                    // E0 - E5 is the face count for a unit room, this will be used later for entranceways
                    case 1:
                        tempPos += Vector3.right * _roomGridCellSize;    // E0 : (1, 0, 0) * Cell Unit Size
                        break;
                    case 2:
                        tempPos += Vector3.left * _roomGridCellSize;     // E1 : (-1, 0, 0) * Cell Unit Size
                        break;
                    case 3:
                        tempPos += Vector3.forward * _roomGridCellSize;  // E2 : (0, 0, 1) * Cell Unit Size
                        break;
                    case 4:
                        tempPos += Vector3.back * _roomGridCellSize;     // E3 : (0, 0, -1) * Cell Unit Size
                        break;
                    case 5:
                        tempPos += Vector3.up * _roomGridCellSize;       // E4 : (0, 1, 0) * Cell Unit Size
                        break;
                    case 6:
                        tempPos += Vector3.down * _roomGridCellSize;     // E5 : (0, 1, 0) * Cell Unit Size
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

                    BlueprintRoom newBlueRoom = new BlueprintRoom(curPos);
                    //FlagDoorways(newRoom, curRoom, entrFlagIdx);

                    if (_debugAll || _debugBlueprint)
                    {
                        string blueName = $"BlueprintRoom ({MasterPath.BlueprintCount()})";
                        GenerateBlueprintGizmo(curPos, pathType, blueName);
                    }

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
        #endregion

        #region RoomGenerationProcedure
        public void RoomGenerationProcedure()  // 2. Generate Rooms
        {
            // Generate Rooms along trails
            GenerateRooms(MainPath);
            GenerateRooms(PrizePath);
        }

        private enum RoomCase
        {
            PosZ = 0,
            NegZ = 1,
            PosX = 2,
            NegX = 3,
            PosY = 4,
            NegY = 5
        }

        /// <summary>
        /// Loop through paths and generate rooms based on the blueprint room positions.
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
                        float roomRoll = Random.Range(0, 1.01f);
                        RoomCase rCase = RoomCase.PosX;             // Default Room Case
                        RoomType rType = RoomType.general;          // Default Room Type

                        incAmt = 1;     // Reset incAmt on each iteration

                        if (false)  // if can spawn B-Room & passed B-Room spawn chance
                        {
                            // spawn B-Room
                            // Hook up blueprintRoom.entrancewayflags to new room
                            incAmt = 4; // jump index to next empty blueprint room
                        }
                        else if ((roomRoll <= tallRoomChance) && (i < path.BlueprintCount() - 1) && TallRoomSpawnCondition(path.BlueprintRooms[i].Position, path.BlueprintRooms[i + 1].Position, out rCase))  // else if can spawn T-Room & passed T-Room spawn chance && extra space for a 1x2 at end of trail
                        {
                            //if (i + 2 >= path.Length()) // if the next room to be generated is the last room in the trail
                            //   rType = RoomType.boss;
                            Room genRoom = GenerateRoom(RoomShape.tallRoom, rType, path, i, rCase); // Spawn T-Room
                            path.Add(genRoom);
                            MasterPath.Add(genRoom);
                            incAmt = 2; // jump index to next empty blueprint room
                        }
                        else if ((roomRoll <= longRoomChance) && (i < path.BlueprintCount() - 1) && LongRoomSpawnCondition(path.BlueprintRooms[i].Position, path.BlueprintRooms[i + 1].Position, out rCase)) // else if can spawn L-Room & passed L-Room spawn chance && extra space for a 2x1 at end of trail
                        {
                            //if (i + 2 >= path.Length()) // if the next room to be generated is the last room in the trail
                            //    rType = RoomType.ToBoss;
                            Room genRoom = GenerateRoom(RoomShape.longRoom, rType, path, i, rCase); // Spawn L-Room
                            path.Add(genRoom);
                            MasterPath.Add(genRoom);
                            incAmt = 2; // jump index to next empty blueprint room
                        }
                        else
                        {
                            //if (i + 1 >= path.Length()) // if the next room to be generated is the last room in the trail
                            //    rType = RoomType.ToBoss;
                            Room genRoom = GenerateRoom(RoomShape.smallRoom, rType, path, i, 0); // Spawn S-Room
                            path.Add(genRoom);
                            MasterPath.Add(genRoom);
                        }
                    }
                    break;
                // ********** Prize Path **********
                case PathType.prize:
                    for (int i = 0; i < path.BlueprintCount(); i += incAmt)
                    {
                        float roomRoll = Random.Range(0, 1.01f);
                        RoomCase rCase = RoomCase.PosX;             // Default Room Case
                        RoomType rType = RoomType.general;          // Default Room Type

                        incAmt = 1;     // Reset incAmt on each iteration

                        if (false)  // if can spawn B-Room & passed B-Room spawn chance
                        {
                            // spawn B-Room
                            // Hook up blueprintRoom.entrancewayflags to new room
                            incAmt = 4; // jump index to next empty blueprint room
                        }
                        else if ((roomRoll <= tallRoomChance) && (i < path.BlueprintCount() - 1) && TallRoomSpawnCondition(path.BlueprintRooms[i].Position, path.BlueprintRooms[i + 1].Position, out rCase))  // else if can spawn T-Room & passed T-Room spawn chance && extra space for a 1x2 at end of trail
                        {
                            //if (i + 2 >= path.Length()) // if the next room to be generated is the last room in the trail
                            //   rType = RoomType.boss;
                            Room genRoom = GenerateRoom(RoomShape.tallRoom, rType, path, i, rCase); // Spawn T-Room
                            path.Add(genRoom);
                            MasterPath.Add(genRoom);
                            incAmt = 2;         // jump index to next empty blueprint room
                        }
                        else if ((roomRoll <= longRoomChance) && (i < path.BlueprintCount() - 1) && LongRoomSpawnCondition(path.BlueprintRooms[i].Position, path.BlueprintRooms[i + 1].Position, out rCase)) // else if can spawn H-Room & passed H-Room spawn chance && extra space for a 2x1 at end of trail
                        {
                            //if (i + 2 >= path.Length()) // if the next room to be generated is the last room in the trail
                            //    rType = RoomType.ToBoss;
                            Room genRoom = GenerateRoom(RoomShape.longRoom, rType, path, i, rCase); // Spawn H-Room
                            path.Add(genRoom);
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

        bool TallRoomSpawnCondition(Vector3 originRoomPos, Vector3 nextRoomPos, out RoomCase rCase)
        {
            float differenceY = originRoomPos.y - nextRoomPos.y;
            float differenceX = originRoomPos.x - nextRoomPos.x;
            float differenceZ = originRoomPos.z - nextRoomPos.z;

            if (differenceX == 0 && differenceZ == 0            // if both blueprint rooms have same x value and if both blueprint rooms have same z value
                && (differenceY <= 0) && (Mathf.Abs(differenceY) <= _roomGridCellSize))      // if difference of z <= 0
            {
                rCase = RoomCase.PosY; // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                return true;
            }
            else if (differenceX == 0 && differenceZ == 0            // if both blueprint rooms have same x value and if both blueprint rooms have same z value
                && (differenceY > 0) && (Mathf.Abs(differenceY) <= _roomGridCellSize))      // if difference of z <= 0
            {
                rCase = RoomCase.NegY;
                return true;
            }
            else
            {
                rCase = 0;
                return false;
            } // if both rooms differ by cellsize on y
        }

        bool LongRoomSpawnCondition(Vector3 originRoomPos, Vector3 nextRoomPos, out RoomCase rCase)
        {
            float differenceY = originRoomPos.y - nextRoomPos.y;
            float differenceX = originRoomPos.x - nextRoomPos.x;
            float differenceZ = originRoomPos.z - nextRoomPos.z;

            if (differenceZ == 0 && differenceY == 0             // if both rooms on same z value and if both rooms on same y value
                && (differenceX <= 0) && (Mathf.Abs(differenceX) <= _roomGridCellSize))      // if difference of x <= 0 and if the room is directly adjacent
            {
                rCase = RoomCase.PosX;
                return true;
            }
            else if (differenceZ == 0 && differenceY == 0             // if both rooms on same z value and if both rooms on same y value
                    && (differenceX > 0) && (Mathf.Abs(differenceX) <= _roomGridCellSize))       // if difference of x > 0 and if room is direcly adjacent
            {
                rCase = RoomCase.NegX;
                return true;
            }
            else if (differenceX == 0 && differenceY == 0            // if both rooms have same x value and if both rooms on same y value
                && (differenceZ <= 0) && (Mathf.Abs(differenceZ) <= _roomGridCellSize))      // if difference of z <= 0 and if room is direcly adjacent
            {
                rCase = RoomCase.PosZ;                          // Room Case is used to specify the Room's rotation and movement on instantiation (Difference: origin - next)
                return true;
            }
            else if (differenceX == 0 && differenceY == 0            // if both rooms have same x value and if both rooms on same y value
                && (differenceZ > 0) && (Mathf.Abs(differenceZ) <= _roomGridCellSize))      // if difference of z > 0 and if room is direcly adjacent
            {
                rCase = RoomCase.NegZ;
                return true;
            }
            else  // If none of these conditions hold then return fail
            {
                rCase = 0;
                return false;
            }
        }


        private Room GenerateRoom(RoomShape shape, RoomType rType, Path path, int index, RoomCase rCase)
        {
            Room generatedRoom = null;
            Quaternion rotation = Quaternion.identity;      // Take the rotation of the room into account
            int roomRoll = 0;

            switch (shape)
            {
                // ********* Small Room **************
                case RoomShape.smallRoom:
                    roomRoll = Random.Range(0, rooms1x1x1.Count);         // Choose a random 1x1x1-Room index from the prefab list of 1x1x1-Rooms
                    generatedRoom = Instantiate(rooms1x1x1[roomRoll], path.BlueprintRooms[index].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
                    //generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[index].activeEntranceways, 0);           // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array
                    generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    break;
                // ********* Long Room **************
                case RoomShape.longRoom:
                    roomRoll = Random.Range(0, rooms2x1x1.Count);         // Choose a random 2x1x1-Room index from the prefab list of 2x1x1-Rooms
                    if (rCase == RoomCase.PosX)
                    {
                        generatedRoom = Instantiate(rooms2x1x1[roomRoll], path.BlueprintRooms[index].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        //generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[index].activeEntranceways, 0);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        //generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[index + 1].activeEntranceways, 1);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                            // Activate new rooms entranceways
                    }
                    else if (rCase == RoomCase.NegX)
                    {
                        generatedRoom = Instantiate(rooms2x1x1[roomRoll], path.BlueprintRooms[index + 1].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        //generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[index].activeEntranceways, 1);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        //generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[index + 1].activeEntranceways, 0);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                            // Activate new rooms entranceways
                    }
                    else if (rCase == RoomCase.PosZ)
                    {
                        // TODO: PosZ case
                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        generatedRoom = Instantiate(rooms2x1x1[roomRoll], path.BlueprintRooms[index].Position, rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.Initialize(rType);

                    }
                    else if (rCase == RoomCase.NegZ)
                    {
                        // TODO: NegZ case
                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        generatedRoom = Instantiate(rooms2x1x1[roomRoll], path.BlueprintRooms[index + 1].Position, rotation, _roomContainer).GetComponent<Room>();
                    }
                    else
                        Debug.LogError("Map Generator Error: Roomcase does not match any valid Long-Room Cases.");
                    break;
                // ********* Tall Room **************
                case RoomShape.tallRoom:
                    roomRoll = Random.Range(0, rooms1x2x1.Count); // Choose a random 1x2x1-Room index from the prefab list of 1x2x1-Rooms
                    if (rCase == RoomCase.PosY)
                    {
                        generatedRoom = Instantiate(rooms1x2x1[roomRoll], path.BlueprintRooms[index].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        //generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[index].activeEntranceways, 0);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        //generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[index + 1].activeEntranceways, 1);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                             // Activate new rooms entranceways
                    }
                    else if (rCase == RoomCase.NegY)
                    {
                        generatedRoom = Instantiate(rooms1x2x1[roomRoll], path.BlueprintRooms[index + 1].Position, rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        //generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[index].activeEntranceways, 1);       // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        //generatedRoom.CopyBlueprintRoomEntranceFlags(path.BlueprintRooms[index + 1].activeEntranceways, 0);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
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
                    Debug.Log("Map Generator Error: Room Shape Invalid.");
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
            PrizePath.ClearBluePrintRooms();  // path to Prize
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
