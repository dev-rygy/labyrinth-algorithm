/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   02/08/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.Console;
using RyansLibrary.UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;      // Use Unity Engine's Random not System.Collection's Random

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

    public class MapGeneratorController : MonoBehaviour
    {
        #region Variables

        // ***** Singleton Reference *****
        public static MapGeneratorController Instance { get; private set; }

        // ***** Events *****
        public static event Action OnGenerationStarted;
        public static event Action OnGenerationDone;
        public static event Action OnGenerationFailed;

        public static event Action OnOperationsStarted;
        public static event Action<int> OnOperationsGetTotal;
        public static event Action<int> OnOperationsUpdate;
        public static event Action OnOperationExecuted;
        public static event Action OnOperationsEnded;
        public static event Action OnSeedUpdate;

        public static event Action OnRoomParseStarted;
        public static event Action OnRoomParseDone;

        // Public Flags
        public bool IsGenerating { get; private set; }
        public bool DebugSequential { get; private set; }

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
        private int _gridUnitSize = 13;                        // The unit size of the room grid's cell
        public int GridUnitSize => _gridUnitSize;
        [SerializeField] private Transform _roomContainer;                      // Parent transform that will contain all the spawned rooms

        [Header("Blueprint Settings")]
        [SerializeField] private int _maxPlacementAttempts = 50;

        [Header("Zones")]
        [SerializeField] 
        private List<Zone> _zones;
        public List<Zone> Zones => _zones;

        // Entries to connect zones together
        [Header("Zone Connection")]
        [SerializeField] 
        private List<ZoneConnectionEntry> _zoneConnections;
        public List<ZoneConnectionEntry> ZoneConnections => _zoneConnections;

        // ***** Private Variables *****
        private Coroutine _mapGeneratorCoroutine;

        private BlueprintGenerator _bpg;
        private RoomGenerator _roomGenerator;
        private MapGenerationContext _context;
        public MapGenerationContext Context => _context;

        // Debugging
        private bool _debug = false;

        // Logs
        private bool _debugLogs = false;
        private bool _debugBlueprintLogs = false;
        private bool _debugRoomGeneratorLogs = false;

        // Stepwise procedure
        private int _stepBudget = 0;
        private bool _runToEnd = false;
        #endregion

        #region Mono
        private void Awake()
        {
            // Handle Singleton
            if (Instance != null)
            {
                Debug.LogWarning("[MapGenerator][Controller] Another instance of MapGeneratorController already exists. Deleting Object...");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            RegisterConsoleCommands();
        }

        public IEnumerator StartGeneration()
        {
            // Return if the Map Generator is not enabled
            if (!_enabled)
                yield break;

            if (_debug)
            {
                Debug.Log("[MapGenerator][Controller] Debug On");
            }
            else
                Debug.Log("[MapGenerator][Controller] Debug Off");

            _mapGeneratorCoroutine = StartCoroutine(GenerateLabyrinth());
            yield return _mapGeneratorCoroutine;
        }
        #endregion

        #region Labyrinth Algorithm Sequence
        #region Labyrinth Init Functions
        private void InitializeLabyrinth()
        {
            // Handle Map Seed Generation
            if (_generateRandomSeed)
            {
                int seed = Random.Range(int.MinValue, int.MaxValue);                  // Generate with random seed
                SetSeed(seed);
            }
            else
                SetSeed(_customSeed);         // Generate with custom seed

            Random.InitState(_seed);

            OnSeedUpdate?.Invoke();

            if (_debugLogs) Debug.Log($"[MapGenerator][Controller] Generating map with seed: {_seed}");

            _context = new();

            // Initialize Master Data Structures
            _context.InitializeMasters();

            // Initialize Blueprint Generator
            _bpg = new(_context);

            // Initialize Room Generator
            _roomGenerator = new RoomGenerator(_context, _gridUnitSize, _roomContainer);

            // Toggle Logs
            ToggleBlueprintLogs(_debugBlueprintLogs);
            ToggleRoomGeneratorLogs(_debugRoomGeneratorLogs);

            // Initialize the Main Path in each Zone
            foreach (Zone zone in _zones)
            {
                InitializeZone(zone);
            }
            foreach (ZoneConnectionEntry entry in _zoneConnections)
            {
                InitializeZone(entry.ConnectionZone);
            }
        }

        private void InitializeZone(Zone zone)
        {
            zone.MainPath.Initialize();     // Must be done before zone connection blueprints
        }
        #endregion

        private IEnumerator GenerateLabyrinth()
        {
            // Do not Generate a labyrinth if one is already generating
            if (IsGenerating)
                yield break;

            IsGenerating = true;

            // Event to signal when map generation has begun
            OnGenerationStarted?.Invoke();

            // Initialize Data Structures and Seed
            InitializeLabyrinth();

            // Spawn connection zones in random positions.
            // TODO: Later make all zones slightly random.
            SpawnZones();

            // Load all blueprint data and operations into memory
            LoadOperations();

            // Parse the blueprint and generate rooms
            yield return StartCoroutine(ExecuteOperations());

            // Parse the blueprint and generate rooms
            GenerateRooms();

            // Labyrinth Generation Success
            // Event to signal when map generation is complete
            IsGenerating = false;
            OnGenerationDone?.Invoke();
        }

        private void SpawnZones()
        {
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
                    Debug.LogError("[MapGenerator][Controller] Zone Connection cannot fit within the spawning bounds.");
                    return;
                }
            }
        }

        private void LoadOperations()
        {
            // ******* Generate Zone Connection Blueprints *******
            foreach (ZoneConnectionEntry entry in _zoneConnections)
            {
                // TODO: Make a zone connection entry that takes a connection zone and the two zones it connects and
                // generates the connection path and room between them; this will make sure the connection generation
                // has access to all needed data and is generated in the correct order in relation to zone blueprint generation

                LoadConnectionZoneOperations(entry.ConnectionZone, entry.ZoneA, entry.ZoneB);
            }

            // ******* Generate Blueprint Map For Each Zone *******
            // Will generate an entire blueprint for a zone. Generates all paths
            // in zone and makes sure they are contiguous.
            foreach (Zone zone in _zones)
            {
                // Must have a zone to generate anything
                if (zone == null)
                {
                    Debug.LogError("[MapGenerator][Controller] Zone Entry Missing for blueprint procedure.");
                    return;
                }

                // Take the volume of the bounding cubic space and return an error if the amount of rooms to spawn is larger than that volume; make sure we have space for needed rooms
                if (!CheckZoneBoundedVolume(zone))
                {
                    Debug.LogError($"[MapGenerator][Controller] The amount of blueprint rooms desired for zone {zone.Name} exceeds " +
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

        private void ConsumeStep()
        {
            if (!IsGenerating)
                return;

            if (_runToEnd)
                return;

            if (_stepBudget > 0)
                _stepBudget--;
        }

        public void Advance(int stepLength)
        {
            if (!IsGenerating)
                return;

            if (stepLength <= 0)
                return;

            _stepBudget += stepLength;
            _runToEnd = false;
        }

        public void AdvanceAll()
        {
            if (!IsGenerating)
                return;

            _runToEnd = true;
        }

        private IEnumerator ExecuteOperations()
        {
            OnOperationsStarted?.Invoke();
            OnOperationsGetTotal?.Invoke(GetOperationCount());

            // Generate Blueprints
            while (_context.OperationQueue.Count > 0)
            {
                if (DebugSequential)
                {
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
                if (_debugBlueprintLogs) Debug.Log($"[MapGenerator][Controller] Running Operation {operation.OperationID}...");

                bool result = operation.Execute();
                OnOperationExecuted?.Invoke();
                OnOperationsUpdate?.Invoke(GetOperationCount());

                if (_debugBlueprintLogs) Debug.Log(result ? "[MapGenerator][Controller] Execution Success!" :
                    "[MapGenerator][Controller] Execution Failure");

                // Operation failed to execute; stop running generation
                if (!result)
                {
                    GenerationFailed();
                    Debug.LogError("[MapGenerator][Controller] Operation failed to execute. Stopping coroutine.");
                    yield break;
                }
            }

            OnOperationsEnded?.Invoke();

            if (_debugBlueprintLogs) Debug.Log("[MapGenerator][Controller] End of operation execution.");
        }

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
                    Debug.LogError("[MapGenerator][Controller] Rooms failed to generate.");
                    GenerationFailed();
                    return;
                }
            }

            OnRoomParseDone?.Invoke();

            if (_debugBlueprintLogs) Debug.Log("[MapGenerator][Controller] End of room generation.");
        }

        public void ResetLabyrinth()
        {
            if (!Application.isPlaying)     // Only run code when game is executing
                return;

            IsGenerating = false;

            _mapGeneratorCoroutine = null;
            _context.ClearAll();

            _stepBudget = 0;
            _runToEnd = false;

            if (_debugLogs) Debug.Log("[MapGenerator][Controller] Map generation restarting.");

            DestroyAllRooms();      // Destroy all rooms from last generation
        }

        /// <summary>
        /// Resets Labyrinth and retrys generation
        /// </summary>
        private void GenerationFailed()
        {
            IsGenerating = false;

            StopCoroutine(_mapGeneratorCoroutine);
            ResetLabyrinth();

            Debug.LogError("[MapGenerator][Controller] Map generation failed.");
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
                Debug.LogError($"[MapGenerator][Controller] The Main Path for zone {zone.Name} is not assigned.");
                return;
            }

            // Unique Room Placement
            LoadUniqueRoomOperations(zone);

            // Divergent Room Placement
            LoadDivergentRoomOperations(zone);

            // Generate Delauney Triangulation
            LoadMainPathConnectionsOperations(zone);

            if (_debugLogs) Debug.Log($"[MapGenerator][Controller] {zone.Name} generated path {zone.MainPath.name} with {zone.MainPath.BlueprintCount()} rooms.");
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

                    PlaceFixedBlueprintsOp placeFixedBlueprintOp = new PlaceFixedBlueprintsOp(_context, _bpg,
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

                    CheckOutOfBoundsOp adjBoundsBlueprintOp = new CheckOutOfBoundsOp(_context, _bpg,
                        zoneBoundsBlueprintData.OutputPorts[0], roomEntryBlueprintData.OutputPorts[1]);
                    _context.OperationQueueEnqueue(adjBoundsBlueprintOp);

                    PlaceBoundedBlueprintsOp placeBoundedBlueprintsOp = new PlaceBoundedBlueprintsOp(_context, _bpg,
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

                    PlaceBoundedBlueprintsOp placeBoundedBlueprintsOp = new PlaceBoundedBlueprintsOp(_context, _bpg,
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

            DivergentBlueprintsOp divergentRoomsOp = new DivergentBlueprintsOp(_context, _bpg, mainPathBlueprintData.OutputPorts[0], zoneBoundsBlueprintData.OutputPorts[0],
                dimensionsData.OutputPorts[0], cellCountData.OutputPorts[0], maxPlacementAttemptsData.OutputPorts[0]);
            _context.OperationQueueEnqueue(divergentRoomsOp);
        }

        private void LoadMainPathConnectionsOperations(Zone zone)
        {
            // ***** Triangulation
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

            GetAvailableBlueprintsOp availibleBlueprintsOp = new GetAvailableBlueprintsOp(_context, _bpg, mainPathBlueprintData.OutputPorts[2], 
                availableBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(availibleBlueprintsOp);

            // *** Choose Between 2D and 3D Triangulation based on bounds size
            // TODO: DelaunayTriangulation has issues solving cases with coplanar tetrahedra; instead consider
            // a C# library called MIConvexHull that has thousands of lines to solve these issues.
            FindMSTOp mstOp;
            ListDifferenceOp listDiffOp;

            if (zone.Bounds.size.y < 3)
            {
                // **** Perform 2D Triangulation
                TriangulateBlueprints2DOp triangulationOp = new TriangulateBlueprints2DOp(_context, _bpg, availibleBlueprintsOp.OutputPorts[0]);
                _context.OperationQueueEnqueue(triangulationOp);

                mstOp = new FindMSTOp(_context, _bpg, triangulationOp.OutputPorts[0]);
                _context.OperationQueueEnqueue(mstOp);

                listDiffOp = new ListDifferenceOp(_context, _bpg, triangulationOp.OutputPorts[0], mstOp.OutputPorts[0]);
                _context.OperationQueueEnqueue(listDiffOp);
            }
            else
            {
                // **** Perform 3D Triangulation
                TriangulateBlueprints3DOp triangulationOp = new TriangulateBlueprints3DOp(_context, _bpg, availibleBlueprintsOp.OutputPorts[0]);
                _context.OperationQueueEnqueue(triangulationOp);

                mstOp = new FindMSTOp(_context, _bpg, triangulationOp.OutputPorts[0]);
                _context.OperationQueueEnqueue(mstOp);

                listDiffOp = new ListDifferenceOp(_context, _bpg, triangulationOp.OutputPorts[0], mstOp.OutputPorts[0]);
                _context.OperationQueueEnqueue(listDiffOp);
            }

            ListSelectRandomSetFromOp randomCyclesListOp = new ListSelectRandomSetFromOp(_context, _bpg, listDiffOp.OutputPorts[0], 
                setSize.OutputPorts[0]);
            _context.OperationQueueEnqueue(randomCyclesListOp);

            ListUnionOp zoneGraphUnionOp = new ListUnionOp(_context, _bpg, mstOp.OutputPorts[0], randomCyclesListOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(zoneGraphUnionOp);

            // **** Pathfinding
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
            BranchGreaterOrEqualOp bgeOp = new BranchGreaterOrEqualOp(_context, _bpg, targetOpIDBlueprintData.OutputPorts[0], currentIndexBlueprintData.OutputPorts[0],
                zoneGraphUnionOp.OutputPorts[1]);
            _context.OperationQueueEnqueue(bgeOp);

            StringBlueprintData branchIDBlueprintData = new StringBlueprintData(_context, bgeOp.OperationID);
            branchIDBlueprintData.LoadIntoMemory();

            // Pathfinding logic
            AccessListElementOp currentEdgeOp = new AccessListElementOp(_context, _bpg, currentIndexBlueprintData.OutputPorts[0], zoneGraphUnionOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(currentEdgeOp);

            ExtractVerticesFromEdgeOp verticiesFromEdgeOp = new ExtractVerticesFromEdgeOp(_context, _bpg, currentEdgeOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(verticiesFromEdgeOp);

            FindBlueprintFromPositionOp blueprintStart = new FindBlueprintFromPositionOp(_context, _bpg, verticiesFromEdgeOp.OutputPorts[2]);
            _context.OperationQueueEnqueue(blueprintStart);

            FindBlueprintFromPositionOp blueprintEnd = new FindBlueprintFromPositionOp(_context, _bpg, verticiesFromEdgeOp.OutputPorts[3]);
            _context.OperationQueueEnqueue(blueprintEnd);

            GetAvailableBlueprintsOp findObstructionsOp = new GetAvailableBlueprintsOp(_context, _bpg, mainPathBlueprintData.OutputPorts[2], 
                unavailableBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(findObstructionsOp);

            PathfindingBlueprintOp pathFindingOp = new PathfindingBlueprintOp(_context, _bpg, mainPathBlueprintData.OutputPorts[0], blueprintStart.OutputPorts[0], 
                blueprintEnd.OutputPorts[0], zoneBoundsBlueprintData.OutputPorts[0], findObstructionsOp.OutputPorts[0], pathfindingHeuristicData.OutputPorts[0]);
            _context.OperationQueueEnqueue(pathFindingOp);
            // End of Pathfinding logic

            AddIntOp incrementOp = new AddIntOp(_context, _bpg, currentIndexBlueprintData.OutputPorts[0], intOneBlueprintData.OutputPorts[0], 
                currentIndexBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(incrementOp);

            JumpOp jumpOp = new JumpOp(_context, _bpg, branchIDBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(jumpOp);
            // End loop

            NoOp targetIDNoOp = new NoOp(_context, _bpg);                       // Load this operation after loop; jump target for bge
            targetOpIDBlueprintData.ModifyData(targetIDNoOp.OperationID);
            _context.OperationQueueEnqueue(targetIDNoOp);
        }

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
                    Debug.LogError($"[MapGenerator][Controller] A path {path.Name} for zone {zone.name} is not assigned.");
                    return;
                }

                // Initialize path
                path.Initialize();

                PathBlueprintData pathBlueprintData = new PathBlueprintData(_context, path);
                pathBlueprintData.LoadIntoMemory();

                BoolBlueprintData canGoVerticalData = new BoolBlueprintData(_context, path.DrunkardWalkCanGoVertical);
                canGoVerticalData.LoadIntoMemory();

                GetPathLengthOp branchedpathLengthOp = new GetPathLengthOp(_context, _bpg, branchedPathBlueprintData.OutputPorts[0]);
                _context.OperationQueueEnqueue(branchedpathLengthOp);

                AddIntOp lengthMinusOneOp = new AddIntOp(_context, _bpg, branchedpathLengthOp.OutputPorts[0], negativeOneBlueprintData.OutputPorts[0]);
                _context.OperationQueueEnqueue(lengthMinusOneOp);

                DrunkardWalkBlueprintOp drunkardWalkOperation = new DrunkardWalkBlueprintOp(_context, _bpg, pathBlueprintData.OutputPorts[0], branchedPathBlueprintData.OutputPorts[0],
                    boundsBlueprintData.OutputPorts[0], startIndexBlueprintData.OutputPorts[0], lengthMinusOneOp.OutputPorts[0], canGoVerticalData.OutputPorts[0]);
                _context.OperationQueueEnqueue(drunkardWalkOperation);
            }
        }

        public void LoadConnectionZoneOperations(Zone connectionZone, Zone zoneA, Zone zoneB)
        {
            // *** Error Handleing ***
            if (connectionZone == null)
            {
                Debug.LogError("[MapGenerator][Controller] Connection Zone of zone connection was null.");
                return;
            }
            if (zoneA == null)
            {
                Debug.LogError("[MapGenerator][Controller] Zone A of zone connection was null.");
                return;
            }
            if (zoneB == null)
            {
                Debug.LogError("[MapGenerator][Controller] zone B of zone connection was null.");
                return;
            }
            if (connectionZone.MainPath == null)
            {
                Debug.LogError($"[MapGenerator][Controller] The Main Path for connection zone {connectionZone.Name} is not assigned.");
                return;
            }
            if (connectionZone.UniqueRooms.Count < 2)
            {
                Debug.LogError($"[MapGenerator][Controller] Connection zone {connectionZone.Name} must have at least two unique rooms assigned for zone connection.");
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
            BoundsIntersectOp zoneConnectzoneAIntersectOp = new BoundsIntersectOp(_context, _bpg, zoneABoundsBlueprintData.OutputPorts[0], 
                zoneConnectionBoundsBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(zoneConnectzoneAIntersectOp);
            // Place Room A in Zone A
            PlaceBoundedBlueprintsOp placeRoomAOp = new PlaceBoundedBlueprintsOp(_context, _bpg, zoneAMainPathBlueprintData.OutputPorts[0], roomABlueprintData.OutputPorts[0],
                zoneConnectzoneAIntersectOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(placeRoomAOp);

            // **** Room B Placement Operations ***
            // Find the bounds area intersection of the two zones to find where the connection can be placed; this will be used for placing the room and path of the zone connection
            BoundsIntersectOp zoneConnectzoneBIntersectOp = new BoundsIntersectOp(_context, _bpg, zoneConnectionBoundsBlueprintData.OutputPorts[0],
                zoneBBoundsBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(zoneConnectzoneBIntersectOp);
            // Place Room B in Zone B
            PlaceBoundedBlueprintsOp placeRoomBOp = new PlaceBoundedBlueprintsOp(_context, _bpg, zoneBMainPathBlueprintData.OutputPorts[0], roomBBlueprintData.OutputPorts[0],
                zoneConnectzoneBIntersectOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(placeRoomBOp);

            // *** PathFind Operations ***
            // Initialize pathfinding data
            BoolBlueprintData availableBlueprintData = new BoolBlueprintData(_context, true);
            availableBlueprintData.LoadIntoMemory();
            StringBlueprintData blueprintTypeBlueprintData = new StringBlueprintData(_context, "Blueprint");
            blueprintTypeBlueprintData.LoadIntoMemory();

            // Select random available blueprints from each room to be the start and end points for pathfinding
            GetAvailableBlueprintsOp roomAAvailableBlueprintsOp = new GetAvailableBlueprintsOp(_context, _bpg, placeRoomAOp.OutputPorts[0], availableBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(roomAAvailableBlueprintsOp);
            GetAvailableBlueprintsOp roomBAvailableBlueprintsOp = new GetAvailableBlueprintsOp(_context, _bpg, placeRoomBOp.OutputPorts[0], availableBlueprintData.OutputPorts[0]);
            _context.OperationQueueEnqueue(roomBAvailableBlueprintsOp);
            ListSelectRandomElementOp randomBlueprintFromRoomAOp = new ListSelectRandomElementOp(_context, _bpg, roomAAvailableBlueprintsOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(randomBlueprintFromRoomAOp);
            ListSelectRandomElementOp randomBlueprintFromRoomBOp = new ListSelectRandomElementOp(_context, _bpg, roomBAvailableBlueprintsOp.OutputPorts[0]);
            _context.OperationQueueEnqueue(randomBlueprintFromRoomBOp);

            // Fill obstructions list with all blueprints from the main paths of both zones except the ones that are part of the roomA and roomB
            ListUnionOp obstructionsListOp = new ListUnionOp(_context, _bpg, zoneAMainPathBlueprintData.OutputPorts[2], zoneBMainPathBlueprintData.OutputPorts[2]);
            _context.OperationQueueEnqueue(obstructionsListOp);

            // Pathfind a connection between the two rooms along the bounds intersection area while avoiding main path blueprints
            PathfindingBlueprintOp pathfindOp = new PathfindingBlueprintOp(_context, _bpg, zoneConnectionMainPathBlueprintData.OutputPorts[0], randomBlueprintFromRoomAOp.OutputPorts[0],
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
                Debug.LogError($"[MapGenerator][Controller] Zone Entry Missing for room generation procedure.");
                return false;
            }

            // Generate Unique Rooms
            bool result;
            result = GenerateUniqueRooms(zone);
            if (!result)
            {
                Debug.LogError($"[MapGenerator][Controller] Unique Room Generation for zone {zone} failed.");
                return false;
            }

            // Turn off blueprint availability for unique rooms; we do not want to parse and spawn new rooms in these spots
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                if (entry.Prefab.TryGetComponent(out Room room))
                    _bpg.ToggleAvailableCellsInUniqueRoom(zone.MainPath, room.AvailableCellData, entry.SpawnPosition, false);
                else
                    Debug.LogError($"[MapGenerator][Controller] Failed to get Room component from prefab {entry.Prefab}.");

                if (_debugRoomGeneratorLogs) Debug.Log("[MapGeneratorController] Blueprint Room: " + entry.SpawnPosition + "has available cells disabled.");
            }

            // Generate Rooms along main path
            result = _roomGenerator.ParsePathAndGenerateRooms(zone.MainPath);
            if (!result)
            {
                Debug.LogError($"[MapGenerator][Controller] Path Room Generation for path {zone.MainPath} in zone {zone} failed.");
                return false;
            }

            // Generate Rooms along alt. paths
            foreach (Path path in zone.Paths)
            {
                result = _roomGenerator.ParsePathAndGenerateRooms(path);
                if (!result)
                {
                    Debug.LogError($"[MapGenerator][Controller] Path Room Generation for path {path} in zone {zone} failed.");
                    return false;
                }
            }
            return true;
        }

        public bool GenerateUniqueRooms(Zone zone)
        {
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                if (_context.MasterPath == null || _context.MasterDictionary == null)
                {
                    Debug.Log("[MapGenerator][Controller] Masters are null.");
                    return false;
                }

                // TODO: This needs to be changed to a more universal solution.
                Vector3Int actualPosition = entry.SpawnPosition;
                if (entry.PlacementType == RoomPlacementType.Fixed)
                    actualPosition += zone.Bounds.position;

                Room generatedRoom = _roomGenerator.GenerateRoom(entry.Prefab, actualPosition, zone.MainPath);

                // TODO: Make this into a new function in the room generator. Make the function check for all rooms inside
                // the unique room.
                // Unique rooms with available cells
                if (generatedRoom.AvailableCellData != null)
                {
                    for (int i = 0; i < generatedRoom.AvailableCellData.Count; i++)
                    {
                        if (_context.MasterDictionary.TryGetValue(actualPosition + generatedRoom.AvailableCellData[i], out Blueprint blueprint))
                        {
                            generatedRoom.CopyBlueprintEntranceFlags(blueprint.EntryPointFlags, i, Vector3.zero);
                        }
                        else
                        {
                            Debug.LogError($"[MapGenerator][Controller] Could not copy entranceway flags into unique room {entry}.");
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
                    Debug.LogWarning("[MapGenerator][Controller] Room Entry Prefab has no Room Script");
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

        public void DestroyAllRooms()
        {
            foreach (Transform child in _roomContainer.transform)
                Destroy(child.gameObject);
        }

        public int GetOperationCount()
        {
            if (_context == null)
            {
                Debug.LogError("[MapGenerator][Controller] Context is not assigned.");
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

            // This no longer works as BlueprintData is no longer static
            // BlueprintData.ToggleDebugLogs(toggle);

            if (_bpg == null)
                return;

            _bpg.ToggleDebugLogs(_debugBlueprintLogs);
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
            DebugSequential = toggle;
        }
        #endregion

        #region Console Commands
        private void RegisterConsoleCommands()
        {
            // Map generator step command - Step the map generator by a desired amount of operations.
            ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
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
                            Debug.LogWarning($"[Console] Invalid argument '{args[0]}'. Please enter a positive amount of steps to advance.");
                            ConsoleUI.OnNewConsoleOutput($"Invalid argument '{args[0]}'. " +
                                $"Please enter a positive amount of steps to advance.", LogType.Warning);
                            return;
                        }

                        Advance(stepLength);
                    }
                    else
                    {
                        Debug.LogWarning($"[Console] Invalid argument '{args[0]}'. Please enter a positive amount of steps to advance.");
                        ConsoleUI.OnNewConsoleOutput($"Invalid argument '{args[0]}'. " +
                            $"Please enter a positive amount of steps to advance.", LogType.Warning);
                    }
                    Debug.Log($"[MapGenerator][Controller] Map generator stepped {stepLength} operation(s).");
                    ConsoleUI.OnNewConsoleOutput($"Map generator stepped {stepLength} operation(s).", LogType.Log);
                }));

            // Map generator step all command - Execute all of the remaining map generator operations.
            ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.stepall",
                "When debugging, will execute all the remaining operations in the operation queue.",
                args =>
                {
                    AdvanceAll();
                    Debug.Log("[MapGenerator][Controller] Map Generator executed all remaining operations.");
                    ConsoleUI.OnNewConsoleOutput("Map Generator executed all remaining operations.", LogType.Log);
                }));

            // Map generator reset command - Resets and restarts the map generator state.
            ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.reset",
                "When debugging, will reset the map generator and start a new generation.",
                args =>
                {
                    ResetLabyrinth();
                    ApplicationController.Instance.StartNewGame();      // DELETE AFTER DEMO
                    Debug.Log("[MapGenerator][Controller] All data deleted. Map Generator restarted.");
                    ConsoleUI.OnNewConsoleOutput("All data deleted. Map Generator restarted.", LogType.Log);
                }));

            // Register custom seed command - Sets the map generator to use a custom seed for generation.
            ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.setseed",
                "Sets the map generator to use a custom seed for generation.",
                args =>
                {
                    int seed = 0;

                    if (args.Length < 1)
                    {
                        Debug.LogWarning("[Console] No argument given, please enter a valid seed value between " + int.MinValue + " and " + int.MaxValue + ".");
                        ConsoleUI.OnNewConsoleOutput($"No argument given, please enter a valid seed value " +
                            $"between {int.MinValue} and {int.MaxValue}.", LogType.Warning);
                        return;
                    }
                    else if (!int.TryParse(args[0], out seed))
                    {
                        Debug.LogWarning("[Console] Invalid argument given, please enter a valid seed value between " + int.MinValue + " and " + int.MaxValue + ".");
                        ConsoleUI.OnNewConsoleOutput($"Invalid argument given, please enter a valid seed value " +
                            $"between {int.MinValue} and {int.MaxValue}.", LogType.Warning);
                        return;
                    }

                    _customSeed = seed;

                    OnSeedUpdate?.Invoke();
                    Debug.Log($"[MapGenerator][Controller] Map Generator seed set to {seed}.");
                    ConsoleUI.OnNewConsoleOutput($"Map Generator seed set to {seed}.", LogType.Log);
                }
                ));

            // Map generator restart command - Resets and restarts the map generator state.
            ConsoleUI.CommandRegistry.RegisterCommand(new ConsoleCommand(
                "mapgenerator.togglerandomseed",
                "Toggles the use of a random seed for map generation.",
                args =>
                {
                    if (args.Length < 1)
                    {
                        Debug.LogWarning("[Console] No argument given, please enter true or false.");
                        ConsoleUI.OnNewConsoleOutput("No argument given, please enter true or false.", LogType.Warning);
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
                        Debug.LogWarning($"[Console] Invalid argument '{args[0]}'. Please input either true or false.");
                        ConsoleUI.OnNewConsoleOutput($"Invalid argument '{args[0]}'. Please input either true or false.", LogType.Warning);
                    }

                    OnSeedUpdate?.Invoke();
                    Debug.Log($"[MapGenerator][Controller] Map Generator toggle random seed set to {_generateRandomSeed}.");
                    ConsoleUI.OnNewConsoleOutput($"Map Generator toggle random seed set to {_generateRandomSeed}.", LogType.Log);
                }));
        }
        #endregion
    }
}
