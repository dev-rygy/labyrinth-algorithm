/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   10/13/2024 
 * Notes:           Room Map Generator
*/
using System.Collections.Generic;
using UnityEngine;

public enum TrailType
{
    master,
    main
}

public class MapGenerator : MonoBehaviour
{
    // Amount of faces on a blueprint room
    const int STAND_ROOM_FACE_COUNT = 6;

    // Singleton Reference
    public static MapGenerator Instance { get; private set; }

    public List<BlueprintRoom> MasterPath { get; private set; }
    public List<BlueprintRoom> MainPath { get; private set; }

    [Header("Settings")]
    [SerializeField] private float _roomGridCellSize = 13;     // The unit size of the room grid's cell

    [Header("Trail Lengths")]
    [SerializeField] private int _mainPathLength;

    [Header("Room Prefabs")]
    [SerializeField] private GameObject _blueprintGizmoPrefab;

    [Header("Debug")]
    [SerializeField] private bool _debug;


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
        MasterPath = new List<BlueprintRoom>();
        MainPath = new List<BlueprintRoom>();

        if (!_debug)            // If debug is active; step through procedures with buttons
            LabyrinthAlg();
    }

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
    }

    /// <summary>
    /// First procedure in the Labyrinth Algorithm that will make pseudo paths in different directions.
    /// These paths are basically just lists of positions on the room grid and will be used to generate
    /// the actual rooms later. It is called blueprint because it is a pre-map layout before placing the
    /// actual rooms.
    /// </summary>
    public void BlueprintProcedure()
    {
        RandomWalker(_mainPathLength, MainPath, null); // Main Path to boss
    }

    /// <summary>
    /// Random Walker Algorithm, will walk a specified length and store it into a path. The algorithm
    /// has been modified to handle collisions create pseudo paths where rooms can potentially
    /// spawn later.
    /// </summary>
    /// <param name="trailLength">The amount of room units the algorithm will walk</param>
    /// <param name="path">A list of room unit positions in the order they were placed</param>
    /// <param name="startRoom">The starting room for the path. If null will create it's own start room</param>
    private void RandomWalker(int trailLength, List<BlueprintRoom> path, BlueprintRoom startRoom = null)
    {
        Vector3 curPos = Vector3.zero;
        Vector3 tempPos = Vector3.zero;
        BlueprintRoom curRoom = null;

        // Prime loop with starting room
        if (startRoom == null)        // Generate Start Room if a start room was not passed in
        {
            BlueprintRoom newRoom = new BlueprintRoom(curPos);

            if (_debug)
            {
                GenerateBlueprintGizmo(curPos);
            }

            // Update paths
            path.Add(newRoom);
            MasterPath.Add(newRoom);

            // Update current Room
            curRoom = newRoom;
        }
        else
        {
            curPos = startRoom.position;
            curRoom = startRoom;
        }

        int failedAttempts = 0;
        while (path.Count < trailLength)
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
                    Debug.LogError("Direction choosen by gen alg does not exist.");
                    break;
            }

            // Check Master Path for colliding rooms (the temp pos is inside another designated room space)
            bool inRoomList = false;
            BlueprintRoom collidedRoom = null;
            foreach (BlueprintRoom room in MasterPath)      // Check all rooms in the Master Path
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

                if (_debug)
                {
                    GenerateBlueprintGizmo(curPos);
                }

                curRoom = newRoom;
                path.Add(newRoom);
                MasterPath.Add(newRoom);

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

    /// <summary>
    /// Clean up path lists to free up memory
    /// </summary>
    void ClearAllPaths()
    {
        MasterPath.Clear(); // All paths combined
        MainPath.Clear();   // path to Boss Room
    }
}
