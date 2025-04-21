/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   04/10/2025 (Ryan)
 * Notes:           Map Generator
*/
using System;
using System.Collections.Generic;
using UnityEngine;

using RyansLibrary.Graphs;
using RyansLibrary.Geometry;
using RyansLibrary.AI;
using static UnityEngine.EventSystems.EventTrigger;

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

    // Entry for connection zones together
    [System.Serializable]
    public class ZoneConnectionEntry
    {
        [field: Header("Zones")]
        [field: SerializeField] public Area AreaA { get; set; }
        [field: SerializeField] public Area AreaB { get; set; }

        [field: Header("Connection Rooms")]
        // Connection rooms (if null then it will choose randomly from the given path below
        [field: SerializeField] public RoomEntry RoomA { get; set; }
        [field: SerializeField] public RoomEntry RoomB { get; set; }

        [field: Header("Connection Path")]
        [field: SerializeField] public Path ConnectionPath { get; set; }
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
        // Keys are in room coords
        public Dictionary<Vector3Int, BlueprintRoom> MasterDictionary { get; private set; }
        
        // ***** Inspector Values *****
        // Enable the map generator
        [Tooltip("Enables map generation.")]
        [SerializeField] private bool _enabled = true;

        [Header("Global Settings")]
        [Tooltip("The size of a room unit or how large a 1x1 room is in Unity units.")]
        [SerializeField] private int _gridUnitSize = 13;                      // The unit size of the room grid's cell
        [SerializeField] private Transform _roomContainer;                      // Parent transform that will contain all the spawned rooms
        [SerializeField] private int _numOfPlacementAttempsBeforeRegen = -1;    // If this number is exceeded then the generator will refresh its entire generation attempt

        [Header("Areas")]
        [SerializeField] private List<Area> _areas;

        [Header("Zone Connection")]
        [SerializeField] private List<ZoneConnectionEntry> _zoneConnections;

        [Header("Debuging")]
        [SerializeField] private bool _debug = false;
        [SerializeField] private GameObject _blueprintGizmoPrefab;
        [SerializeField] private Color _boundingBoxColor;
        [SerializeField] private Color _triangulationColor;
        [SerializeField] private Color _circumcircleColor;
        [SerializeField] private Color _minimumSpanningTreeColor;

        // ***** Private Variables *****
        // TODO: do not make this global in this class, maybe in the Area class?
        private DelaunayTriangulation3D _triangulation;     // A single triangulation structure for a main path
        private List<Edge> _minimumSpanningTree;

        // Debugging
        private enum DebugState
        {
            Start = 0,
            Initialize,
            GenUniqueRooms,
            GenDivergentRooms,
            GenTriangulation,
            GenMainPath,
            GenPaths,
            GenRooms,
            NotifyListeners,
            Done,
            Failed
        }
        private DebugState _debugState = DebugState.Start;
        private bool _debugGizmos = false;
        private bool _debugLogs = false;

        //private Vector3Int _currentUpperBound;     // The upper bound of the current area being generated in room coords
        //private Vector3Int _currentLowerBound;     // The lower bound of the current area being generated in room coords
        #endregion

        #region Mono
        private void Awake()
        {
            // Handle Singleton
            if (Instance != null)
            {
                Debug.LogWarning("Map Generator Warning: Another instance of MapGenerator already exists. Deleting Object...");
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
                // Initialize Master Data Structures
                InitializeMasterPath();

                // Initialize Area Data Structures
                foreach (Area area in _areas)
                    InitializeArea(area);

                GenerateLabyrinth();
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Map Generator Error: Failed to generate labyrinth: {e.Message}");
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
            // ******* Generate Blueprints *******
            // Generate Zone Connection Paths
            foreach (ZoneConnectionEntry entry in _zoneConnections)
            {
                GenerateZoneConnectionBlueprint(entry);
            }

            // Generate blueprint map for each area
            foreach (Area area in _areas)
            {
                GenerateAreaBlueprints(area);
            }

            // ******* Generate Rooms *******
            // Generate Zone Connection Rooms
            foreach (ZoneConnectionEntry entry in _zoneConnections)
            {
                // Generate actual rooms for the zone connection
                GenerateZoneConnectionRooms(entry);
            }

            // Spawn rooms based on the blueprint map for each area
            foreach (Area area in _areas)
            {

                // Check room conditions and generate rooms using the blueprint map of the area
                GenerateAreaRooms(area);

                // TODO: Implement perlin noise height and type Map

                // Generate random loot when the room generation is complete through subscribing to this event
                OnGenerationDone?.Invoke();

                // TODO: Clean Up
                // ClearAllPaths();
            }
        }
        #endregion

        #region Blueprint Procedure
        private void InitializeMasterPath()     // NOTE: This must be done before generating anything!
        {
            // Initialize Master Data Structures
            MasterDictionary = new Dictionary<Vector3Int, BlueprintRoom>();
            MasterPath = ScriptableObject.CreateInstance<Path>();
            MasterPath.Initialize();
            MasterPath.Name = MASTER_PATH_NAME;
        }

        private void InitializeArea(Area area)
        {
            area.MainPath.Initialize();
        }

        /// <summary>
        /// First procedure in the Labyrinth Algorithm that will make pseudo paths in different directions.
        /// These paths are basically just lists of positions on the room grid and will be used to generate
        /// the actual rooms later. It is called blueprint because it is a pre-map layout before placing the
        /// actual rooms.
        /// </summary>
        public void GenerateAreaBlueprints(Area area)
        {
            // Must have a area to generate anything
            if (area == null)
            {
                Debug.LogError("Map Generator Error: Area Entry Missing for blueprint procedure.");
                return;
            }

            // Take the volume of the bounding cubic space and return an error if the amount of rooms to spawn is larger than that volume; make sure we have space for needed rooms
            if (!CheckAreaBoundedVolume(area))
            {
                Debug.LogError($"Map Generator Error: The amount of blueprint rooms for area {area.Name} exceeds the bounding box's volume or the bounding box is inverted.");
                return;
            }

            // ******* Generate Area Blueprints *******
            // Generate Main Path to boss
            GenerateMainPathBlueprint(area);

            // Ganerate Alternative paths
            GenerateAltPathBlueprints(area);            
        }

        /// <summary>
        /// Wrapper function for generating the main path.
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

            // Unique Room Placement
            PlaceUniqueRooms(area);

            // Divergent Room Placement
            PlaceDivergentRooms(area);

            // Generate Delauney Triangulation
            GenerateTriangulation(area);

            // Pathfind and Connect Main Path
            ConnectMainPath(area);

            if (_debugLogs) Debug.Log($"Map Generator: {area.Name} generated path {area.MainPath.name} with {area.MainPath.BlueprintCount()} rooms.");
        }

        /// <summary>
        /// Wrapper function for generating the prize path
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
            int startIndex = area.MainPath.BlueprintCount() - 1;              // Start index in master path
            int endIndex = startIndex + area.MainPath.PathLength;

            foreach (Path path in area.Paths)
            {
                if (path == null)
                {
                    Debug.LogError($"Map Generator Error: A path {path.Name} for area {area.name} is not assigned.");
                    return;
                }

                BlueprintRoom startRoom = ChooseRandomRoomOnPath(area.MainPath, 1); // start at index 1 as to not choose the starting room of the game
                path.Initialize(startIndex, endIndex);

                DrunkardWalk(path, area.Bounds, startRoom);

                if (_debugLogs) Debug.Log($"Map Generator: {path.name} generated with {path.BlueprintCount()} rooms.");
            }
        }

        #region Main Path Blueprint Generation
        #region Unique Room Placement
        private bool PlaceUniqueRooms(Area area)
        {
            // 1.) Spawn Fixed Rooms
            foreach (RoomEntry entry in area.UniqueRooms)
            {
                if (entry.PlacementType == RoomPlacementType.Fixed)
                {
                    bool hasPlaced = PlaceFixedRoomBlueprints(entry, area);

                    if (!hasPlaced)
                    {
                        // Fixed room failed to generate, stop all operations
                        Debug.LogError($"Map Generator Error: Fixed Room was outside of bounds and could not be placed.");
                        return false;
                    }
                }
            }

            // 2.) Spawn Constrained Rooms
            foreach (RoomEntry entry in area.UniqueRooms)
            {
                bool hasPlaced = false;
                int attempts = 0;

                if (entry.PlacementType == RoomPlacementType.Constrained)
                {
                    // Attempt to place the constrained room in it's bounded area; if not then break the function
                    // and return false
                    while (!hasPlaced)
                    {
                        if (attempts++ > _numOfPlacementAttempsBeforeRegen)
                        {
                            // TODO: Clear all data and regenerate the map
                            Debug.LogWarning("Map Generator Warning: Constrained Room has exceeded the maximum number of placement attempts.");
                            return false;
                        }

                        hasPlaced = PlaceConstrainedRoomBlueprints(entry, area);
                    }
                }
            }

            // 3.) Spawn Free Rooms
            foreach (RoomEntry entry in area.UniqueRooms)
            {
                bool hasPlaced = false;
                int attempts = 0;

                if (entry.PlacementType == RoomPlacementType.Free)
                {
                    // Place Free Rooms
                    // Attempt to place the constrained room in it's bounded area; if not then break the function
                    // and return false
                    while (!hasPlaced)
                    {
                        if (attempts++ > _numOfPlacementAttempsBeforeRegen)
                        {
                            // TODO: Clear all data and regenerate the map
                            Debug.LogWarning("Map Generator Warning: Constrained Room has exceeded the maximum number of placement attempts.");
                            return false;
                        }

                        hasPlaced = PlaceFreeRoomBlueprints(entry, area);
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Will place rooms randomly in an area but will pull rooms randomly from the main path
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>
        private bool PlaceDivergentRooms(Area area)
        {
            Path mainPath = area.MainPath;
            int occupancy = area.DivergentRoomsCellOccupancy;

            // TODO: Remove these chances later; change them from being hard coded like this
            float chance2x1x2 = 0.20f;
            float chance1x2x1 = 0.40f;
            float chance2x1x1 = 0.60f;

            int indexOffset = 1;        // The amount to increment the loop by
            for (int i = 0; i < occupancy; i += indexOffset)
            {
                int spaceLeft = occupancy - i;
                float roomRoll = UnityEngine.Random.Range(0, 1.01f);        // Roll for room based on it's % chance of spawning
                Vector3Int dimensions = Vector3Int.one;         // Dimensions of a Small Room

                // If there is enough room to spawn a Big Room and the roll was right
                if (spaceLeft < 4 && roomRoll < chance2x1x2)
                {
                    dimensions = new Vector3Int(2, 1, 2);        // Dimensions of a Big Room
                    indexOffset = 4;
                }
                else if (spaceLeft < 2 && roomRoll < chance1x2x1)
                {
                    dimensions = new Vector3Int(1, 2, 1);        // Dimensions of a Tall Room
                    indexOffset = 2;
                }
                else if (spaceLeft < 2 && roomRoll < chance2x1x1)
                {
                    dimensions = new Vector3Int(2, 1, 1);        // Dimensions of a Long Room
                    indexOffset = 2;
                }
                else                                                
                {
                    indexOffset = 1;
                }

                // Adjust the upper bounds so that the room's volume will properly fit within the bounded space
                Vector3Int adjUpperBound = new Vector3Int(
                    area.Bounds.xMax - dimensions.x,
                    area.Bounds.yMax - dimensions.y,
                    area.Bounds.zMax - dimensions.z
                );

                // Choose random spawn pos in the room's bounds;
                // NOTE: this random position is in room coords
                Vector3Int randomSpawnPos = new Vector3Int
                (
                    UnityEngine.Random.Range(area.Bounds.xMin, adjUpperBound.x + 1),
                    UnityEngine.Random.Range(area.Bounds.yMin, adjUpperBound.y + 1),
                    UnityEngine.Random.Range(area.Bounds.zMin, adjUpperBound.z + 1)
                );

                // Append the newly generated blueprint rooms to the end of the list
                List<BlueprintRoom> newRooms = GenerateBlueprintsFromDimensions(mainPath, randomSpawnPos, dimensions);

                // do not advance iteration if nothing was spawned
                if (newRooms == null)
                    indexOffset = 0;
            }
            return true;
        }

        /// <summary>
        /// Place fixed room within the bounds of an area; returns false if the
        /// room could not be placed correctly
        /// </summary>
        /// <param name="room"></param>
        /// <param name="upperBound"></param>
        /// <param name="lowerBound"></param>
        /// <param name="placementPoint"></param>
        /// <returns></returns>
        private bool PlaceFixedRoomBlueprints(RoomEntry entry, Area area, ZoneConnectionEntry zoneEntry = null)
        {
            if (entry.Prefab.TryGetComponent<Room>(out Room room))      // Prefab in entry does not have a Room Component
            {
                // Adjust parameters to fit the area's actual position
                Vector3Int areaOffset = area.Bounds.position;
                Vector3Int adjustedSpawnPos = entry.SpawnPosition + areaOffset;

                // Check Collision with the area's bounds
                Vector3 difference = CheckOutOfBounds(adjustedSpawnPos, room.RoomDimensions, area.Bounds);

                if (difference != Vector3.zero)     // Room was outside the bounds of the area
                {
                    Debug.LogError($"Map Generator Error: Fixed Room \"{room.name}\" was outside of bounds and could not be placed.\n" +
                        $"It was {difference} units outside the bounds of the area.");
                    return false;
                }

                // TODO: Use hash map instead of List for faster lookup maybe?
                // Check Collision with other rooms
                List<BlueprintRoom> rooms;
                if (zoneEntry == null)
                    rooms = GenerateBlueprintsFromDimensions(area.MainPath, adjustedSpawnPos, room.RoomDimensions, false);      // Fill room space with blueprint rooms
                else
                {
                    rooms = GenerateBlueprintsFromDimensions(zoneEntry.ConnectionPath, adjustedSpawnPos, room.RoomDimensions, false);      // Fill room space with blueprint rooms
                }

                // Set rooms that are supposed to be available to available
                foreach (Vector3Int cell in entry.AvailableCells)
                {
                    Vector3Int cellPosition = adjustedSpawnPos + cell;      // Find the actual position in room space of the cell

                    if (MasterDictionary.TryGetValue(cellPosition, out BlueprintRoom r))
                    {
                        r.Available = true;
                    }
                    else
                    {
                        if (zoneEntry == null)
                            GenerateBlueprintRoom(area.MainPath, cellPosition, true);
                        else
                            GenerateBlueprintRoom(zoneEntry.ConnectionPath, cellPosition, true);
                    }
                }

                if (rooms == null)     // Room was outside the bounds of the area
                {
                    Debug.LogError($"Map Generator Error: Fixed Room \"{room.name}\" was obstructed and could not be placed");
                    return false;
                }

                return true;
            }
            Debug.LogError($"Map Generator Error: {entry.Prefab.name} does not have a Room script!");
            return false;
        }

        private bool PlaceConstrainedRoomBlueprints(RoomEntry entry, Area area)
        {
            if (entry.Prefab.TryGetComponent<Room>(out Room room))      // Prefab in entry does not have a Room Component
            {
                // Adjust the upper bounds so that the room's volume will properly fit within the bounded space
                Vector3Int adjUpperBound = new Vector3Int(
                    entry.Bounds.xMax - room.RoomDimensions.x,
                    entry.Bounds.yMax - room.RoomDimensions.y,
                    entry.Bounds.zMax - room.RoomDimensions.z
                );

                // Choose random spawn pos in the room's bounds;
                // NOTE: this random position is in room coords
                Vector3Int randomSpawnPos = new Vector3Int
                (
                    UnityEngine.Random.Range(entry.Bounds.xMin, adjUpperBound.x + 1),
                    UnityEngine.Random.Range(entry.Bounds.yMin, adjUpperBound.y + 1),
                    UnityEngine.Random.Range(entry.Bounds.zMin, adjUpperBound.z + 1)
                );

                // Fill room space with blueprint rooms
                List<BlueprintRoom> rooms = GenerateBlueprintsFromDimensions(area.MainPath, randomSpawnPos, room.RoomDimensions, false);

                if (rooms == null)     // the room collided with another room
                {
                    Debug.LogWarning($"Map Generator Error: Constrained Room {entry.Prefab.name} collided with another room and could not be placed");
                    return false;
                }

                entry.SpawnPosition = randomSpawnPos;

                // Set rooms that are supposed to be available to available
                foreach (Vector3Int cell in entry.AvailableCells)
                {
                    Vector3Int actualPos = randomSpawnPos + cell;      // Find the actual position in room space of the cell

                    if (MasterDictionary.TryGetValue(actualPos, out BlueprintRoom r))
                    {
                        r.Available = true;
                    }
                    else
                    {
                        GenerateBlueprintRoom(area.MainPath, actualPos, true);
                    }
                }
                return true;
            }
            Debug.LogError($"Map Generator Error: {entry.Prefab.name} does not have a Room script!");
            return false;
        }

        private bool PlaceFreeRoomBlueprints(RoomEntry entry, Area area)
        {
            if (entry.Prefab.TryGetComponent<Room>(out Room room))      // Prefab in entry does not have a Room Component
            {
                // Adjust the upper bounds so that the room's volume will properly fit within the bounded space
                Vector3Int adjUpperBound = new Vector3Int(
                    area.Bounds.xMax - room.RoomDimensions.x,
                    area.Bounds.yMax - room.RoomDimensions.y,
                    area.Bounds.zMax - room.RoomDimensions.z
                );

                // Choose random spawn pos in the room's bounds;
                // NOTE: this random position is in room coords
                Vector3Int randomSpawnPos = new Vector3Int
                (
                    UnityEngine.Random.Range(area.Bounds.xMin, adjUpperBound.x + 1),
                    UnityEngine.Random.Range(area.Bounds.yMin, adjUpperBound.y + 1),
                    UnityEngine.Random.Range(area.Bounds.zMin, adjUpperBound.z + 1)
                );

                // Fill room space with blueprint rooms
                List<BlueprintRoom> rooms = GenerateBlueprintsFromDimensions(area.MainPath, randomSpawnPos, room.RoomDimensions);

                if (rooms == null)     // the room collided with another room
                {
                    Debug.LogWarning($"Map Generator Error: Constrained Room {entry.Prefab.name} collided with another room and could not be placed");
                    return false;
                }

                entry.SpawnPosition = randomSpawnPos;

                // Set rooms that are supposed to be available to available
                foreach (Vector3Int cell in entry.AvailableCells)
                {
                    Vector3Int actualPos = randomSpawnPos + cell;      // Find the actual position in room space of the cell

                    if (MasterDictionary.TryGetValue(actualPos, out BlueprintRoom r))
                    {
                        r.Available = true;
                    }
                    else
                    {
                        GenerateBlueprintRoom(area.MainPath, actualPos, true);
                    }
                }
                return true;
            }
            Debug.LogError($"Map Generator Error: {entry.Prefab.name} does not have a Room script!");
            return false;
        }
        #endregion

        private void GenerateTriangulation(Area area)
        {
            if (area == null || area.MainPath == null)
            {
                Debug.LogError($"Map Generator Error: Error Area {area.Name} in invalid for triangulation.");
                return;
            }

            List<Vertex> vertices = new List<Vertex>();

            foreach(BlueprintRoom room in area.MainPath.BlueprintRooms)
            {
                // if room is not available then forget about triangulating/pathfinding to it
                if (!room.Available)
                    continue;

                vertices.Add(new Vertex<BlueprintRoom>(room.Position, room));
            }

            // TODO: Remove, this is messy code
            // Turn off blueprint room availability for unique rooms 
            foreach (RoomEntry e in area.UniqueRooms)
            {
                foreach (Vector3Int cell in e.AvailableCells)
                {
                    Vector3Int actualPos = e.SpawnPosition + cell;      // Find the actual position in room space of the cell
                    if (MasterDictionary.TryGetValue(actualPos, out BlueprintRoom r))
                        r.Available = false;
                }
            }

            // Perform Delaunay Triangulation
            _triangulation = DelaunayTriangulation3D.Triangulate(vertices);

            // TODO: Remove edges that link to same room

            // Find Minimum Spanning tree with Prim's Algorithm
            _minimumSpanningTree = PrimsAlgorithm.MinimumSpanningTree(_triangulation.Edges, _triangulation.Edges[0].U);
        }

        private void ConnectMainPath(Area area)
        {
            if (area == null || area.MainPath == null)
            {
                Debug.LogError($"Map Generator Error: Error Area {area.Name} in invalid for pathfinding.");
                return;
            }

            SimpleAStar3D aStar = new SimpleAStar3D(area.Bounds, area.Bounds.position);

            foreach (Edge e in _minimumSpanningTree)
            {
                Vector3Int startPos = new Vector3Int((int)e.U.Position.x, (int)e.U.Position.y, (int)e.U.Position.z);
                Vector3Int endPos = new Vector3Int((int)e.V.Position.x, (int)e.V.Position.y, (int)e.V.Position.z);
                
                // Add obstructions
                HashSet<Vector3Int> obstructions = new HashSet<Vector3Int>();
                obstructions.Clear();

                foreach (BlueprintRoom room in area.MainPath.BlueprintRooms)
                {
                    // if the room is the start room or ending room of the edge then don't add to obstructions
                    Vector3Int roomPos = room.Position;
                    if (roomPos == startPos || roomPos == endPos)
                        continue;

                    obstructions.Add(roomPos);
                }

                List<Vector3Int> path = aStar.FindPath(startPos, endPos, obstructions, Heuristic.Manhattan);

                if (path == null)
                {
                    Debug.LogError($"Map Generator Error: Pathfinding failed for edge.");
                    return;
                }

                BlueprintRoom curRoom = null;
                BlueprintRoom prevRoom = null;
                foreach (Vector3Int pos in path)
                {
                    if (pos != startPos && pos != endPos)
                    {
                        curRoom = GenerateBlueprintRoom(area.MainPath, pos);
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
        }
        #endregion

        #region Alt Path Blueprint Generation
        /// <summary>
        /// Choose a random room in a path. If endIndex = -1 => endIndex = path's last room.
        /// </summary>
        /// <param name="path">The path to choose the starting room from</param>
        /// <param name="startIndex">Index to start from</param>
        /// <returns>The Choosen Blueprint Room.</returns>
        private BlueprintRoom ChooseRandomRoomOnPath(Path path, int startIndex = 0, int endIndex = -1)
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
                room = ChooseRandomRoomOnPath(path, startIndex, endIndex);
            }

            if (_debugLogs) Debug.Log($"Map Generator: Random room choosen from {path.Name} at index {randomRoomIndex}");

            return room;
        }

        /// <summary>
        /// Drunkard Walk Algorithm, will walk a specified length and store it into a newly created path. The algorithm
        /// has been modified to handle collisions and create pseudo paths where rooms can potentially spawn later.
        /// </summary>
        /// <param name="path">A path with a length of atleast one.</param>
        /// <param name="startRoom">The starting room for the path. If null will create it's own start room</param>
        private void DrunkardWalk(Path path, BoundsInt bounds, BlueprintRoom startRoom = null)
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

        #region Zone Path Blueprint Generation
        // Generate Zone Connection Paths
        // TODO: make connection entrys into a type of zone of it's own that intersects two zones together
        public bool GenerateZoneConnectionBlueprint(ZoneConnectionEntry entry)
        {
            if (entry.ConnectionPath == null)
            {
                Debug.LogError("Map Generator Error: Connection path was null.");
                return false;
            }

            entry.ConnectionPath.Initialize();

            // ***** Place Room A; Room A becomes a part of the first area
            if (entry.AreaA == null)
            {
                Debug.LogError("Map Generator Error: Area A of zone connection was null.");
                return false;
            }
            if (entry.RoomA == null)
            {
                Debug.LogError("Map Generator Error: Room A of zone connection was null.");
                return false;
            }

            bool hasPlaced = false;
            if (entry.RoomA.PlacementType == RoomPlacementType.Fixed)
                hasPlaced = PlaceFixedRoomBlueprints(entry.RoomA, entry.AreaA);
            //else if (entry.RoomA.PlacementType == RoomPlacementType.Constrained)
            //    hasPlaced = PlaceConstrainedRoomBlueprints(entry.RoomA, entry.areaA);
            //else if (entry.RoomA.PlacementType == RoomPlacementType.Free)
            //    hasPlaced = PlaceFreeRoomBlueprints(entry.RoomA, entry.areaA);
            
            if (!hasPlaced)     // Error placing RoomA
            {
                Debug.LogError("Map Generator Error: Error placing RoomA");
                return false;
            }

            // ***** Place Room B; Room B becomes a part of the second area
            if (entry.AreaB == null)
            {
                Debug.LogError("Map Generator Error: Area B of zone connection was null.");
                return false;
            }
            if (entry.RoomB == null)
            {
                Debug.LogError("Map Generator Error: Room B of zone connection was null.");
                return false;
            }

            hasPlaced = false;
            if (entry.RoomB.PlacementType == RoomPlacementType.Fixed)
                hasPlaced = PlaceFixedRoomBlueprints(entry.RoomB, entry.AreaB);
            //else if (entry.RoomA.PlacementType == RoomPlacementType.Constrained)
            //    hasPlaced = PlaceConstrainedRoomBlueprints(entry.RoomA, entry.areaA);
            //else if (entry.RoomA.PlacementType == RoomPlacementType.Free)
            //    hasPlaced = PlaceFreeRoomBlueprints(entry.RoomA, entry.areaA);

            if (!hasPlaced)     // Error placing RoomB
            {
                Debug.LogError("Map Generator Error: Error placing RoomA");
                return false;
            }

            // ***** Find a path from Room A to Room B
            Vector3Int position = new Vector3Int(
                                (int)(entry.AreaA.Bounds.position.x + entry.AreaB.Bounds.position.x) / 2,
                                (int)(entry.AreaA.Bounds.position.y + entry.AreaB.Bounds.position.y) / 2,
                                (int)(entry.AreaA.Bounds.position.z + entry.AreaB.Bounds.position.z) / 2);
            Vector3Int size = entry.AreaA.Bounds.size + entry.AreaB.Bounds.size;
            BoundsInt combinedBounds = new BoundsInt();
            combinedBounds.position = position;
            combinedBounds.size = size;

            // Adjust parameters to fit the room's actual positions
            Vector3Int areaAOffset = entry.AreaA.Bounds.position;
            Vector3Int areaBOffset = entry.AreaB.Bounds.position;
            Vector3Int startPos = entry.RoomA.SpawnPosition + areaAOffset;
            Vector3Int endPos = entry.RoomB.SpawnPosition + areaBOffset;

            // Obstructions
            HashSet<Vector3Int> obstructions = new HashSet<Vector3Int>();
            obstructions.Clear();

            SimpleAStar3D aStar = new SimpleAStar3D(combinedBounds, combinedBounds.position);
            List<Vector3Int> path = aStar.FindPath(startPos, endPos, obstructions, Heuristic.Manhattan);

            if (path == null)
            {
                Debug.LogError($"Map Generator Error: Pathfinding failed for Zone connection.");
                return false;
            }

            // ***** Generate Blueprint Rooms from path
            BlueprintRoom curRoom = null;
            BlueprintRoom prevRoom = null;
            foreach (Vector3Int pos in path)
            {
                if (pos != startPos && pos != endPos)
                    curRoom = GenerateBlueprintRoom(entry.ConnectionPath, pos);
                else
                    curRoom = MasterDictionary[pos];
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
            return true;
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
        private List<BlueprintRoom> GenerateBlueprintsFromDimensions(Path path, Vector3Int position, Vector3Int roomDimensions, bool available = true)
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

        /* OLD COLLISION CHECKER (UNUSED)
        /// <summary>
        /// Overloaded CheckCollision() function that will check a room with collision with a blueprint room
        /// from the Master Dictionary
        /// </summary>
        /// <param name="room"></param>
        /// <param name="collidedRoom"></param>
        /// <returns></returns>
        private bool CheckCollision(Room room, out BlueprintRoom collidedRoom)
        {
            // TODO: Change this later cause it's inefficient!
            Vector3Int roomPosition = ConvertToRoomCoords(room.gameObject.transform.position);
            collidedRoom = null;

            // TODO: Add padding to condition (x < room.RoomDimensions.x + roomPadding) ?
            for (int x = 0; x < room.RoomDimensions.x; x++)
            {
                for (int y = 0; y < room.RoomDimensions.y; y++)
                {
                    for (int z = 0; z < room.RoomDimensions.z; z++)
                    {
                        Vector3Int currentPos = new Vector3Int(x, y, z) + roomPosition;
                        
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
        */

        /// <summary>
        /// Pass in two rooms and link their entrancways together. 
        /// </summary>
        /// <param name="room1">First blueprint room</param>
        /// <param name="room2">Second blueprint room</param>
        /// <param name="entrFlagIdx">The index of the choosen face of the *first* room.</param>
        private void FlagDoorways(BlueprintRoom room1, BlueprintRoom room2, int entrFlagIdx) // Flag the entranceways to be activated in each room
        {
            if (entrFlagIdx < 0)
            {
                Debug.LogError("Map Generator Error: Two rooms are invalid for entrance connection");
                return;
            }

            // Flag the fact of the next room facing the prev. room
            if (Math.IsEven(entrFlagIdx))                                   // If choosen an even numbered side then set opposite to true (Ex. F4 -> F3 = true)
                room1.entrancewayFlags[entrFlagIdx + 1] = true;
            else                                                            // If choosen an odd numbered side then set opposite to true (Ex. F3 -> F4 = true)
                room1.entrancewayFlags[entrFlagIdx - 1] = true;

            // Flag the face of the prev. room facing the next room
            room2.entrancewayFlags[entrFlagIdx] = true;
        }
        #endregion
        #endregion

        #region RoomGenerationProcedure
        /// <summary>
        /// Second procedure of the Labyrinth Algorithm. Will parse through all of the 
        /// paths and generate rooms based on conditions. These conditions are based on 
        /// room shape chance, room prefab chance, if the room shape will align adiquately to the path, and what path
        /// the room is a part of. It will also activate the entranceways of rooms based on the path's sequence.
        /// </summary>
        public void GenerateAreaRooms(Area area) 
        {
            // Must have an area to generate anything
            if (area == null)
            {
                Debug.LogError($"Map Generator Error: Area Entry Missing for room generation procedure.");
                return;
            }

            // Generate Unique Rooms
            GenerateUniqueRooms(area);

            // Generate Rooms along main path
            GenerateRoomsOnPath(area.MainPath);

            // Generator Rooms along alt. paths
            foreach (Path path in area.Paths)
                GenerateRoomsOnPath(path);
        }

        public void GenerateZoneConnectionRooms(ZoneConnectionEntry entry)
        {
            // ******* Generate Room A ******
            // Adjust parameters to fit the area's actual position
            Vector3Int areaAOffset = entry.AreaA.Bounds.position;
            Vector3Int adjustedSpawnPosA = entry.RoomA.SpawnPosition + areaAOffset;

            Room generatedRoomA = GenerateRoom(entry.RoomA.Prefab, adjustedSpawnPosA, entry.AreaA.MainPath);

            // Unique rooms with available cells
            if (entry.RoomA.AvailableCells != null)
            {
                for (int i = 0; i < entry.RoomA.AvailableCells.Count; i++)
                {
                    if (MasterDictionary.TryGetValue(adjustedSpawnPosA + entry.RoomA.AvailableCells[i], out BlueprintRoom room))
                        generatedRoomA.CopyBlueprintEntranceFlags(room.entrancewayFlags, i, Vector3.zero);
                    else
                        Debug.LogError($"Map Generator Error: Could not copy entranceway flags into unique room");
                }
            }

            generatedRoomA.Initialize();

            // ******* Generate Room B ******
            // Adjust parameters to fit the area's actual position
            Vector3Int areaBOffset = entry.AreaA.Bounds.position;
            Vector3Int adjustedSpawnPosB = entry.RoomA.SpawnPosition + areaBOffset;

            Room generatedRoomB = GenerateRoom(entry.RoomA.Prefab, adjustedSpawnPosB, entry.AreaA.MainPath);

            // Unique rooms with available cells
            if (entry.RoomA.AvailableCells != null)
            {
                for (int i = 0; i < entry.RoomA.AvailableCells.Count; i++)
                {
                    if (MasterDictionary.TryGetValue(adjustedSpawnPosB + entry.RoomA.AvailableCells[i], out BlueprintRoom room))
                        generatedRoomB.CopyBlueprintEntranceFlags(room.entrancewayFlags, i, Vector3.zero);
                    else
                        Debug.LogError($"Map Generator Error: Could not copy entranceway flags into unique room");
                }
            }

            generatedRoomB.Initialize();

            // ******* Spawn Rooms On Connection Path ******
            GenerateRoomsOnPath(entry.ConnectionPath);
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

        private void GenerateUniqueRooms(Area area)
        {
            foreach (RoomEntry entry in area.UniqueRooms)
            {
                // Adjust parameters to fit the area's actual position
                Vector3Int areaOffset = area.Bounds.position;
                Vector3Int adjustedSpawnPos = entry.SpawnPosition + areaOffset;

                Room generatedRoom = GenerateRoom(entry.Prefab, adjustedSpawnPos, area.MainPath);

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

        /// <summary>
        /// Spawn a room given a position and direction; room type not passed as room is expected to
        /// already know it's type if unique (FOR NOW BUT MAYBE NOT LATER)
        /// For random room placement algorithm to use.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="placementPosition"></param>
        /// <param name="rDir"></param>
        /// <returns></returns>
        private Room GenerateRoom(GameObject prefab, Vector3Int placementPosition, Path path, RoomDirection rDir = 0)
        {
            Quaternion rotation = Quaternion.identity;      // TODO: set rotation
            Room generatedRoom = Instantiate(prefab, ConvertToWorldCoords(placementPosition), rotation, _roomContainer).GetComponent<Room>();

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
        private Room GenerateRoom(RoomShape shape, RoomType rType, Path path, BlueprintRoom originRoom, RoomDirection rDir = 0, int prefabIndex = -1)      // prefabIndex = -1 means spawn random room
        {
            Room generatedRoom = null;
            Quaternion rotation = Quaternion.identity;      // Take the rotation of the room into account
            Vector3 eulerRotation = Vector3.zero;

            // If starting room then spawn starting room and return
            if (rType == RoomType.start)
            {
                // Generate Small Room; no direction condition needed
                generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.startingRooms), ConvertToWorldCoords(originRoom.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
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

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), ConvertToWorldCoords(originRoom.Position), rotation, _roomContainer).GetComponent<Room>();
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
                        
                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), ConvertToWorldCoords(leftRoom.Position), rotation, _roomContainer).GetComponent<Room>();
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

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), ConvertToWorldCoords(leftRoom.Position), rotation, _roomContainer).GetComponent<Room>();
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

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x2), ConvertToWorldCoords(backRoom.Position), rotation, _roomContainer).GetComponent<Room>();
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

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms1x2x1), ConvertToWorldCoords(originRoom.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    }
                    else if (rDir == RoomDirection.NegY)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + Vector3Int.down];

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms1x2x1), ConvertToWorldCoords(nextRoom.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x2x1-Room at position of indexed blueprint room; use a random room in the 1x2x1-Room list
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

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), ConvertToWorldCoords(originRoom.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    }
                    else if (rDir == RoomDirection.NegX)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + Vector3Int.left];

                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), ConvertToWorldCoords(nextRoom.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 2x1x1-Room at position of indexed blueprint room; use a random room in the 2x1x1-Room list
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 1, eulerRotation);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 0, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);                                                                // Activate new rooms entranceways
                    }
                    else if (rDir == RoomDirection.PosZ)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + Vector3Int.forward];

                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        eulerRotation = new Vector3(0, 90, 0);
                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), ConvertToWorldCoords(originRoom.Position), rotation, _roomContainer).GetComponent<Room>();
                        generatedRoom.CopyBlueprintEntranceFlags(originRoom.entrancewayFlags, 0, eulerRotation);        // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (first 6 elements : 0 - 5)
                        generatedRoom.CopyBlueprintEntranceFlags(nextRoom.entrancewayFlags, 1, eulerRotation);          // Copy array of blueprint's entrencewayFlags to the newly generated room's entrancewayFlags array (next 6 elements : 6 - 11)
                        generatedRoom.Initialize(rType);
                    }
                    else if (rDir == RoomDirection.NegZ)
                    {
                        BlueprintRoom nextRoom = MasterDictionary[originRoom.Position + Vector3Int.back];

                        rotation.SetFromToRotation(Vector3.right, Vector3.forward);
                        eulerRotation = new Vector3(0, 90, 0);
                        generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms2x1x1), ConvertToWorldCoords(nextRoom.Position), rotation, _roomContainer).GetComponent<Room>();
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
                    generatedRoom = Instantiate(ChooseRandomRoomFromWeights(path.rooms1x1x1), ConvertToWorldCoords(originRoom.Position), rotation, _roomContainer).GetComponent<Room>(); // Instantiate 1x1x1-Room at position of indexed blueprint room; use a random room in the 1x1x1-Room list
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

        // Vector based conversion from room -> world coords
        private float ConvertToWorldCoords(int roomCoords)
        {
            return roomCoords * _gridUnitSize;
        }

        // Vector based conversion from world -> room coords; can be expensive!
        private Vector3Int ConvertToRoomCoords(Vector3 worldCoord)
        {
            int xComp = (int)(worldCoord.x / _gridUnitSize);
            int yComp = (int)(worldCoord.y / _gridUnitSize);
            int zComp = (int)(worldCoord.z / _gridUnitSize);
            return new Vector3Int(xComp, yComp, zComp);
        }

        // Vector based conversion from world -> room coords; can be expensive!
        private int ConvertToRoomCoords(float worldCoord)
        {
            return (int)(worldCoord / _gridUnitSize);
        }

        /// <summary>
        /// Check if a point lies outside the bounds of the area.
        /// *** The point must be in world coords ***
        /// </summary>
        /// <param name="desiredPos">The desired position to spawn the next room</param>
        /// <returns>Returns true if the space is out of bounds and false otherwise.</returns>
        private bool CheckOutOfBounds(Vector3Int desiredPos, Vector3Int upperBound, Vector3Int lowerBound)
        {
            Vector3Int differenceUpper = upperBound - desiredPos;
            Vector3Int differenceLower = lowerBound - desiredPos;
            if (differenceUpper.x <= 0 || differenceUpper.y <= 0 || differenceUpper.z <= 0)        // Valid space
                return false;
            if (differenceLower.x > 0 || differenceLower.y > 0 || differenceLower.z > 0)        // Valid space
                return false;

            return true;           // Invalid space
        }

        private bool CheckOutOfBounds(Vector3Int desiredPos, BoundsInt bounds)
        {
            if (bounds.Contains(desiredPos))        // Valid space
                return false;

            return true;           // Invalid space
        }

        /* OLD BOUNDS CHECK (DEPRICATED)
        /// <summary>
        /// Checks if a specified volume with a starting point overlaps the bounds of an area;
        /// Returns the offset of the area of the room outside the bounds.
        /// If the value returned is zero then there is no overlap
        /// *** The origin, bounds, and dimensions must be in room coords ***
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="roomDimensions"></param>
        /// <param name="upperBound"></param>
        /// <param name="lowerBound"></param>
        /// <returns>Room offset from bounds in room coordinates</returns>
        private Vector3Int CheckOutOfBounds(Vector3Int origin, Vector3Int roomDimensions, Vector3Int upperBound, Vector3Int lowerBound)
        {
            Vector3Int lowerPoint = origin;
            Vector3Int upperPoint = origin + (roomDimensions - Vector3Int.one);

            Vector3Int lowerDiff = lowerBound - lowerPoint;
            Vector3Int upperDiff = upperBound - upperPoint;

            Debug.Log(lowerDiff);
            Debug.Log(upperDiff);

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
        */

        private Vector3Int CheckOutOfBounds(Vector3Int origin, Vector3Int roomDimensions, BoundsInt bounds)
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
        private bool CheckCollision(Vector3Int position, out BlueprintRoom collidedRoom)
        {
            return MasterDictionary.TryGetValue(position, out collidedRoom);
        }

        /// <summary>
        /// Checks if the total amount of rooms is valid in an area's bounded range.
        /// </summary>
        /// <returns>The test success or fail</returns>
        private bool CheckAreaBoundedVolume(Area area)
        {
            float totalCellOccupancy = 0;

            foreach (RoomEntry entry in area.UniqueRooms)                    // Add Unique Room volume
            {
                if (entry.Prefab.TryGetComponent<Room>(out Room room))
                {
                    totalCellOccupancy += room.GetRoomOccupancy();
                }
                else
                    Debug.LogWarning("Map Generator Warning: Room Entry Prefab has no Room Script");
            }

            // TODO: Add Divergent Room volume ?

            totalCellOccupancy += area.MainPath.PathLength;                 // Add Main Path volume

            foreach (Path path in area.Paths)                               // Add Alt. Paths volume
                totalCellOccupancy += path.PathLength;

            // Calculate the bounded volume and check if amount of room cells taken up exceeds that amount
            float xSize = area.Bounds.size.x;
            float ySize = area.Bounds.size.y;
            float zSize = area.Bounds.size.z;
            float volume = Math.RectangularVolume(xSize, ySize, zSize);

            if (volume < totalCellOccupancy)        // The bounded volume cannot fullfill the area's cell requirements
                return false;

            return true;        // The area's cell requirements are met with the bounded volume
        }

        /* CHECK THE BOUNDING BOX OF A ROOM WITH DIMENSIONS (UNUSED)
        /// <summary>
        /// Checks if the volume of the object can fit in the space of a bounded area
        /// </summary>
        /// <param name="cellOccupancy">The amound of cells an object takes up</param>
        /// <param name="lowerBound">The lower bound of the bounding box</param>
        /// <param name="upperBound">The upper bound of the bounding box</param>
        /// <returns></returns>
        private bool CheckBoundedVolume(float cellOccupancy, Vector3 upperBound, Vector3 lowerBound)
        {
            // Calculate the bounded volume and check if amount of room cells taken up exceeds that amount
            float xSize = upperBound.x - lowerBound.x;
            float ySize = upperBound.y - lowerBound.y;
            float zSize = upperBound.z - lowerBound.z;
            float volume = Math.RectangularVolume(xSize, ySize, zSize);

            if (volume < cellOccupancy)     // The bounded volume CANNOT fullfill the amount of required cells
                return false;

            return true;        // The bounded volume CAN fullfill the amount of required cells
        }
        */
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

        /* CLEAR PATHS (UNUSED)
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
                _areas[0].MainPath.Initialize();

                if (_areas[0] == null)
                {
                    Debug.LogError("Map Generator Error: Area Entry Missing.");
                    _debugState = DebugState.Failed;
                    return;
                }

                // Take the volume of the bounding cubic space and return an error if the amount of rooms to spawn is larger than that volume; make sure we have space for needed rooms
                if (!CheckAreaBoundedVolume(_areas[0]))
                {
                    Debug.LogError($"Map Generator Error: The amount of blueprint rooms for area {_areas[0].Name} exceeds the bounding box's volume or the bounding box is inverted.");
                    _debugState = DebugState.Failed;
                    return;
                }

                _debugState = DebugState.GenUniqueRooms;
            }

            if (_debugState == DebugState.GenUniqueRooms)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Critical Rooms"))       // Generates Unique Rooms
                {
                    // Generate Unique Rooms
                    PlaceUniqueRooms(_areas[0]);
                    _debugState = DebugState.GenDivergentRooms;
                }
            }

            if (_debugState == DebugState.GenDivergentRooms)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Divergent Rooms"))        // Generates Divergent Rooms
                {
                    // Generate Divergent Rooms
                    PlaceDivergentRooms(_areas[0]);
                    _debugState = DebugState.GenTriangulation;
                }
            }

            if (_debugState == DebugState.GenTriangulation)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Triangulation"))        // Generates triangulation of main path
                {
                    GenerateTriangulation(_areas[0]);
                    _debugState = DebugState.GenMainPath;
                }
            }

            if (_debugState == DebugState.GenMainPath)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Main Path"))        // Generates main path
                {
                    ConnectMainPath(_areas[0]);
                    _debugState = DebugState.GenPaths;
                }
            }

            if (_debugState == DebugState.GenPaths)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Alt Blueprint Paths"))        // Generates alt paths that diverge from the main path
                {
                    GenerateAltPathBlueprints(_areas[0]);
                    _debugState = DebugState.GenRooms;
                }
            }

            if (_debugState == DebugState.GenRooms)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Rooms From Paths"))        // Generates alt paths that diverge from the main path
                {
                    GenerateUniqueRooms(_areas[0]);
                    GenerateAreaRooms(_areas[0]);
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
                    if (_debugState == DebugState.GenUniqueRooms)
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
                        GenerateMainPathBlueprint(_areas[0]);
                        _debugState = DebugState.GenPaths;
                    }
                    if (_debugState == DebugState.GenPaths)
                    {
                        GenerateAltPathBlueprints(_areas[0]);
                        _debugState = DebugState.GenRooms;
                    }
                    if (_debugState == DebugState.GenRooms)
                    {
                        GenerateAreaRooms(_areas[0]);
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

            foreach (Area area in _areas)
            {
                DrawBoundingBox(area.Bounds);
                DrawTriangulation();
                DrawBluePrintGizmos(area);
            }

            foreach (ZoneConnectionEntry entry in _zoneConnections)
            {
                DrawBluePrintGizmos(entry.ConnectionPath);
            }
        }

        /* OLD DRAWING OF BOUNDING BOX (DEPRICATED)
        /// <summary>
        /// Draw the bounding box of the generator
        /// </summary>
        private void DrawBoundingBox()
        {
            // Find the centerpoint of the box
            float xPos = ConvertToWorldCoords(_currentLowerBound.x + _currentUpperBound.x) / 2;
            float yPos = ConvertToWorldCoords(_currentLowerBound.y + _currentUpperBound.y) / 2;
            float zPos = ConvertToWorldCoords(_currentLowerBound.z + _currentUpperBound.z) / 2;
            Vector3 centerPoint = new Vector3(xPos, yPos, zPos);

            // Find the size of the box
            float xSize = ConvertToWorldCoords(_currentUpperBound.x - _currentLowerBound.x);
            float ySize = ConvertToWorldCoords(_currentUpperBound.y - _currentLowerBound.y);
            float zSize = ConvertToWorldCoords(_currentUpperBound.z - _currentLowerBound.z);
            Vector3 size = new Vector3(xSize, ySize, zSize);


            Gizmos.color = _boundingBoxColor;
            Gizmos.DrawWireCube(centerPoint, size);
        }
        */
        private void DrawBoundingBox(BoundsInt bounds)
        {
            Vector3 worldSize = ConvertToWorldCoords(bounds.size + Vector3Int.one);
            Vector3 worldCenter = bounds.center * _gridUnitSize;

            Gizmos.color = _boundingBoxColor;
            Gizmos.DrawWireCube(worldCenter, worldSize);
        }

        private void DrawTriangulation()
        {
            if (_triangulation == null) 
                return;

            // Draw circumcircles in remaining tetrahedron from triangulation
            foreach (Tetrahedron t in _triangulation.Tetrahedra)
            {
                Gizmos.color = _circumcircleColor;
                Gizmos.DrawSphere(t.Circumcenter * _gridUnitSize, Mathf.Sqrt(t.CircumradiusSquared) * _gridUnitSize);
            }

            // Draw remaining edges from triangulation
            foreach (Edge e in _triangulation.Edges)
            {
                Gizmos.color = _triangulationColor;
                Gizmos.DrawLine(e.V.Position * _gridUnitSize, e.U.Position * _gridUnitSize);
            }

            // Draw the minimum spanning tree of the area
            foreach (Edge e in _minimumSpanningTree)
            {
                Gizmos.color = _minimumSpanningTreeColor;
                Gizmos.DrawLine(e.V.Position * _gridUnitSize, e.U.Position * _gridUnitSize);
            }
        }

        private void DrawBluePrintGizmos(Area area)
        {
            if (area.MainPath.BlueprintRooms == null)
                return;

            Vector3 unitSize = Vector3.one * _gridUnitSize;

            // Draw Gizmos for main path
            foreach (BlueprintRoom bRoom in area.MainPath.BlueprintRooms)
            {
                Gizmos.color = area.MainPath.PathGizmoColor;
                Gizmos.DrawCube(ConvertToWorldCoords(bRoom.Position), unitSize);
            }

            foreach (Path path in area.Paths)
            {
                if (path.BlueprintRooms == null)
                    return;

                // Draw Gizmos for alt paths
                foreach (BlueprintRoom bRoom in path.BlueprintRooms)
                {
                    Gizmos.color = path.PathGizmoColor;
                    Gizmos.DrawCube(ConvertToWorldCoords(bRoom.Position), unitSize);
                }
            }
        }

        private void DrawBluePrintGizmos(Path path)
        {
            if (path.BlueprintRooms == null)
                return;

            Vector3 unitSize = Vector3.one * _gridUnitSize;

            // Draw Gizmos for main path
            foreach (BlueprintRoom bRoom in path.BlueprintRooms)
            {
                Gizmos.color = path.PathGizmoColor;
                Gizmos.DrawCube(ConvertToWorldCoords(bRoom.Position), unitSize);
            }
        }
        #endregion
    }
}