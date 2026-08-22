/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   08/18/2026 (Ryan)
 * Notes:           
*/
using RyansLibrary.Debugging;
using RyansLibrary.UnityEditor;
using RyansLibrary.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Console = RyansLibrary.Debugging.Console;
using Math = RyansLibrary.Utilities.Math;
using Random = UnityEngine.Random;      // Using Unity Engine's Random not System.Collection's Random

namespace RyansLibrary.Labyrinth
{
    #region Helper Objects
    // Entry for connection zones together
    [System.Serializable]
    public class ZoneConnectionEntry
    {
        [field: Header("Zones")]
        [field: SerializeField] public Zone ZoneA { get; set; }
        [field: SerializeField] public Zone ZoneB { get; set; }
        [field: SerializeField] public Zone ConnectionZone { get; set; }
        [field: SerializeField] public BoundsInt ZoneSpawnBounds { get; set; }
    }
    #endregion

    /// <summary>
    /// Top-level driver and entry point for the whole labyrinth algorithm. GenerateLabyrinth() below runs the
    /// pipeline in four stages:
    ///   1. InitializeLabyrinth/SpawnZones - seed the RNG and position each Zone's bounds in world space.
    ///   2. LoadOperations - *builds* the generation algorithm as data: for every zone this constructs a chain of
    ///      BlueprintOperation/BlueprintData nodes (drunkard walk, triangulation, MST, pathfinding, ...) and queues
    ///      them in MapGenerationContext.OperationQueue. Nothing is actually generated yet at this point.
    ///   3. ExecuteOperations - drains that queue one operation at a time (a coroutine so it can be stepped/paused
    ///      for debugging via DebugSequential/Advance/AdvanceAll). This is where the abstract Blueprint grid actually
    ///      gets filled in.
    ///   4. GenerateRooms - walks the finished Blueprint grid and hands it to RoomGenerator to instantiate real room
    ///      prefabs in world space.
    /// Splitting "decide the algorithm" (stage 2) from "run it" (stage 3) is what the whole BlueprintOperation graph
    /// design is for.
    /// </summary>
    public class MapGeneratorController : MonoBehaviour
    {
        #region Variables

        // ***** Singleton Reference *****
        private static MapGeneratorController _instance;
        public static MapGeneratorController Instance => _instance;

        // ***** Events *****
        // General Events
        public static event Action OnGenerationStarted;
        public static event Action OnGenerationDone;
        public static event Action OnGenerationFailed;
        public static event Action OnGenerationReset;

        // Blueprint Events
        public static event Action OnOperationsStarted;
        public static event Action<int> OnOperationsGetTotal;
        public static event Action<int> OnOperationsUpdate;
        public static event Action OnOperationExecuted;
        public static event Action OnOperationsEnded;
        public static event Action OnSeedUpdate;

        // Room Events
        public static event Action OnRoomParseStarted;
        public static event Action OnRoomParseDone;

        // ***** Inspector Values *****
        [Tooltip("Enables map generation.")]
        [SerializeField]
        private bool _enabled = true;

        [Header("Seed")]
        [SerializeField] private int _customSeed = 0;
        [SerializeField] private bool _generateRandomSeed = true;
        [SerializeField, ReadOnly]
        private int _seed = 0;
        public int Seed => _seed;

        [Header("Global Settings")]
        [Tooltip("The size of a room unit or how large a 1x1 room is in Unity units.")]
        [SerializeField]
        private int _gridUnitSize = 13;                         // The unit size of a single grid cell
        public int GridUnitSize => _gridUnitSize;
        [SerializeField] private Transform _roomContainer;      // Parent transform that will contain all the spawned rooms

        [Header("Blueprint Settings")]
        [SerializeField] private int _maxPlacementAttempts = 50;        // Prevents infinate loops with divergent room placement.

        [Header("Zones")]
        [SerializeField]
        private List<Zone> _zones;
        public List<Zone> Zones => _zones;

        // Entries to connect zones together
        [Header("Zone Connection")]
        [SerializeField] private List<ZoneConnectionEntry> _zoneConnections;
        public List<ZoneConnectionEntry> ZoneConnections => _zoneConnections;

        // Debugging
        [Header("Debug")]
        [SerializeField] private bool _debugLogs = false;
        [SerializeField] private bool _debugBlueprintLogs = false;
        [SerializeField] private bool _debugRoomGeneratorLogs = false;

        // Flags
        private bool _isGenerating;
        public bool IsGenerating => _isGenerating;
        private bool _hasGenerated;
        public bool HasGenerated => _hasGenerated;
        private bool _isDubuggingSequential;
        public bool IsDebuggingSequential => _isDubuggingSequential;


        // Storage for blueprints and blueprint operations
        private MapGenerationContext _context;
        public MapGenerationContext Context => _context;

        private RoomGenerator _roomGenerator;

        // Stepwise procedure
        private int _stepBudget = 0;
        private bool _runToEnd = false;
        #endregion

        #region Mono
        private void Awake()
        {
            // Handle Singleton
            if (_instance != null)
            {
                Debug.LogWarning("Another instance of MapGeneratorController already exists. Deleting Object...");
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void Start()
        {
            RegisterConsoleCommands();

            _isGenerating = false;
            _hasGenerated = false;
        }

        public IEnumerator StartGeneration()
        {
            // Return if the Map Generator is not enabled
            if (!_enabled)
                yield break;

            yield return StartCoroutine(GenerateLabyrinth());
        }
        #endregion

        #region Labyrinth Algorithm Sequence
        #region Labyrinth Init Functions
        /// <summary>
        /// Set seed an initialize zone main paths.
        /// </summary>
        private void InitializeLabyrinth()
        {
            // Generate Seed
            if (_generateRandomSeed)
            {
                // Generate with random seed
                int seed = Random.Range(int.MinValue, int.MaxValue);
                SetSeed(seed);
            }
            else
                // Generate with custom seed
                SetSeed(_customSeed);

            Random.InitState(_seed);

            OnSeedUpdate?.Invoke();

            if (_debugLogs) Debug.Log($"Generating map with seed: ({_seed})");

            // Create new context - Proc gen state and storage manager
            _context = new();

            // Initialize Room Generator
            _roomGenerator = new RoomGenerator(_context, _gridUnitSize, _roomContainer);

            // Initialize the main path in each zone
            foreach (Zone zone in _zones)
            {
                // Init main zones
                InitializeZone(zone);
            }
            foreach (ZoneConnectionEntry entry in _zoneConnections)
            {
                // Init connection zones
                InitializeZone(entry.ConnectionZone);
            }

            // Toggle Debug Logs
            ToggleBlueprintLogs(_debugBlueprintLogs);
            ToggleRoomGeneratorLogs(_debugRoomGeneratorLogs);
        }

        private void InitializeZone(Zone zone)
        {
            zone.MainPath.Initialize();     // Must be done before zone connection blueprints
        }
        #endregion

        /// <summary>
        /// Entire generation procedure.
        /// 1. Initialization
        /// 2. Zone Spawning
        /// 3. Load and Execute Blueprint Operations
        /// 4. Generate Rooms
        /// </summary>
        /// <returns></returns>
        private IEnumerator GenerateLabyrinth()
        {
            // Do not Generate a labyrinth if one is already generating
            if (_isGenerating && !_hasGenerated)
                yield break;

            _isGenerating = true;

            // Event to signal when map generation has begun
            OnGenerationStarted?.Invoke();

            // Initialize Data Structures and Seed
            InitializeLabyrinth();

            // Spawn connection zones in random positions.
            // TODO: Later place all zones randomly using techniques like voronoi diagrams or bsp
            SpawnZones();

            // Load all blueprint data and operations into memory
            LoadOperations();

            // Parse the blueprint and generate rooms
            yield return StartCoroutine(ExecuteOperations());

            // Parse the blueprint and generate rooms
            GenerateRooms();

            // Labyrinth Generation Success
            // Event to signal when map generation is complete
            _isGenerating = false;
            _hasGenerated = true;
            OnGenerationDone?.Invoke();
        }

        private void SpawnZones()
        {
            // Spawn connection zone bounds
            // Each connection zone can spawn randomly inside a bounded space
            foreach (ZoneConnectionEntry zoneConnection in _zoneConnections)
            {
                BoundsInt connectionZoneBounds = zoneConnection.ConnectionZone.Bounds;
                BoundsInt connectionZoneSpawnBounds = zoneConnection.ZoneSpawnBounds;

                if (BoundsIntUtils.CanContainBounds(connectionZoneBounds, connectionZoneSpawnBounds))
                {
                    // Adjust the upper bounds so that the connection bound's volume will properly fit within the bounded space; in
                    // other words it will never spawn outside it's bounds
                    Vector3Int adjUpperBound = new Vector3Int(
                        connectionZoneSpawnBounds.xMax - connectionZoneBounds.size.x,
                        connectionZoneSpawnBounds.yMax - connectionZoneBounds.size.y,
                        connectionZoneSpawnBounds.zMax - connectionZoneBounds.size.z
                    );

                    // Choose random spawn pos in the room's bounds;
                    // NOTE: this random position is in room coords
                    Vector3Int randomSpawnPos = new Vector3Int(
                        Random.Range(connectionZoneSpawnBounds.xMin, adjUpperBound.x + 1),
                        Random.Range(connectionZoneSpawnBounds.yMin, adjUpperBound.y + 1),
                        Random.Range(connectionZoneSpawnBounds.zMin, adjUpperBound.z + 1)
                    );

                    // Move the connection zone to the random spawn position
                    zoneConnection.ConnectionZone.Bounds = new BoundsInt(randomSpawnPos, zoneConnection.ConnectionZone.Bounds.size);
                }
                else
                {
                    Debug.LogError("Zone Connection cannot fit within the spawning bounds.");
                    return;
                }
            }
        }

        /// <summary>
        /// Hardcoded operations listed in their order of execution. Loaded into context to be executed later.
        /// </summary>
        private void LoadOperations()
        {
            // ******* Load Zone Connection Blueprints *******
            // Load connection zone blueprints first and add parts of it to the zones
            foreach (ZoneConnectionEntry entry in _zoneConnections)
            {
                // Connect zones together with A*
                LoadConnectionZoneOperations(entry.ConnectionZone, entry.ZoneA, entry.ZoneB);
            }

            // ******* Load Zone Blueprints *******
            // Will generate an entire blueprint for a zone. Generates all paths
            // in zone and makes sure they are contiguous.
            foreach (Zone zone in _zones)
            {
                // Must have a zone to generate anything
                if (zone == null)
                {
                    Debug.LogError("Zone Entry Missing for blueprint procedure.");
                    return;
                }

                // Take the volume of the bounding cubic space and return an error if the amount of rooms to spawn is larger than that volume; make sure we have space for needed rooms
                if (!CheckZoneBoundedVolume(zone))
                {
                    Debug.LogError($"The amount of determined blueprint rooms desired for zone {zone.Name} exceeds " +
                        $"the bounding box's volume or the bounding box is inverted.");
                    return;
                }

                // ******* Generate Zone Blueprints *******
                // Generate Main Path to boss
                LoadMainPathOperations(zone);


                // Generate Alternative paths (prize, trial, etc.)
                LoadAltPathOperations(zone);
            }
        }

        #region Operation Execution
        private void ConsumeStep()
        {
            // Make sure the algorithm is still running
            if (!_isGenerating || _hasGenerated)
                return;

            if (_runToEnd)
                return;

            if (_stepBudget > 0)
                _stepBudget--;
        }

        public void Advance(int stepLength)
        {
            // Make sure the algorithm is still running
            if (!_isGenerating || _hasGenerated)
                return;

            if (stepLength <= 0)
                return;

            _stepBudget += stepLength;
            _runToEnd = false;
        }

        public void AdvanceAll()
        {
            // Make sure the algorithm is still running
            if (!_isGenerating || _hasGenerated)
                return;

            _runToEnd = true;
        }

        private IEnumerator ExecuteOperations()
        {
            OnOperationsStarted?.Invoke();
            OnOperationsGetTotal?.Invoke(GetOperationCount());

            // Execute operations and generate blueprints
            while (_context.OperationQueue.Count > 0)
            {
                if (_isDubuggingSequential)
                {
                    // Halt the execution of operations
                    while (!_runToEnd && _stepBudget <= 0)
                        yield return null;

                    ConsumeStep();
                }

                // Dequeue the current opration
                BlueprintOperation operation = _context.OperationQueueDequeue();
                if (operation == null)
                    throw new ArgumentNullException(nameof(operation));

                // Push the operation into history
                _context.OperationHistory.Push(operation);

                // Execute Operation
                if (_debugBlueprintLogs)
                {
                    Debug.Log($"{operation.OperationID} - Running Operation...");
                }

                bool result = operation.Execute();
                OnOperationExecuted?.Invoke();
                OnOperationsUpdate?.Invoke(GetOperationCount());

                if (_debugBlueprintLogs)
                {
                    Debug.Log(result ? $"{operation.OperationID} - Execution success!" :
                        $"{operation.OperationID} - Execution Failure.");
                }

                // Operation failed to execute; stop running coroutine
                if (!result)
                {
                    GenerationFailed();
                    yield break;
                }
            }

            OnOperationsEnded?.Invoke();

            if (_debugBlueprintLogs) Debug.Log("End of operation execution.");
        }
        #endregion

        private void GenerateRooms()
        {
            OnRoomParseStarted?.Invoke();

            // Generate Zone Connection Rooms
            // Since the connection zones are just a zone generate rooms normally
            foreach (ZoneConnectionEntry entry in _zoneConnections)
            {
                if (!GenerateZoneRooms(entry.ConnectionZone))
                {
                    GenerationFailed();
                    return;     // Room Generation failed for zone connection; stop algorithm
                }
            }


            foreach (Zone zone in _zones)
            {
                if (!GenerateZoneRooms(zone))
                {
                    Debug.LogError("Rooms failed to generate.");
                    GenerationFailed();
                    return;
                }
            }

            OnRoomParseDone?.Invoke();

            if (_debugBlueprintLogs) Debug.Log("End of room generation.");
        }

        public void ResetLabyrinth()
        {
            if (!Application.isPlaying)     // Only run code when game is executing
                return;

            // Reset generation flags
            _isGenerating = false;
            _hasGenerated = false;

            // Stop the entire generation coroutine chain
            StopAllCoroutines();
            _context.ClearAll();

            // Reset stepwise procedure values
            _stepBudget = 0;
            _runToEnd = false;

            DestroyAllRooms();      // Destroy all rooms from last generation

            OnGenerationReset?.Invoke();
            if (_debugLogs) Debug.Log("Map generator restarting.");
        }

        /// <summary>
        /// Resets Labyrinth and retrys generation
        /// </summary>
        private void GenerationFailed()
        {
            _isGenerating = false;
            _hasGenerated = false;

            // Stop the entire generation coroutine chain
            StopAllCoroutines();

            Debug.LogError("Map generation failed.");

            OnGenerationFailed?.Invoke();
        }
        #endregion

        #region Blueprint Procedure
        /// <summary>
        /// Wrapper function for generating the Main Path. 
        /// A path that leads to the zone boss, alternative paths, and 
        /// entrances to other zones.
        /// </summary>
        /// <param name="zone">Zone who's Main Path to generate.</param>
        /// <returns>Generation success or failure</returns>
        public void LoadMainPathOperations(Zone zone)
        {
            if (zone.MainPath == null)      // Throw error if MainPath for zone does not exist
            {
                Debug.LogError($"The Main Path for zone {zone.Name} is not assigned.");
                return;
            }

            // Unique Room Placement
            LoadUniqueRoomOperations(zone);

            // Divergent Room Placement
            LoadDivergentRoomOperations(zone);

            // Generate Delauney Triangulation
            LoadMainPathConnectionsOperations(zone);

            if (_debugLogs) Debug.Log($"{zone.Name} generated path {zone.MainPath.name} with {zone.MainPath.BlueprintCount()} rooms.");
        }

        /// <summary>
        /// Places all Unique Rooms specified in zone.
        /// </summary>
        /// <param name="zone">Zone to place unique rooms in</param>
        /// <returns>Placement success or failure</returns>
        public void LoadUniqueRoomOperations(Zone zone)
        {
            PathBlueprintData mainPathBlueprintData = new PathBlueprintData(_context, zone.MainPath);
            mainPathBlueprintData.LoadIntoMemory();
            BoundsIntBlueprintData zoneBoundsBlueprintData = new BoundsIntBlueprintData(_context, zone.Bounds);
            zoneBoundsBlueprintData.LoadIntoMemory();

            // 1.) Spawn Fixed Rooms (Rooms that have a set spawn destination)
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                if (entry.PlacementType == RoomPlacementType.Fixed)
                {
                    RoomEntryBlueprintData roomEntryBlueprintData = new RoomEntryBlueprintData(_context, entry);
                    roomEntryBlueprintData.LoadIntoMemory();

                    PlaceFixedBlueprintsOp placeFixedBlueprintOp = new PlaceFixedBlueprintsOp(_context,
                        mainPathBlueprintData.OutputPorts[0], roomEntryBlueprintData.OutputPorts[0], zoneBoundsBlueprintData.OutputPorts[0]);
                    _context.OperationQueueEnqueue(placeFixedBlueprintOp);
                }
            }

            // 2.) Spawn Bounded Rooms (Rooms that have a unique spawn area)
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                if (entry.PlacementType == RoomPlacementType.Constrained)
                {
                    // Attempt to place the bounded room in it's own bounded zone
                    RoomEntryBlueprintData roomEntryBlueprintData = new RoomEntryBlueprintData(_context, entry);
                    roomEntryBlueprintData.LoadIntoMemory();

                    CheckOutOfBoundsOp adjBoundsBlueprintOp = new CheckOutOfBoundsOp(_context,
                        zoneBoundsBlueprintData.OutputPorts[0], roomEntryBlueprintData.OutputPorts[1]);
                    _context.OperationQueueEnqueue(adjBoundsBlueprintOp);

                    PlaceBoundedBlueprintsOp placeBoundedBlueprintsOp = new PlaceBoundedBlueprintsOp(_context,
                        mainPathBlueprintData.OutputPorts[0], roomEntryBlueprintData.OutputPorts[0], adjBoundsBlueprintOp.OutputPorts[0]);
                    _context.OperationQueueEnqueue(placeBoundedBlueprintsOp);
                }
            }

            // 3.) Spawn Free Rooms (Rooms that can spawn in any point inside the zone bounds)
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                if (entry.PlacementType == RoomPlacementType.Free)
                {
                    RoomEntryBlueprintData roomEntryBlueprintData = new RoomEntryBlueprintData(_context, entry);
                    roomEntryBlueprintData.LoadIntoMemory();

                    PlaceBoundedBlueprintsOp placeBoundedBlueprintsOp = new PlaceBoundedBlueprintsOp(_context,
                        mainPathBlueprintData.OutputPorts[0], roomEntryBlueprintData.OutputPorts[0], zoneBoundsBlueprintData.OutputPorts[0]);
                    _context.OperationQueueEnqueue(placeBoundedBlueprintsOp);
                }
            }
        }

        private void LoadDivergentRoomOperations(Zone zone)
        {
            PathBlueprintData mainPathBlueprintData = new PathBlueprintData(_context, zone.MainPath);
            mainPathBlueprintData.LoadIntoMemory();
            BoundsIntBlueprintData zoneBoundsBlueprintData = new BoundsIntBlueprintData(_context, zone.Bounds);
            zoneBoundsBlueprintData.LoadIntoMemory();
            Vector3IntBlueprintData dimensionsData = new Vector3IntBlueprintData(_context, Vector3Int.one);
            dimensionsData.LoadIntoMemory();
            IntBlueprintData cellCountData = new IntBlueprintData(_context, zone.DivergentRoomsCellOccupancy);
            cellCountData.LoadIntoMemory();
            IntBlueprintData maxPlacementAttemptsData = new IntBlueprintData(_context, _maxPlacementAttempts);
            maxPlacementAttemptsData.LoadIntoMemory();

            DivergentBlueprintsOp divergentRoomsOp = new DivergentBlueprintsOp(_context, mainPathBlueprintData.OutputPorts[0], zoneBoundsBlueprintData.OutputPorts[0],
                dimensionsData.OutputPorts[0], cellCountData.OutputPorts[0], maxPlacementAttemptsData.OutputPorts[0]);
            _context.OperationQueueEnqueue(divergentRoomsOp);
        }

        // This method is the heart of the labyrinth algorithm: it turns a scattered set of rooms into a connected,
        // loopy dungeon graph and then physically carves corridors between them. Overview of the pipeline it builds
        // (each step below is queued as a BlueprintOperation node, not executed immediately - see BlueprintOperation.cs
        // for why operations talk through memory IDs instead of calling each other directly):
        //   1. Delaunay-triangulate every available room position (2D or 3D depending on the zone's height) to get
        //      a graph where nearby rooms are connected - this over-connects everything, which is intentional.
        //   2. Run Prim's algorithm (FindMSTOp) to reduce that to a Minimum Spanning Tree: the fewest edges needed
        //      so every room is still reachable. A pure MST makes a maze with no loops, which plays poorly, so...
        //   3. A pure MST makes a maze with no loops, which plays poorly, so random edges from the triangulation graph
        //      are choosen to make forks and cycles in the dungeon.
        //   4. Each choosen edge is turned into an actual corridor of rooms via A* pathfinding (PathfindingBlueprintOp),
        //      obstructed by rooms already claimed by the main path so corridors don't cut through existing rooms.
        private void LoadMainPathConnectionsOperations(Zone zone)
        {
            // ***** Form Triangulation *****
            PathBlueprintData mainPathBlueprintData = new PathBlueprintData(_context, zone.MainPath);
            mainPathBlueprintData.LoadIntoMemory();
            BoolBlueprintData availableBlueprintData = new BoolBlueprintData(_context, true);
            availableBlueprintData.LoadIntoMemory();
            BoolBlueprintData unavailableBlueprintData = new BoolBlueprintData(_context, false);
            unavailableBlueprintData.LoadIntoMemory();
            StringBlueprintData edgeTypeBlueprintData = new StringBlueprintData(_context, "Edge");
            edgeTypeBlueprintData.LoadIntoMemory();
            IntBlueprintData setSize = new IntBlueprintData(_context, zone.RandomCyclesInGraph);
            setSize.LoadIntoMemory();

            GetAvailableBlueprintsOp availibleBlueprintsOp = new GetAvailableBlueprintsOp(_context, mainPathBlueprintData.OutputPorts[2],
                availableBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(availibleBlueprintsOp);

            // *** Choose Between 2D and 3D Triangulation based on bounds size *****
            // TODO: DelaunayTriangulation has issues solving cases with coplanar tetrahedra; instead consider
            // a C# library called MIConvexHull that has thousands of lines to solve these issues.
            FindMSTOp mstOp;
            ListDifferenceOp listDiffOp;

            if (zone.Bounds.size.y < 3)
            {
                // Perform 2D Triangulation if bounds size is < 3
                TriangulateBlueprints2DOp triangulationOp = new TriangulateBlueprints2DOp(_context, availibleBlueprintsOp.OutputPorts[0]);
                _context.OperationQueueEnqueue(triangulationOp);

                mstOp = new FindMSTOp(_context, triangulationOp.OutputPorts[0]);
                _context.OperationQueueEnqueue(mstOp);

                listDiffOp = new ListDifferenceOp(_context, triangulationOp.OutputPorts[0], mstOp.OutputPorts[0]);
                _context.OperationQueueEnqueue(listDiffOp);
            }
            else
            {
                // Perform 3D Triangulation if bounds size in >= 3
                TriangulateBlueprints3DOp triangulationOp = new TriangulateBlueprints3DOp(_context, availibleBlueprintsOp.OutputPorts[0]);
                _context.OperationQueueEnqueue(triangulationOp);

                mstOp = new FindMSTOp(_context, triangulationOp.OutputPorts[0]);
                _context.OperationQueueEnqueue(mstOp);

                listDiffOp = new ListDifferenceOp(_context, triangulationOp.OutputPorts[0], mstOp.OutputPorts[0]);
                _context.OperationQueueEnqueue(listDiffOp);
            }

            ListSelectRandomSetOp randomCyclesListOp = new ListSelectRandomSetOp(_context, listDiffOp.OutputPorts[0],
                setSize.OutputPorts[0]);
            _context.OperationQueueEnqueue(randomCyclesListOp);

            ListUnionOp zoneGraphUnionOp = new ListUnionOp(_context, mstOp.OutputPorts[0], randomCyclesListOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(zoneGraphUnionOp);

            // **** Pathfinding *****
            IntBlueprintData currentIndexBlueprintData = new IntBlueprintData(_context, 0);     // i = 0
            currentIndexBlueprintData.LoadIntoMemory();
            IntBlueprintData intOneBlueprintData = new IntBlueprintData(_context, 1);           // Increment amount
            intOneBlueprintData.LoadIntoMemory();
            StringBlueprintData targetOpIDBlueprintData = new StringBlueprintData(_context, "");    // NOP operation ID; filled in later
            targetOpIDBlueprintData.LoadIntoMemory();
            HeuristicBlueprintData pathfindingHeuristicData = new HeuristicBlueprintData(_context, zone.DefaultPathfindingHeuristic);
            pathfindingHeuristicData.LoadIntoMemory();
            BoundsIntBlueprintData zoneBoundsBlueprintData = new BoundsIntBlueprintData(_context, zone.Bounds);
            zoneBoundsBlueprintData.LoadIntoMemory();

            // Loop
            BranchGreaterOrEqualOp bgeOp = new BranchGreaterOrEqualOp(_context, targetOpIDBlueprintData.OutputPorts[0], currentIndexBlueprintData.OutputPorts[0],
                zoneGraphUnionOp.OutputPorts[1]);
            _context.OperationQueueEnqueue(bgeOp);

            StringBlueprintData branchIDBlueprintData = new StringBlueprintData(_context, bgeOp.OperationID);
            branchIDBlueprintData.LoadIntoMemory();

            // Pathfinding logic
            AccessListElementOp currentEdgeOp = new AccessListElementOp(_context, currentIndexBlueprintData.OutputPorts[0], zoneGraphUnionOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(currentEdgeOp);

            ExtractVerticesFromEdgeOp verticiesFromEdgeOp = new ExtractVerticesFromEdgeOp(_context, currentEdgeOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(verticiesFromEdgeOp);

            FindBlueprintFromPositionOp blueprintStart = new FindBlueprintFromPositionOp(_context, verticiesFromEdgeOp.OutputPorts[2]);
            _context.OperationQueueEnqueue(blueprintStart);

            FindBlueprintFromPositionOp blueprintEnd = new FindBlueprintFromPositionOp(_context, verticiesFromEdgeOp.OutputPorts[3]);
            _context.OperationQueueEnqueue(blueprintEnd);

            GetAvailableBlueprintsOp findObstructionsOp = new GetAvailableBlueprintsOp(_context, mainPathBlueprintData.OutputPorts[2],
                unavailableBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(findObstructionsOp);

            PathfindingBlueprintOp pathFindingOp = new PathfindingBlueprintOp(_context, mainPathBlueprintData.OutputPorts[0], blueprintStart.OutputPorts[0],
                blueprintEnd.OutputPorts[0], zoneBoundsBlueprintData.OutputPorts[0], findObstructionsOp.OutputPorts[0], pathfindingHeuristicData.OutputPorts[0]);
            _context.OperationQueueEnqueue(pathFindingOp);
            // End of Pathfinding logic

            AddIntOp incrementOp = new AddIntOp(_context, currentIndexBlueprintData.OutputPorts[0], intOneBlueprintData.OutputPorts[0],
                currentIndexBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(incrementOp);

            JumpOp jumpOp = new JumpOp(_context, branchIDBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(jumpOp);
            // End loop

            NoOp targetIDNoOp = new NoOp(_context);                       // Load this operation after loop; jump target for bge
            targetOpIDBlueprintData.ModifyData(targetIDNoOp.OperationID);
            _context.OperationQueueEnqueue(targetIDNoOp);
        }

        // Builds one drunkard-walk branch per Path in zone.Paths (side content: prize rooms, trial rooms, etc.),
        // each one starting from a random point already claimed on the main path (branchedPathBlueprintData) rather
        // than from scratch, so alt paths always connect back into the zone's spine instead of floating disconnected.
        private void LoadAltPathOperations(Zone zone)
        {
            PathBlueprintData branchedPathBlueprintData = new PathBlueprintData(_context, zone.MainPath);
            branchedPathBlueprintData.LoadIntoMemory();
            BoundsIntBlueprintData boundsBlueprintData = new BoundsIntBlueprintData(_context, zone.Bounds);
            boundsBlueprintData.LoadIntoMemory();
            IntBlueprintData startIndexBlueprintData = new IntBlueprintData(_context, 1);
            startIndexBlueprintData.LoadIntoMemory();
            IntBlueprintData negativeOneBlueprintData = new IntBlueprintData(_context, -1);
            negativeOneBlueprintData.LoadIntoMemory();

            foreach (Path path in zone.Paths)
            {
                if (path == null)
                {
                    Debug.LogError($"A path {path.Name} for zone {zone.name} is not assigned.");
                    return;
                }

                // Initialize path
                path.Initialize();

                PathBlueprintData pathBlueprintData = new PathBlueprintData(_context, path);
                pathBlueprintData.LoadIntoMemory();

                BoolBlueprintData canGoVerticalData = new BoolBlueprintData(_context, path.DrunkardWalkCanGoVertical);
                canGoVerticalData.LoadIntoMemory();

                GetPathLengthOp branchedpathLengthOp = new GetPathLengthOp(_context, branchedPathBlueprintData.OutputPorts[0]);
                _context.OperationQueueEnqueue(branchedpathLengthOp);

                AddIntOp lengthMinusOneOp = new AddIntOp(_context, branchedpathLengthOp.OutputPorts[0], negativeOneBlueprintData.OutputPorts[0]);
                _context.OperationQueueEnqueue(lengthMinusOneOp);

                DrunkardWalkBlueprintOp drunkardWalkOperation = new DrunkardWalkBlueprintOp(_context, pathBlueprintData.OutputPorts[0], branchedPathBlueprintData.OutputPorts[0],
                    boundsBlueprintData.OutputPorts[0], startIndexBlueprintData.OutputPorts[0], lengthMinusOneOp.OutputPorts[0], canGoVerticalData.OutputPorts[0]);
                _context.OperationQueueEnqueue(drunkardWalkOperation);
            }
        }

        // A "connection zone" is a small in-between zone (e.g. a hallway/gate) that links two otherwise separate
        // zones together: its first two UniqueRooms (connectionZone.UniqueRooms[0]/[1]) are placed inside the
        // overlapping area between the connection zone and each parent zone (BoundsIntersectOp), then a corridor
        // is pathfound between a random open cell in each of those two rooms. This is how the overall labyrinth
        // ends up as multiple independently-generated zones stitched into one connected world instead of one giant
        // zone.
        public void LoadConnectionZoneOperations(Zone connectionZone, Zone zoneA, Zone zoneB)
        {
            // *** Error Handling ***
            if (connectionZone == null)
            {
                Debug.LogError("Connection Zone of zone connection was null.");
                return;
            }
            if (zoneA == null)
            {
                Debug.LogError("Zone A of zone connection was null.");
                return;
            }
            if (zoneB == null)
            {
                Debug.LogError("zone B of zone connection was null.");
                return;
            }
            if (connectionZone.MainPath == null)
            {
                Debug.LogError($"The Main Path for connection zone {connectionZone.Name} is not assigned.");
                return;
            }
            if (connectionZone.UniqueRooms.Count < 2)
            {
                Debug.LogError($"Connection zone {connectionZone.Name} must have at least two unique rooms assigned for zone connection.");
                return;
            }

            // *** Connection Zone Data ***
            // Connection Zone Path BlueprintData
            PathBlueprintData zoneConnectionMainPathBlueprintData = new PathBlueprintData(_context, connectionZone.MainPath);
            zoneConnectionMainPathBlueprintData.LoadIntoMemory();
            // Connection Zone Bounds BlueprintData
            BoundsIntBlueprintData zoneConnectionBoundsBlueprintData = new BoundsIntBlueprintData(_context, connectionZone.Bounds);
            zoneConnectionBoundsBlueprintData.LoadIntoMemory();

            // *** Zone A Data ***
            // Zone A BlueprintData
            PathBlueprintData zoneAMainPathBlueprintData = new PathBlueprintData(_context, zoneA.MainPath);
            zoneAMainPathBlueprintData.LoadIntoMemory();
            // Room A BlueprintData
            RoomEntryBlueprintData roomABlueprintData = new RoomEntryBlueprintData(_context, connectionZone.UniqueRooms[0]);
            roomABlueprintData.LoadIntoMemory();
            // Zone A Bounds BlueprintData
            BoundsIntBlueprintData zoneABoundsBlueprintData = new BoundsIntBlueprintData(_context, zoneA.Bounds);
            zoneABoundsBlueprintData.LoadIntoMemory();

            // *** Zone B Data ***
            // Zone B BlueprintData
            PathBlueprintData zoneBMainPathBlueprintData = new PathBlueprintData(_context, zoneB.MainPath);
            zoneBMainPathBlueprintData.LoadIntoMemory();
            // Room B BlueprintData
            RoomEntryBlueprintData roomBBlueprintData = new RoomEntryBlueprintData(_context, connectionZone.UniqueRooms[1]);
            roomBBlueprintData.LoadIntoMemory();
            // Zone B Bounds BlueprintData
            BoundsIntBlueprintData zoneBBoundsBlueprintData = new BoundsIntBlueprintData(_context, zoneB.Bounds);
            zoneBBoundsBlueprintData.LoadIntoMemory();

            // **** Room A Placement Operations ***
            // Find the bounds area intersection of the two zones to find where the connection can be placed; this will be used for placing the room and path of the zone connection
            BoundsIntersectOp zoneConnectzoneAIntersectOp = new BoundsIntersectOp(_context, zoneABoundsBlueprintData.OutputPorts[0],
                zoneConnectionBoundsBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(zoneConnectzoneAIntersectOp);
            // Place Room A in Zone A
            PlaceBoundedBlueprintsOp placeRoomAOp = new PlaceBoundedBlueprintsOp(_context, zoneAMainPathBlueprintData.OutputPorts[0], roomABlueprintData.OutputPorts[0],
                zoneConnectzoneAIntersectOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(placeRoomAOp);

            // **** Room B Placement Operations ***
            // Find the bounds area intersection of the two zones to find where the connection can be placed; this will be used for placing the room and path of the zone connection
            BoundsIntersectOp zoneConnectzoneBIntersectOp = new BoundsIntersectOp(_context, zoneConnectionBoundsBlueprintData.OutputPorts[0],
                zoneBBoundsBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(zoneConnectzoneBIntersectOp);
            // Place Room B in Zone B
            PlaceBoundedBlueprintsOp placeRoomBOp = new PlaceBoundedBlueprintsOp(_context, zoneBMainPathBlueprintData.OutputPorts[0], roomBBlueprintData.OutputPorts[0],
                zoneConnectzoneBIntersectOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(placeRoomBOp);

            // *** PathFind Operations ***
            // Initialize pathfinding data
            BoolBlueprintData availableBlueprintData = new BoolBlueprintData(_context, true);
            availableBlueprintData.LoadIntoMemory();
            StringBlueprintData blueprintTypeBlueprintData = new StringBlueprintData(_context, "Blueprint");
            blueprintTypeBlueprintData.LoadIntoMemory();

            // Select random available blueprints from each room to be the start and end points for pathfinding
            GetAvailableBlueprintsOp roomAAvailableBlueprintsOp = new GetAvailableBlueprintsOp(_context, placeRoomAOp.OutputPorts[0], availableBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(roomAAvailableBlueprintsOp);
            GetAvailableBlueprintsOp roomBAvailableBlueprintsOp = new GetAvailableBlueprintsOp(_context, placeRoomBOp.OutputPorts[0], availableBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(roomBAvailableBlueprintsOp);
            ListSelectRandomElementOp randomBlueprintFromRoomAOp = new ListSelectRandomElementOp(_context, roomAAvailableBlueprintsOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(randomBlueprintFromRoomAOp);
            ListSelectRandomElementOp randomBlueprintFromRoomBOp = new ListSelectRandomElementOp(_context, roomBAvailableBlueprintsOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(randomBlueprintFromRoomBOp);

            // Fill obstructions list with all blueprints from the main paths of both zones except the ones that are part of the roomA and roomB
            ListUnionOp obstructionsListOp = new ListUnionOp(_context, zoneAMainPathBlueprintData.OutputPorts[2], zoneBMainPathBlueprintData.OutputPorts[2]);
            _context.OperationQueueEnqueue(obstructionsListOp);

            // Pathfind a connection between the two rooms along the bounds intersection area while avoiding main path blueprints
            PathfindingBlueprintOp pathfindOp = new PathfindingBlueprintOp(_context, zoneConnectionMainPathBlueprintData.OutputPorts[0], randomBlueprintFromRoomAOp.OutputPorts[0],
                randomBlueprintFromRoomBOp.OutputPorts[0], zoneConnectionBoundsBlueprintData.OutputPorts[0]);//  obstructionsListOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(pathfindOp);
        }
        #endregion

        #region RoomGenerationProcedure
        /// <summary>
        /// Second procedure of the Labyrinth Algorithm. Will parse through all of the 
        /// paths and generate rooms based on conditions. These conditions are based on 
        /// room shape chance, room prefab chance, if the room shape will align adequately to the path, and what path
        /// the room is a part of. It will also activate the entranceways of rooms based on the path's sequence.
        /// </summary>
        public bool GenerateZoneRooms(Zone zone)
        {
            // Must have a zone to generate anything
            if (zone == null)
            {
                Debug.LogError($"Zone Entry Missing for room generation procedure.");
                return false;
            }

            // Generate Unique Rooms
            bool result;
            result = GenerateUniqueRooms(zone);
            if (!result)
            {
                Debug.LogError($"Unique Room Generation for zone {zone} failed.");
                return false;
            }

            // Turn off blueprint availability for unique rooms; we do not want to parse and spawn new rooms in these spots
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                Vector3Int actualPosition = entry.SpawnPosition;

                // If room is Fixed then actual position needs to be calculated relative to the bounded zone
                if (entry.PlacementType == RoomPlacementType.Fixed)
                    actualPosition += zone.Bounds.position;

                if (entry.Prefab.TryGetComponent(out Room room))
                    BlueprintGenerator.ToggleAvailableBlueprintsInRoom(_context, zone.MainPath, room.RoomCells, actualPosition, false);
                else
                    Debug.LogError($"Failed to get Room component from prefab {entry.Prefab}.");

                if (_debugRoomGeneratorLogs) Debug.Log("Blueprint Room: " + actualPosition + "has available cells disabled.");
            }

            // Generate Rooms along main path
            result = _roomGenerator.ParsePathAndGenerateRooms(zone.MainPath);
            if (!result)
            {
                Debug.LogError($"Path Room Generation for path {zone.MainPath} in zone {zone} failed.");
                return false;
            }

            // Generate Rooms along alt. paths
            foreach (Path path in zone.Paths)
            {
                result = _roomGenerator.ParsePathAndGenerateRooms(path);
                if (!result)
                {
                    Debug.LogError($"Path Room Generation for path {path} in zone {zone} failed.");
                    return false;
                }
            }
            return true;
        }

        // Unlike ParsePathAndGenerateRooms (which picks a room shape/prefab based on the path's blueprint layout),
        // unique rooms already know their exact prefab from RoomEntry, so this just instantiates it directly at its
        // resolved position and copies over whichever entranceway flags were set on the blueprint grid during
        // generation (so doors line up with whatever corridor was pathfound into this room).
        public bool GenerateUniqueRooms(Zone zone)
        {
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                if (_context.BlueprintDictionary == null)
                {
                    Debug.Log("Masters are null.");
                    return false;
                }

                // TODO: This needs to be changed to a more universal solution.
                Vector3Int actualPosition = entry.SpawnPosition;
                if (entry.PlacementType == RoomPlacementType.Fixed)
                    actualPosition += zone.Bounds.position;

                Room generatedRoom = _roomGenerator.GenerateRoom(entry.Prefab, actualPosition, zone.MainPath);

                // TODO: Make this into a new function in the room generator. Make the function check for all cells inside
                // the unique room.
                if (generatedRoom.RoomCells != null)
                {
                    for (int i = 0; i < generatedRoom.RoomCells.Count; i++)
                    {
                        if (!generatedRoom.RoomCells[i].IsAvilable)     // Make sure cell is available
                            continue;

                        if (_context.BlueprintDictionary.TryGetValue(actualPosition + generatedRoom.RoomCells[i].Position, out Blueprint blueprint))
                        {
                            generatedRoom.CopyBlueprintEntranceFlags(blueprint.EntryPointFlags, i);
                        }
                        else
                        {
                            Debug.LogError($"Could not copy entranceway flags into unique room {entry}.");
                            return false;
                        }
                    }
                }

                generatedRoom.Initialize();
            }

            return true;
        }
        #endregion

        #region Utility
        /// <summary>
        /// Checks if the total amount of desired rooms in zone is valid in a zone's bounded range.
        /// </summary>
        /// <returns>The test success or fail</returns>
        private bool CheckZoneBoundedVolume(Zone zone)
        {
            float totalCellOccupancy = 0;

            // Add Unique Room volume
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                if (entry.Prefab.TryGetComponent(out Room room))
                {
                    totalCellOccupancy += room.GetRoomOccupancy();
                }
                else
                    Debug.LogWarning("Room Entry Prefab has no Room Script");
            }

            // Add Divergent Room volume
            totalCellOccupancy += zone.DivergentRoomsCellOccupancy;

            // Add Main Path volume
            totalCellOccupancy += zone.MainPath.DesiredPathLength;

            // Add Alt. Paths volume
            foreach (Path path in zone.Paths)
                totalCellOccupancy += path.DesiredPathLength;

            // Calculate the bounded volume and check if amount of room cells taken up exceeds that amount
            float xSize = zone.Bounds.size.x;
            float ySize = zone.Bounds.size.y;
            float zSize = zone.Bounds.size.z;
            float volume = Math.RectangularVolume(xSize, ySize, zSize);

            if (volume < totalCellOccupancy)        // The bounded volume cannot fullfill the zone's cell requirements
                return false;

            return true;        // The zone's cell requirements are met with the bounded volume
        }

        public int GetOperationCount()
        {
            if (_context == null)
            {
                Debug.LogError("Context is not assigned.");
                return 0;
            }

            return _context.GetOperationQueueCount();

        }

        private int SetSeed(int seed)
        {
            _seed = seed;
            Random.InitState(_seed);
            return _seed;
        }

        public void DestroyAllRooms()
        {
            foreach (Transform child in _roomContainer.transform)
                Destroy(child.gameObject);
        }
        #endregion

        #region Debug
        // Log Toggles
        public void ToggleLogs(bool toggle)
        {
            _debugLogs = toggle;
        }

        // Toggle blueprint logs; this includes blueprint operations and blueprint spawning
        public void ToggleBlueprintLogs(bool toggle)
        {
            _debugBlueprintLogs = toggle;
            BlueprintOperation.ToggleDebugLogs(toggle);
            // BlueprintData<>.ToggleDebugLogs(toggle);

            BlueprintGenerator.ToggleDebugLogs(_debugBlueprintLogs);
        }

        public void ToggleRoomGeneratorLogs(bool toggle)
        {
            if (_roomGenerator == null)
                return;

            _roomGenerator.ToggleDebugLogs(toggle);
        }

        // Stepwise Function Toggles
        public void ToggleStepwiseDebugging(bool toggle)
        {
            _isDubuggingSequential = toggle;
        }
        #endregion

        #region Console Commands
        private void RegisterConsoleCommands()
        {
            // Map generator step command - Step the map generator by a desired amount of operations.
            Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.step",
                "When debugging will advance the map generator operation queue by a desired amount of operations.",
                args =>
                {
                    int stepLength = 1;        // Default step length

                    if (args.Length < 1)        // No step amount given; default to stepping 1 operation
                    {
                        Advance(stepLength);
                        return;
                    }
                    else if (int.TryParse(args[0], out stepLength))      // Step amount given and is valid
                    {
                        if (stepLength < 1)
                        {
                            Debug.LogWarning($"Invalid argument '{args[0]}'. Please enter a positive amount of steps to advance.");
                            return;
                        }

                        Advance(stepLength);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid argument '{args[0]}'. Please enter a positive amount of steps to advance.");
                    }
                    Debug.Log($"Map generator stepped {stepLength} operation(s).");
                }));

            // Map generator step all command - Execute all of the remaining map generator operations.
            Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.stepall",
                "When debugging, will execute all the remaining operations in the operation queue.",
                args =>
                {
                    AdvanceAll();
                    Debug.Log("Map Generator executed all remaining operations.");
                }));

            // Map generator reset command - Resets and restarts the map generator state.
            Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.start",
                "Begins generating a new map. Cannot be used while generating or when a map is already generated.",
                args =>
                {
                    StartCoroutine(GenerateLabyrinth());
                    Debug.Log("All data deleted. Map Generator restarted.");
                }));

            // Map generator reset command - Resets and restarts the map generator state.
            Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.reset",
                "When debugging, will reset the map generator and start a new generation.",
                args =>
                {
                    ResetLabyrinth();
                    Debug.Log("All data deleted. Map Generator restarted.");
                }));

            // Register custom seed command - Sets the map generator to use a custom seed for generation.
            Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.setseed",
                "Sets the map generator to use a custom seed for generation.",
                args =>
                {
                    int seed = 0;

                    if (args.Length < 1)
                    {
                        Debug.LogWarning("No argument given, please enter a valid seed value between " + int.MinValue + " and " + int.MaxValue + ".");
                        return;
                    }
                    else if (!int.TryParse(args[0], out seed))
                    {
                        Debug.LogWarning("Invalid argument given, please enter a valid seed value between " + int.MinValue + " and " + int.MaxValue + ".");
                        return;
                    }

                    _customSeed = seed;

                    OnSeedUpdate?.Invoke();
                    Debug.Log($"Map Generator seed set to {seed}.");
                }
                ));

            // Register custom seed command - Sets the map generator to use a custom seed for generation.
            Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.seed",
                "Displays the current seed value to the console.",
                args =>
                {
                    Debug.Log($"Seed: {_seed}");
                }
                ));

            // Map generator restart command - Resets and restarts the map generator state.
            Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.togglerandomseed",
                "Toggles the use of a random seed for map generation.",
                args =>
                {
                    if (args.Length < 1)
                    {
                        Debug.LogWarning("No argument given, please enter true or false.");
                        return;
                    }
                    else if (args[0] == "true")
                    {
                        _generateRandomSeed = true;
                    }
                    else if (args[0] == "false")
                    {
                        _generateRandomSeed = false;
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid argument '{args[0]}'. Please input either true or false.");
                    }

                    OnSeedUpdate?.Invoke();
                    Debug.Log($"Map Generator toggle random seed set to {_generateRandomSeed}.");
                }));

            // Map generator restart command - Resets and restarts the map generator state.
            Console.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.toggleblueprintstacktrace",
                "Displays blueprint operation log to the console.",
                args =>
                {
                    if (args.Length < 1)
                    {
                        Debug.LogWarning("No argument given, please enter true or false.");
                        return;
                    }
                    else if (args[0] == "true")
                    {
                        ToggleBlueprintLogs(true);
                    }
                    else if (args[0] == "false")
                    {
                        ToggleBlueprintLogs(false);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid argument '{args[0]}'. Please input either true or false.");
                    }

                    Debug.Log($"Blueprint stack trace set to {_debugBlueprintLogs}.");
                }, true));
        }
        #endregion
    }
}
