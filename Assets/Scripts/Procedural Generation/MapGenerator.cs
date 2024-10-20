/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   10/13/2024 
 * Notes:           Room Map Generator
*/
using System.Collections.Generic;
using UnityEngine;

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
        public Vector3 position;
        public bool[] activeEntranceways;

        // Constructor
        public BlueprintRoom(Vector3 postion)
        {
            position = postion;
            activeEntranceways = new bool[6];
        }
    }

    public class Path
    {
        public string name { get; private set; }
        public List<BlueprintRoom> rooms { get; private set; }
        public int startMasterIdx;  // Start index in master path
        public int endMasterIdx;    // End index in master path

        // Constructor for path; gets it's start and end index in the master path
        public Path(string newName, int startIdx, int endIdx)
        {
            name = newName;
            rooms = new List<BlueprintRoom>();

            startMasterIdx = startIdx;
            endMasterIdx = endIdx;
        }

        public int Length()
        {
            return rooms.Count;
        }

        public void AddRoom(BlueprintRoom room)
        {
            rooms.Add(room);
        }

        public void ClearRooms()
        {
            rooms.Clear();
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

        public Path MasterPath;
        public Path MainPath;
        public Path PrizePath;

        [Header("Settings")]
        [SerializeField] private float _roomGridCellSize = 13;     // The unit size of the room grid's cell

        [Header("Path Lengths")]
        [SerializeField] private int _mainPathLength;
        [SerializeField] private int _prizePathLength;

        [Header("Debug")]
        [SerializeField] private bool _debugAll;
        [SerializeField] private bool _debugBlueprint;
        [SerializeField] private GameObject _blueprintGizmoPrefab;
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
            MasterPath = new Path(MASTER_PATH_NAME, 0, 0);

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
        public void BlueprintProcedure()
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
            RandomWalker(_mainPathLength, out MainPath, MAIN_PATH_NAME);
            
            if (_debugAll || _debugBlueprint)
                Debug.Log($"Map Generator: {MainPath.name} generated with {MainPath.Length()} rooms.");
        }

        /// <summary>
        /// Helper function for generating the prize path
        /// </summary>
        public void GeneratePrizePath()
        {
            // Path to prize room; choose a random start room
            BlueprintRoom startRoom = ChooseRandomRoom(MasterPath, 1); // start at index 1 as to not choose the starting room of the game
            RandomWalker(_prizePathLength, out PrizePath, PRIZE_PATH_NAME, startRoom);
            
            if (_debugAll || _debugBlueprint)
                Debug.Log($"Map Generator: {PrizePath.name} generated with {PrizePath.Length()} rooms.");
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
                endIndex = pathToChooseFrom.Length() - 1;

            // Check if range is valid
            if ((startIndex < 0) || (startIndex > endIndex) || (endIndex > (pathToChooseFrom.Length() - 1)))
            {
                Debug.LogError("Map Generator Error: Path index out of range or set incorrectly.");
                return null;
            }
            // Check if path to choose from is valid
            if (pathToChooseFrom.Length() <= 0)
            {
                Debug.LogError("Map Generator Error: Path to choose from has no rooms.");
                return null;
            }

            // TODO: Make a enum/layer mask perameter that can choose a room from a specific type or types

            // Choose a random room respecting the constraints and return
            int randomRoomIndex = UnityEngine.Random.Range(startIndex, endIndex);
            BlueprintRoom room = pathToChooseFrom.rooms[randomRoomIndex];

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
        private void RandomWalker(int desiredLength, out Path path, string pathName = "New Path", BlueprintRoom startRoom = null)
        {
            Vector3 curPos = Vector3.zero;
            Vector3 tempPos = Vector3.zero;
            BlueprintRoom curRoom = null;

            // Initialize a new path
            int startMasterIdx = MasterPath.Length() - 1;               // Start index in master path
            int endMasterIdx = startMasterIdx + desiredLength;
            path = new Path(pathName, startMasterIdx, endMasterIdx);    // End index in master path
            MasterPath.endMasterIdx = endMasterIdx;                     // Update the master path's end index

            // Prime loop with starting room
            if (startRoom == null)        // Generate Start Room if a start room was not passed in
            {
                BlueprintRoom newRoom = new BlueprintRoom(curPos);

                if (_debugAll || _debugBlueprint)
                {
                    GenerateBlueprintGizmo(curPos);
                }

                // Update paths
                path.AddRoom(newRoom);
                MasterPath.AddRoom(newRoom);

                // Update current Room
                curRoom = newRoom;
            }
            else
            {
                curPos = startRoom.position;
                curRoom = startRoom;
            }

            int failedAttempts = 0;

            while (path.Length() < desiredLength)
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
                foreach (BlueprintRoom room in MasterPath.rooms)      // Check all rooms in the Master Path
                {
                    bool hasCollided = Vector3.Equals(tempPos, room.position);
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

                    BlueprintRoom newRoom = new BlueprintRoom(curPos);
                    //FlagDoorways(newRoom, curRoom, entrFlagIdx);

                    if (_debugAll || _debugBlueprint)
                    {
                        GenerateBlueprintGizmo(curPos);
                    }

                    curRoom = newRoom;
                    path.AddRoom(newRoom);
                    MasterPath.AddRoom(newRoom);

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

        /// <summary>
        /// Clean up path lists to free up memory
        /// </summary>
        void ClearAllPaths()
        {
            MasterPath.ClearRooms(); // All paths combined
            MainPath.ClearRooms();   // path to Boss Room
        }
        #endregion

        #region Debug
        /// <summary>
        /// Gizmo to show the paths taken to generate the rooms
        /// </summary>
        /// <param name="roomPos">Center position of the room to be generated</param>
        /// <param name="name">The name of the room; can be blank</param>
        private void GenerateBlueprintGizmo(Vector3 roomPos, string name = "BlueprintRoom")
        {
            GameObject genRoom = Instantiate(_blueprintGizmoPrefab, roomPos, Quaternion.identity) as GameObject;
            genRoom.name = name;
            genRoom.transform.SetParent(transform);
        }
        #endregion
    }
}
