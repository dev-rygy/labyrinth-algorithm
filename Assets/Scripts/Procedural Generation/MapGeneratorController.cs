/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.AI;
using RyansLibrary.Geometry;
using RyansLibrary.Graphs;
using RyansLibrary.UnityEditor;
using RyansLibrary.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;      // Use Unity Engine's Random not System.Collection's Random

namespace RyansLibrary.Labyrinth
{
    public class MapGeneratorController : MonoBehaviour
    {
        #region Variables
        // ***** CONSTANTS *****
        const string MASTER_PATH_NAME = "Master Path";

        // ***** Singleton Reference *****
        public static MapGeneratorController Instance { get; private set; }

        // ***** Events *****
        public static event Action OnGenerationStarted;
        public static event Action OnGenerationDone;
        public static event Action OnGenerationFailed;

        // ***** Path Containers *****
        // The Master Path holds a reference to all bluprint rooms in an zone
        public Path MasterPath { get; private set; }

        // Dictionary used for quick access like checking locations for conflicts and checking locations for room shape conditions
        // Keys are in room coords
        public Dictionary<Vector3Int, Blueprint> MasterDictionary { get; private set; }

        public bool IsGenerating { get; private set; }

        // ***** Inspector Values *****
        [Tooltip("Enables map generation.")]
        [SerializeField] private bool _enabled = true;

        [Header("Seed")]
        [SerializeField] private int customSeed = 0;
        [SerializeField] private bool generateRandomSeed = true;
        [SerializeField, ReadOnly] private int _seed = 0;

        [Header("Global Settings")]
        [Tooltip("The size of a room unit or how large a 1x1 room is in Unity units.")]
        [SerializeField] private int _gridUnitSize = 13;                        // The unit size of the room grid's cell
        [SerializeField] private Transform _roomContainer;                      // Parent transform that will contain all the spawned rooms
        [SerializeField] private bool _retryGenerationOnFail;

        [Header("Blueprint Settings")]
        [SerializeField] private int _maxPlacementAttempts = 50;

        [Header("Zones")]
        [SerializeField] private List<Zone> _zones;

        // Entrys to connect zones together
        [Header("Zone Connection")]
        [SerializeField] private List<ZoneConnectionEntry> _zoneConnections;

        [Header("Debugging")]
        [Space]
        [SerializeField] private Color _boundingBoxColor;
        [SerializeField] private Color _triangulationColor;
        // [SerializeField] private Color _circumcircleColor;   DEPRICATED
        [SerializeField] private Color _contiguousGraphColor;
        [SerializeField] private Color _randomCyclesColor;
        [SerializeField] private Color _currentEdgeColor;

        // ***** Private Variables *****
        // private int _seed;      // TODO: For networking make the host generate this

        private BlueprintGenerator _bpg;
        private RoomGenerator _roomGenerator;

        // Debugging
        private bool _debug = false;

        // Logs
        private bool _debugLogs = false;
        private bool _debugBlueprintLogs = false;
        private bool _debugRoomGeneratorLogs = false;

        // Gizmos
        private bool _debugGizmos = false;
        private bool _debugBlueprintGizmos = false;
        private bool _debugTriangulationGizmos = false;
        private bool _debugBoundsGizmos = false;

        private Queue<BlueprintOperation> operationQueue;
        private Stack<BlueprintOperation> operationHistory;

        private MapGenerationContext _context;
        #endregion

        private void LoadOperations()
        {
            // Do not Generate a labyrinth if one is already generating
            if (IsGenerating)
                return;

            IsGenerating = true;

            // Event to signal when map generation has begun
            OnGenerationStarted?.Invoke();

            // Initialize Data Structures and Seed
            InitializeLabyrinth();

            // ******* Generate Blueprints *******
            // Generate Zone Connection Paths
            /*
            foreach (ZoneConnectionEntry entry in _zoneConnections)
            {
                // TODO: Option 1: Handle this after both zone A's and B's blueprints have been generated.
                // This is only needed here because of triangulation but can be handled with
                // an extra step of finding the shortest path to a room
                // TODO: Option 2: Connect blueprint with a unique room entry assiciated with the zone so
                // that the room can still be a part of triangulation and the pathfinding occurs after 
                // the zone's generation
                if (!GenerateZoneConnectionBlueprints(entry))
                {
                    GenerationFailed();
                    return;     // Blueprint failed for zone connection; stop algorithm
                }
            }
            */
            // Generate Blueprint Map For Each Zone
            //foreach (Zone zone in _zones)
            //{
            LoadZoneBlueprints(_zones[0]);
            //}

            /*
            // ******* Parse and Generate Rooms *******
            // TODO: Possibly do this dynamically as players move around the map
            // Generate Zone Connection Rooms
            foreach (ZoneConnectionEntry entry in _zoneConnections)
            {
                // Generate actual rooms for the zone connection
                if (!GenerateZoneConnectionRooms(entry))
                {
                    GenerationFailed();
                    return;     // Room Generation failed for zone connection; stop algorithm
                }
            }
            */

            /*
            // Spawn rooms based on the blueprint map for each zone
            foreach (Zone zone in _zones)
            {
                // Check room conditions and generate rooms using the blueprint map of the zone
                if (!GenerateZoneRooms(zone))
                {
                    GenerationFailed();
                    return;     // Room Generation failed for zone; stop algorithm
                }

                // TODO: Implement perlin noise height Map
            }
            */
            IsGenerating = false;

            // Labyrinth Generation Success
            // Event to signal when map generation is complete
            OnGenerationDone?.Invoke();
        }

        private IEnumerator ExecuteOperations()
        {
            while (operationQueue.Count > 0)
            {
                yield return new WaitForSeconds(1.5f);

                BlueprintOperation op = operationQueue.Peek();
                Debug.Log($"Running Operation {op.OperationID}");
                bool result = op.Execute();

                if (result)
                {
                    Debug.Log("Execution Successs!");
                    operationQueue.Dequeue();
                    operationHistory.Push(op);
                }
                else
                    Debug.Log("Execution Failure. Retrying...");
                
            }

            Debug.Log("End of execution.");
        }

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

            gameObject.transform.parent = null;     // Parent must be cleared to be DNDOL
            DontDestroyOnLoad(gameObject);  // Have this gameObject persist
        }

        private void Start()
        {
            operationQueue = new();
            operationHistory = new();

            _context = new();
            _bpg = new(MasterPath, MasterDictionary);

            StartGeneration();
        }


        public void StartGeneration()
        {
            // Return if the Map Generator is not enabled
            if (!_enabled)
                return;

            // TODO: Enable debug in editor script when stepwise is being worked on
            // If debug is active; step through procedures
            if (_debug)
            {
                Debug.Log("Map Generator: Debug On");
                return;
            }
            else
                Debug.Log("Map Generator: Debug Off");

            try
            {
                // Generate Labyrinth Blueprint and Rooms
                LoadOperations();
                StartCoroutine(ExecuteOperations());
            }
            catch (Exception e)
            {
                Debug.LogError($"Map Generator Error: Failed to generate labyrinth: {e.Message}");
            }

        }
        #endregion

        #region Labyrinth Algorithm Sequence
        #region Labyrinth Init Functions
        private void InitializeLabyrinth()
        {
            // Handle Map Seed Generation
            if (generateRandomSeed)
                _seed = Random.Range(int.MinValue, int.MaxValue);                  // Generate with random seed
            else
                _seed = customSeed;         // Generate with custom seed

            Random.InitState(_seed);

            if (_debugLogs)
                Debug.Log($"Generating map with seed: {_seed}");

            // Initialize Master Data Structures
            InitializeMasters();

            // Initialize Blueprint Generator
            _bpg = new BlueprintGenerator(MasterPath, MasterDictionary);
            _bpg.ToggleDebugLogs(_debugBlueprintLogs);

            // Initialize Room Generator
            _roomGenerator = new RoomGenerator(MasterPath, MasterDictionary, _gridUnitSize, _roomContainer);
            _roomGenerator.ToggleDebugLogs(_debugRoomGeneratorLogs);

            // Initialize the Main Path in each Zone
            foreach (Zone zone in _zones)
                InitializeZone(zone);
        }

        public void InitializeMasters()     // NOTE: This must be done before generating anything!
        {
            // Initialize Master Data Structures
            MasterDictionary = new Dictionary<Vector3Int, Blueprint>();
            MasterPath = ScriptableObject.CreateInstance<Path>();
            MasterPath.Initialize();
            MasterPath.Name = MASTER_PATH_NAME;
        }

        private void InitializeZone(Zone zone)
        {
            zone.MainPath.Initialize();     // Must be done before zone connection blueprints
        }
        #endregion

        // Only to be used in the inspector
        public void ResetLabyrinth()
        {
            if (!Application.isPlaying)     // Only run code when game is executing
                return;

            DestroyAllRooms();      // Destroy all rooms from last generation
            ScenesManager.Instance.ReloadScene();       // Reload to reset data
            StartGeneration();
        }

        /// <summary>
        /// Resets Labyrinth and retrys generation
        /// </summary>
        private void GenerationFailed()
        {
            IsGenerating = false;

            Debug.LogWarning("Map Generator Warning: Map generation failed");
            OnGenerationFailed?.Invoke();

            if (!_retryGenerationOnFail)
                return;

            Instance.DestroyAllRooms();

            // TODO: Delete after demo
            ApplicationController.Instance.StartNewGame();
        }
        #endregion

        #region Blueprint Procedure
        /// <summary>
        /// Will generate an entire blueprint for a zone. Generates all paths
        /// in zone and makes sure they are contiguous.
        /// </summary>
        /// <returns>Generation Success or Failure</returns>
        public void LoadZoneBlueprints(Zone zone)
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
                Debug.LogError($"Map Generator Error: The amount of blueprint rooms desired for zone {zone.Name} exceeds " +
                    $"the bounding box's volume or the bounding box is inverted.");
                return;
            }

            // ******* Generate Zone Blueprints *******
            // Generate Main Path to boss
            LoadMainPathOperations(zone);

            /*
            // Generate Alternative paths (prize, trial, etc.)
            if (!LoadAltPathOperations(zone))
            {
                Debug.LogWarning($"Map Generator Warning: Alt. Path Generation for {zone.Name} zone failed.");
                return false;
            }
            */
        }

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
                Debug.LogError($"Map Generator Error: The Main Path for zone {zone.Name} is not assigned.");
                return;
            }

            // Unique Room Placement
            LoadUniqueRoomOperations(zone);

            // Divergent Room Placement
            LoadDivergentRoomOperations(zone);

            // Generate Delauney Triangulation
            ConnectMainPathOperations(zone);

            if (_debugLogs) Debug.Log($"Map Generator: {zone.Name} generated path {zone.MainPath.name} with {zone.MainPath.BlueprintCount()} rooms.");
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

                    FixedUniqueBlueprintsOp placeFixedBlueprintOp = new FixedUniqueBlueprintsOp(_context, _bpg,
                        mainPathBlueprintData.OutputPorts[0], roomEntryBlueprintData.OutputPorts[0], zoneBoundsBlueprintData.OutputPorts[0]);
                    operationQueue.Enqueue(placeFixedBlueprintOp);
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

                    IntersectingBoundsOp adjBoundsBlueprintOp = new IntersectingBoundsOp(_context, _bpg,
                        zoneBoundsBlueprintData.OutputPorts[0], roomEntryBlueprintData.OutputPorts[1]);
                    operationQueue.Enqueue(adjBoundsBlueprintOp);

                    BoundedUniqueBlueprintsOp placeBoundedBlueprintsOp = new BoundedUniqueBlueprintsOp(_context, _bpg,
                        mainPathBlueprintData.OutputPorts[0], roomEntryBlueprintData.OutputPorts[0], adjBoundsBlueprintOp.OutputPorts[0]);
                    operationQueue.Enqueue(placeBoundedBlueprintsOp);
                }
            }

            // 3.) Spawn Free Rooms (Rooms that can spawn in any point inside the zone bounds)
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                if (entry.PlacementType == RoomPlacementType.Free)
                {
                    RoomEntryBlueprintData roomEntryBlueprintData = new RoomEntryBlueprintData(_context, entry);
                    roomEntryBlueprintData.LoadIntoMemory();

                    BoundedUniqueBlueprintsOp placeBoundedBlueprintsOp = new BoundedUniqueBlueprintsOp(_context, _bpg,
                        mainPathBlueprintData.OutputPorts[0], roomEntryBlueprintData.OutputPorts[0], zoneBoundsBlueprintData.OutputPorts[0]);
                    operationQueue.Enqueue(placeBoundedBlueprintsOp);
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
            operationQueue.Enqueue(divergentRoomsOp);
        }

        private void ConnectMainPathOperations(Zone zone)
        {
            PathBlueprintData mainPathBlueprintData = new PathBlueprintData(_context, zone.MainPath);
            mainPathBlueprintData.LoadIntoMemory();
            BoolBlueprintData availibilityBlueprintData = new BoolBlueprintData(_context, true);
            availibilityBlueprintData.LoadIntoMemory();
            StringBlueprintData edgeTypeBlueprintData = new StringBlueprintData(_context, "Edge");
            edgeTypeBlueprintData.LoadIntoMemory();
            IntBlueprintData elementCountBlueprintData = new IntBlueprintData(_context, zone.RandomCyclesInGraph);
            elementCountBlueprintData.LoadIntoMemory();

            GetAvailableBlueprintsOp availibleBlueprintsOp = new GetAvailableBlueprintsOp(_context, _bpg, mainPathBlueprintData.OutputPorts[0], 
                availibilityBlueprintData.OutputPorts[0]);
            operationQueue.Enqueue(availibleBlueprintsOp);

            TriangulateBlueprintsOp triangulationOp = new TriangulateBlueprintsOp(_context, _bpg, availibleBlueprintsOp.OutputPorts[0]);
            operationQueue.Enqueue(triangulationOp);

            FindMSTOp mstOp = new FindMSTOp(_context, _bpg, triangulationOp.OutputPorts[0]);
            operationQueue.Enqueue(mstOp);

            ListDifferenceOp listDiffOp = new ListDifferenceOp(_context, _bpg, triangulationOp.OutputPorts[0], mstOp.OutputPorts[0], edgeTypeBlueprintData.OutputPorts[0]);
            operationQueue.Enqueue(listDiffOp);

            SelectRandomSetFromListOp randomCyclesListOp = new SelectRandomSetFromListOp(_context, _bpg, listDiffOp.OutputPorts[0], 
                elementCountBlueprintData.OutputPorts[0], edgeTypeBlueprintData.OutputPorts[0]);
            operationQueue.Enqueue(randomCyclesListOp);

            ListUnionOp zoneGraphUnionOp = new ListUnionOp(_context, _bpg, mstOp.OutputPorts[0], randomCyclesListOp.OutputPorts[0], edgeTypeBlueprintData.OutputPorts[0]);
            operationQueue.Enqueue(zoneGraphUnionOp);
        }
        #endregion

        #region Utility
        /// <summary>
        /// Checks if the total amount of disired rooms in zone is valid in an zone's bounded range.
        /// </summary>
        /// <returns>The test success or fail</returns>
        private bool CheckZoneBoundedVolume(Zone zone)
        {
            float totalCellOccupancy = 0;

            // Add Unique Room volume
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                if (entry.Prefab.TryGetComponent<Room>(out Room room))
                {
                    totalCellOccupancy += room.GetRoomOccupancy();
                }
                else
                    Debug.LogWarning("Map Generator Warning: Room Entry Prefab has no Room Script");
            }

            // Add Divergent Room volume
            totalCellOccupancy += zone.DivergentRoomsCellOccupancy;

            // Add Main Path volume
            totalCellOccupancy += zone.MainPath.PathLength;

            // Add Alt. Paths volume
            foreach (Path path in zone.Paths)
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

        public void ToggleBlueprintLogs(bool toggle)
        {
            _debugBlueprintLogs = toggle;
        }

        public void ToggleRoomGeneratorLogs(bool toggle)
        {
            _debugRoomGeneratorLogs = toggle;
        }

        // Gizmo Toggles
        public void ToggleGizmos(bool toggle)
        {
            _debugGizmos = toggle;
        }

        public void ToggleBlueprintGizmos(bool toggle)
        {
            _debugBlueprintGizmos = toggle;
        }

        public void ToggleTriangulationGizmos(bool toggle)
        {
            _debugTriangulationGizmos = toggle;
        }

        public void ToggleBoundsGizmos(bool toggle)
        {
            _debugBoundsGizmos = toggle;
        }

        private void OnDrawGizmos()
        {
            if (!_debugGizmos)
                return;

            foreach (Zone zone in _zones)
            {
                if (_debugBlueprintGizmos)
                    DrawBluePrintGizmos(zone);

                if (_debugTriangulationGizmos)
                {
                    DrawTriangulation();
                    DrawMSTs();
                    DrawRandomCycles();
                }

                if (_debugBoundsGizmos)
                    DrawBoundingBox(zone.Bounds);
            }

            foreach (ZoneConnectionEntry entry in _zoneConnections)
            {
                if (_debugBlueprintGizmos)
                    DrawBluePrintGizmos(entry.ConnectionPath);
            }
        }

        private void DrawBoundingBox(BoundsInt bounds)
        {
            Vector3 boundsSize = _gridUnitSize * (bounds.size + Vector3Int.one);
            Vector3 boundsCenter = bounds.center * _gridUnitSize;

            Gizmos.color = _boundingBoxColor;
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }

        private void DrawTriangulation()
        {
            if (_context.Triangulations is null)
                return;

            foreach (List<Edge> edgeList in _context.Triangulations)
            {
                // Draw remaining edges from triangulation
                foreach (Edge e in edgeList)
                {
                    Gizmos.color = _triangulationColor;
                    Gizmos.DrawLine(e.V.Position * _gridUnitSize, e.U.Position * _gridUnitSize);
                }
            }
        }

        private void DrawMSTs()
        {
            if (_context.MinimumSpanningTrees is null)
                return;

            foreach (List<Edge> edgeList in _context.MinimumSpanningTrees)
            {
                if (edgeList is null)
                    continue;

                // Draw the minimum spanning tree of the zone
                foreach (Edge e in edgeList)
                {
                    Gizmos.color = _contiguousGraphColor;
                    Gizmos.DrawLine(e.V.Position * _gridUnitSize, e.U.Position * _gridUnitSize);
                }
            }
        }

        private void DrawRandomCycles()
        {
            if (_context.RandomCycles is null) 
                return;

            foreach (List<Edge> edgeList in _context.RandomCycles)
            {
                if (edgeList is null)
                    continue;

                foreach (Edge e in edgeList)
                {
                    Gizmos.color = _randomCyclesColor;
                    Gizmos.DrawLine(e.V.Position * _gridUnitSize, e.U.Position * _gridUnitSize);
                }
            }
        }

        private void DrawBluePrintGizmos(Zone zone)
        {
            if (zone.MainPath.BlueprintList == null)
                return;

            Vector3 unitSize = Vector3.one * _gridUnitSize;

            // Draw Gizmos for main path
            foreach (Blueprint blueprint in zone.MainPath.BlueprintList)
            {
                Gizmos.color = zone.MainPath.PathGizmoColor;
                Gizmos.DrawCube(blueprint.Position * _gridUnitSize, unitSize);
            }

            foreach (Path path in zone.Paths)
            {
                if (path.BlueprintList == null)
                    return;

                // Draw Gizmos for alt paths
                foreach (Blueprint blueprint in path.BlueprintList)
                {
                    Gizmos.color = path.PathGizmoColor;
                    Gizmos.DrawCube(blueprint.Position * _gridUnitSize, unitSize);
                }
            }
        }

        private void DrawBluePrintGizmos(Path path)
        {
            if (path.BlueprintList == null)
                return;

            Vector3 unitSize = Vector3.one * _gridUnitSize;

            // Draw Gizmos for main path
            foreach (Blueprint blueprint in path.BlueprintList)
            {
                Gizmos.color = path.PathGizmoColor;
                Gizmos.DrawCube(blueprint.Position * _gridUnitSize, unitSize);
            }
        }
        #endregion
    }
}
