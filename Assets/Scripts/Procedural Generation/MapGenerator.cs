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
        [field: SerializeField] public Zone ZoneA { get; set; }
        [field: SerializeField] public Zone ZoneB { get; set; }

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
        const int STANDARD_ROOM_FACE_COUNT = 6;
        const string MASTER_PATH_NAME = "Master Path";

        // ***** Singleton Reference *****
        public static MapGenerator Instance { get; private set; }

        // ***** Events *****
        public static event Action OnGenerationDone;
        public static event Action OnGenerationStarted;

        // ***** Path Containers *****
        // The Master Path holds a reference to all bluprint rooms in an zone
        public Path MasterPath { get; private set; }

        // Dictionary used for quick access like checking locations for conflicts and checking locations for room shape conditions
        // Keys are in room coords
        public Dictionary<Vector3Int, BlueprintRoom> MasterDictionary { get; private set; }
        
        // ***** Inspector Values *****
        // Enable the map generator
        [Tooltip("Enables map generation.")]
        [SerializeField] private bool _enabled = true;

        [Header("Seed")]
        [SerializeField] private int customSeed = 0;
        [SerializeField] private bool generateRandomSeed = true;

        [Header("Global Settings")]
        [Tooltip("The size of a room unit or how large a 1x1 room is in Unity units.")]
        [SerializeField] private int _gridUnitSize = 13;                      // The unit size of the room grid's cell
        [SerializeField] private Transform _roomContainer;                      // Parent transform that will contain all the spawned rooms
        [SerializeField] private int _numOfPlacementAttempsBeforeRegen = -1;    // If this number is exceeded then the generator will refresh its entire generation attempt

        [Header("Zones")]
        [SerializeField] private List<Zone> _zones;

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
        // TODO: do not make this global in this class, maybe in the Zone class?
        private int _seed;      // TODO: For networking make the host generate this

        private BlueprintGenerator _blueprintGenerator;
        private RoomGenerator _roomGenerator;

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
                Debug.Log("Map Generator: Debug On");
                _debugState = DebugState.Start;        // Jump to next step
                return;
            }
            Debug.Log("Map Generator: Debug Off");
            
            try
            {
                if (generateRandomSeed)
                {
                    // Generate Random Seed
                    _seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                }
                else
                    _seed = customSeed;

                UnityEngine.Random.InitState(_seed);

                if (_debugLogs)
                    Debug.Log($"Generating map with seed: {_seed}");

                // Initialize Master Data Structures
                InitializeMasters();

                // Initialize Blueprint Generator
                _blueprintGenerator = new BlueprintGenerator(MasterPath, MasterDictionary);

                // Initialize Room Generator
                _roomGenerator = new RoomGenerator(MasterPath, MasterDictionary, _gridUnitSize, _roomContainer);

                // Initialize Zone Data Structures
                foreach (Zone zone in _zones)
                    InitializeZone(zone);

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

            // Generate blueprint map for each zone
            foreach (Zone zone in _zones)
            {
                GenerateZoneBlueprints(zone);
            }

            // ******* Generate Rooms *******
            // Generate Zone Connection Rooms
            foreach (ZoneConnectionEntry entry in _zoneConnections)
            {
                // Generate actual rooms for the zone connection
                GenerateZoneConnectionRooms(entry);
            }

            // Spawn rooms based on the blueprint map for each zone
            foreach (Zone zone in _zones)
            {

                // Check room conditions and generate rooms using the blueprint map of the zone
                GenerateZoneRooms(zone);

                // TODO: Implement perlin noise height and type Map

                // Generate random loot when the room generation is complete through subscribing to this event
                OnGenerationDone?.Invoke();

                // TODO: Clean Up
                // ClearAllPaths();
            }
        }

        public void InitializeMasters()     // NOTE: This must be done before generating anything!
        {
            // Initialize Master Data Structures
            MasterDictionary = new Dictionary<Vector3Int, BlueprintRoom>();
            MasterPath = ScriptableObject.CreateInstance<Path>();
            MasterPath.Initialize();
            MasterPath.Name = MASTER_PATH_NAME;
        }

        private void InitializeZone(Zone zone)
        {
            zone.MainPath.Initialize();
        }
        #endregion

        #region Blueprint Procedure
        /// <summary>
        /// First procedure in the Labyrinth Algorithm that will make pseudo paths in different directions.
        /// These paths are basically just lists of positions on the room grid and will be used to generate
        /// the actual rooms later. It is called blueprint because it is a pre-map layout before placing the
        /// actual rooms.
        /// </summary>
        public void GenerateZoneBlueprints(Zone zone)
        {
            // Must have a zone to generate anything
            if (zone == null)
            {
                Debug.LogError("Map Generator Error: Zone Entry Missing for blueprint procedure.");
                return;
            }

            // Take the volume of the bounding cubic space and return an error if the amount of rooms to spawn is larger than that volume; make sure we have space for needed rooms
            if (!CheckZoneBoundedVolume(zone))
            {
                Debug.LogError($"Map Generator Error: The amount of blueprint rooms for zone {zone.Name} exceeds the bounding box's volume or the bounding box is inverted.");
                return;
            }

            // ******* Generate Zone Blueprints *******
            // Generate Main Path to boss
            GenerateMainPathBlueprint(zone);

            // Ganerate Alternative paths
            GenerateAltPathBlueprints(zone);            
        }

        /// <summary>
        /// Wrapper function for generating the main path.
        /// The main path is the path to the zone boss and to traversal rooms to other zones
        /// </summary>
        public void GenerateMainPathBlueprint(Zone zone)
        {
            _debugState = DebugState.GenMainPath;

            if (zone.MainPath == null)      // Throw error if MainPath for zone does not exist
            {
                Debug.LogError($"Map Generator Error: The Main Path for zone {zone.name} is not assigned.");
                return;
            }

            // Unique Room Placement
            _blueprintGenerator.PlaceUniqueRooms(zone);

            // Divergent Room Placement
            _blueprintGenerator.PlaceDivergentRooms(zone);

            // Generate Delauney Triangulation
            List<Edge> MST = GenerateContigiousTriangulation(zone);

            // Pathfind and Connect Main Path
            ConnectMainPath(zone, MST);

            if (_debugLogs) Debug.Log($"Map Generator: {zone.Name} generated path {zone.MainPath.name} with {zone.MainPath.BlueprintCount()} rooms.");
        }

        /// <summary>
        /// Wrapper function for generating the prize path
        /// </summary>
        public void GenerateAltPathBlueprints(Zone zone)
        {
            if (zone.MainPath == null)      // Throw error if MainPath for zone does not exist
            {
                Debug.LogError($"Map Generator Error: The Main Path for zone {zone.name} is not assigned.");
                return;
            }

            // Path to prize room; choose a random start room
            // Initialize a new path at starting room if not null
            int startIndex = zone.MainPath.BlueprintCount() - 1;              // Start index in master path
            int endIndex = startIndex + zone.MainPath.PathLength;

            foreach (Path path in zone.Paths)
            {
                if (path == null)
                {
                    Debug.LogError($"Map Generator Error: A path {path.Name} for zone {zone.name} is not assigned.");
                    return;
                }

                BlueprintRoom startRoom = _blueprintGenerator.ChooseRandomRoomInPath(zone.MainPath, 1); // start at index 1 as to not choose the starting room of the game
                path.Initialize(startIndex, endIndex);

                _blueprintGenerator.BlueprintDrunkardWalk(path, zone.Bounds, startRoom);

                if (_debugLogs) Debug.Log($"Map Generator: {path.name} generated with {path.BlueprintCount()} rooms.");
            }
        }        

        private List<Edge> GenerateContigiousTriangulation(Zone zone)
        {
            if (zone == null || zone.MainPath == null)
            {
                Debug.LogError($"Map Generator Error: Error Zone {zone.Name} in invalid for triangulation.");
                return null;
            }

            DelaunayTriangulation3D triangulation = _blueprintGenerator.GenerateTriangulationFromPath(zone.MainPath);

            List<Edge> MST = _blueprintGenerator.FindMinimumSpanningTree(triangulation.Edges, triangulation.Edges[0].U);

            return MST;
        }

        private void ConnectMainPath(Zone zone, List<Edge> edges)
        {
            if (zone == null || zone.MainPath == null)
            {
                Debug.LogError($"Map Generator Error: Error Zone {zone.Name} in invalid for pathfinding.");
                return;
            }

            foreach (Edge e in edges)
            {
                Vector3Int startPos = new Vector3Int((int)e.U.Position.x, (int)e.U.Position.y, (int)e.U.Position.z);
                Vector3Int endPos = new Vector3Int((int)e.V.Position.x, (int)e.V.Position.y, (int)e.V.Position.z);

                _blueprintGenerator.PathfindBlueprint(zone.MainPath, zone.Bounds, startPos, endPos);
            }
        }

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

            // ***** Place Room A; Room A becomes a part of the first zone
            if (entry.ZoneA == null)
            {
                Debug.LogError("Map Generator Error: Zone A of zone connection was null.");
                return false;
            }
            if (entry.RoomA == null)
            {
                Debug.LogError("Map Generator Error: Room A of zone connection was null.");
                return false;
            }

            bool hasPlaced = false;
            if (entry.RoomA.PlacementType == RoomPlacementType.Fixed)
                hasPlaced = _blueprintGenerator.PlaceFixedUniqueRoomBlueprints(entry.RoomA, entry.ZoneA.MainPath, entry.ZoneA.Bounds);
            //else if (entry.RoomA.PlacementType == RoomPlacementType.Constrained)
            //    hasPlaced = PlaceConstrainedRoomBlueprints(entry.RoomA, entry.zoneA);
            //else if (entry.RoomA.PlacementType == RoomPlacementType.Free)
            //    hasPlaced = PlaceFreeRoomBlueprints(entry.RoomA, entry.zoneA);
            
            if (!hasPlaced)     // Error placing RoomA
            {
                Debug.LogError("Map Generator Error: Error placing RoomA");
                return false;
            }

            // ***** Place Room B; Room B becomes a part of the second zone
            if (entry.ZoneB == null)
            {
                Debug.LogError("Map Generator Error: Zone B of zone connection was null.");
                return false;
            }
            if (entry.RoomB == null)
            {
                Debug.LogError("Map Generator Error: Room B of zone connection was null.");
                return false;
            }

            hasPlaced = false;
            if (entry.RoomB.PlacementType == RoomPlacementType.Fixed)
                hasPlaced = _blueprintGenerator.PlaceFixedUniqueRoomBlueprints(entry.RoomB, entry.ZoneB.MainPath, entry.ZoneB.Bounds);
            //else if (entry.RoomA.PlacementType == RoomPlacementType.Constrained)
            //    hasPlaced = PlaceConstrainedRoomBlueprints(entry.RoomA, entry.zoneA);
            //else if (entry.RoomA.PlacementType == RoomPlacementType.Free)
            //    hasPlaced = PlaceFreeRoomBlueprints(entry.RoomA, entry.zoneA);

            if (!hasPlaced)     // Error placing RoomB
            {
                Debug.LogError("Map Generator Error: Error placing RoomA");
                return false;
            }

            // ***** Find a path from Room A to Room B
            Vector3Int position = new Vector3Int(
                                (int)(entry.ZoneA.Bounds.position.x + entry.ZoneB.Bounds.position.x) / 2,
                                (int)(entry.ZoneA.Bounds.position.y + entry.ZoneB.Bounds.position.y) / 2,
                                (int)(entry.ZoneA.Bounds.position.z + entry.ZoneB.Bounds.position.z) / 2);
            Vector3Int size = entry.ZoneA.Bounds.size + entry.ZoneB.Bounds.size;
            BoundsInt combinedBounds = new BoundsInt();
            combinedBounds.position = position;
            combinedBounds.size = size;

            // Adjust parameters to fit the room's actual positions
            Vector3Int zoneAOffset = entry.ZoneA.Bounds.position;
            Vector3Int zoneBOffset = entry.ZoneB.Bounds.position;
            Vector3Int startPos = entry.RoomA.SpawnPosition + zoneAOffset;
            Vector3Int endPos = entry.RoomB.SpawnPosition + zoneBOffset;

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
                    curRoom = _blueprintGenerator.GenerateBlueprintRoom(entry.ConnectionPath, pos);
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

                _blueprintGenerator.FlagDoorways(curRoom, prevRoom, entrFlagIdx);

                prevRoom = curRoom;
            }
            return true;
        }
        #endregion

        #region RoomGenerationProcedure
        /// <summary>
        /// Second procedure of the Labyrinth Algorithm. Will parse through all of the 
        /// paths and generate rooms based on conditions. These conditions are based on 
        /// room shape chance, room prefab chance, if the room shape will align adiquately to the path, and what path
        /// the room is a part of. It will also activate the entranceways of rooms based on the path's sequence.
        /// </summary>
        public void GenerateZoneRooms(Zone zone)
        {
            // Must have an zone to generate anything
            if (zone == null)
            {
                Debug.LogError($"Map Generator Error: Zone Entry Missing for room generation procedure.");
                return;
            }

            // Generate Unique Rooms
            _roomGenerator.GenerateUniqueRooms(zone);

            // Generate Rooms along main path
            _roomGenerator.GenerateRoomsOnPath(zone.MainPath);

            // Generator Rooms along alt. paths
            foreach (Path path in zone.Paths)
                _roomGenerator.GenerateRoomsOnPath(path);
        }

        public void GenerateZoneConnectionRooms(ZoneConnectionEntry entry)
        {
            // ******* Generate Room A ******
            // Adjust parameters to fit the zone's actual position
            Vector3Int zoneAOffset = entry.ZoneA.Bounds.position;
            Vector3Int adjustedSpawnPosA = entry.RoomA.SpawnPosition + zoneAOffset;

            Room generatedRoomA = _roomGenerator.GenerateRoom(entry.RoomA.Prefab, adjustedSpawnPosA, entry.ZoneA.MainPath);

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
            // Adjust parameters to fit the zone's actual position
            Vector3Int zoneBOffset = entry.ZoneA.Bounds.position;
            Vector3Int adjustedSpawnPosB = entry.RoomA.SpawnPosition + zoneBOffset;

            Room generatedRoomB = _roomGenerator.GenerateRoom(entry.RoomA.Prefab, adjustedSpawnPosB, entry.ZoneA.MainPath);

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
            _roomGenerator.GenerateRoomsOnPath(entry.ConnectionPath);
        }
        #endregion

        #region Utility
        /// <summary>
        /// Checks if the total amount of rooms is valid in an zone's bounded range.
        /// </summary>
        /// <returns>The test success or fail</returns>
        private bool CheckZoneBoundedVolume(Zone zone)
        {
            float totalCellOccupancy = 0;

            foreach (RoomEntry entry in zone.UniqueRooms)                    // Add Unique Room volume
            {
                if (entry.Prefab.TryGetComponent<Room>(out Room room))
                {
                    totalCellOccupancy += room.GetRoomOccupancy();
                }
                else
                    Debug.LogWarning("Map Generator Warning: Room Entry Prefab has no Room Script");
            }

            // TODO: Add Divergent Room volume ?

            totalCellOccupancy += zone.MainPath.PathLength;                 // Add Main Path volume

            foreach (Path path in zone.Paths)                               // Add Alt. Paths volume
                totalCellOccupancy += path.PathLength;

            // Calculate the bounded volume and check if amount of room cells taken up exceeds that amount
            float xSize = zone.Bounds.size.x;
            float ySize = zone.Bounds.size.y;
            float zSize = zone.Bounds.size.z;
            float volume = Math.RectangularVolume(xSize, ySize, zSize);

            if (volume < totalCellOccupancy)        // The bounded volume cannot fullfill the zone's cell requirements
                return false;

            return true;        // The zone's cell requirements are met with the bounded volume
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
        /// 
        List<Edge> _debugMST = null;
        private void DebugProcedure()
        {
            if (_debugState == DebugState.Failed)
                return;

            if (_debugState == DebugState.Start)
                _debugState = DebugState.Initialize;

            if (_debugState == DebugState.Initialize)
            {
                // Initialize Master Data Structures
                if (generateRandomSeed)
                {
                    // Generate Random Seed
                    _seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
                }
                else
                    _seed = customSeed;

                UnityEngine.Random.InitState(_seed);

                if (_debugLogs)
                    Debug.Log($"Generating map with seed: {_seed}");

                // Initialize Master Data Structures
                InitializeMasters();

                // Initialize Blueprint Generator
                _blueprintGenerator = new BlueprintGenerator(MasterPath, MasterDictionary);

                // Initialize Room Generator
                _roomGenerator = new RoomGenerator(MasterPath, MasterDictionary, _gridUnitSize, _roomContainer);

                // Initialize Zone Data Structures
                foreach (Zone zone in _zones)
                    InitializeZone(zone);

                if (_zones[0] == null)
                {
                    Debug.LogError("Map Generator Error: Zone Entry Missing.");
                    _debugState = DebugState.Failed;
                    return;
                }

                // Take the volume of the bounding cubic space and return an error if the amount of rooms to spawn is larger than that volume; make sure we have space for needed rooms
                if (!CheckZoneBoundedVolume(_zones[0]))
                {
                    Debug.LogError($"Map Generator Error: The amount of blueprint rooms for zone {_zones[0].Name} exceeds the bounding box's volume or the bounding box is inverted.");
                    _debugState = DebugState.Failed;
                    return;
                }

                _debugState = DebugState.GenUniqueRooms;
            }

            if (_debugState == DebugState.GenUniqueRooms)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Unique Rooms"))       // Generates Unique Rooms
                {
                    // Generate Unique Rooms
                    _blueprintGenerator.PlaceUniqueRooms(_zones[0]);
                    _debugState = DebugState.GenDivergentRooms;
                }
            }

            if (_debugState == DebugState.GenDivergentRooms)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Divergent Rooms"))        // Generates Divergent Rooms
                {
                    // Generate Divergent Rooms
                    _blueprintGenerator.PlaceDivergentRooms(_zones[0]);
                    _debugState = DebugState.GenTriangulation;
                }
            }

            if (_debugState == DebugState.GenTriangulation)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Triangulation"))        // Generates triangulation of main path
                {
                    _debugMST = GenerateContigiousTriangulation(_zones[0]);
                    _debugState = DebugState.GenMainPath;
                }
            }

            if (_debugState == DebugState.GenMainPath)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Main Path"))        // Generates main path
                {
                    if (_debugMST != null)
                    {
                        ConnectMainPath(_zones[0], _debugMST);
                        _debugState = DebugState.GenPaths;
                    }
                    else
                    {
                        Debug.LogError("Map Generator Error: MST was null.");
                        _debugState = DebugState.Failed;
                    }
                }
            }

            if (_debugState == DebugState.GenPaths)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Alt Blueprint Paths"))        // Generates alt paths that diverge from the main path
                {
                    GenerateAltPathBlueprints(_zones[0]);
                    _debugState = DebugState.GenRooms;
                }
            }

            if (_debugState == DebugState.GenRooms)
            {
                if (GUI.Button(new Rect(10, 10, 200, 30), "Generate Rooms From Paths"))        // Generates alt paths that diverge from the main path
                {
                    GenerateZoneRooms(_zones[0]);
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
                        GenerateMainPathBlueprint(_zones[0]);
                        _debugState = DebugState.GenPaths;
                    }
                    if (_debugState == DebugState.GenPaths)
                    {
                        GenerateAltPathBlueprints(_zones[0]);
                        _debugState = DebugState.GenRooms;
                    }
                    if (_debugState == DebugState.GenRooms)
                    {
                        GenerateZoneRooms(_zones[0]);
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

            foreach (Zone zone in _zones)
            {
                DrawBoundingBox(zone.Bounds);
                DrawTriangulation();
                DrawBluePrintGizmos(zone);
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
            Vector3 boundsSize = _gridUnitSize * (bounds.size + Vector3Int.one);
            Vector3 boundsCenter = bounds.center * _gridUnitSize;

            Gizmos.color = _boundingBoxColor;
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
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

            // Draw the minimum spanning tree of the zone
            foreach (Edge e in _minimumSpanningTree)
            {
                Gizmos.color = _minimumSpanningTreeColor;
                Gizmos.DrawLine(e.V.Position * _gridUnitSize, e.U.Position * _gridUnitSize);
            }
        }

        private void DrawBluePrintGizmos(Zone zone)
        {
            if (zone.MainPath.BlueprintRooms == null)
                return;

            Vector3 unitSize = Vector3.one * _gridUnitSize;

            // Draw Gizmos for main path
            foreach (BlueprintRoom bRoom in zone.MainPath.BlueprintRooms)
            {
                Gizmos.color = zone.MainPath.PathGizmoColor;
                Gizmos.DrawCube(bRoom.Position * _gridUnitSize, unitSize);
            }

            foreach (Path path in zone.Paths)
            {
                if (path.BlueprintRooms == null)
                    return;

                // Draw Gizmos for alt paths
                foreach (BlueprintRoom bRoom in path.BlueprintRooms)
                {
                    Gizmos.color = path.PathGizmoColor;
                    Gizmos.DrawCube(bRoom.Position * _gridUnitSize, unitSize);
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
                Gizmos.DrawCube(bRoom.Position * _gridUnitSize, unitSize);
            }
        }
        #endregion
    }
}