/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.AI;
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

        private BlueprintGenerator _blueprintGenerator;
        private RoomGenerator _roomGenerator;

        // Debugging
        private bool _debug = false;
        private List<List<Edge>> _triangulations;
        private List<List<Edge>> _minimumSpanningTrees;
        private List<List<Edge>> _randomCycles;
        private Edge _currentEdge;

        // Logs
        private bool _debugLogs = false;
        private bool _debugBlueprintLogs = false;
        private bool _debugRoomGeneratorLogs = false;

        // Gizmos
        private bool _debugGizmos = false;
        private bool _debugBlueprintGizmos = false;
        private bool _debugTriangulationGizmos = false;
        private bool _debugBoundsGizmos = false;
        #endregion

        private Queue<BlueprintOperation> operationQueue;
        private Stack<BlueprintOperation> operationHistory;

        private MapGenerationContext context;

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
            foreach (Zone zone in _zones)
            {
                if (!GenerateZoneBlueprints(zone))
                {
                    GenerationFailed();
                    return;     // Blueprint failed for zone; stop algorithm
                }
            }

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
                yield return new WaitForSeconds(1f);

                BlueprintOperation op = operationQueue.Dequeue();
                bool result = op.Execute();

                if (result)
                    Debug.Log("Execution Successs!");
                else
                    Debug.Log("Execution Failure.");
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

            context = new();
            _blueprintGenerator = new(MasterPath, MasterDictionary);

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
            _blueprintGenerator = new BlueprintGenerator(MasterPath, MasterDictionary);
            _blueprintGenerator.ToggleDebugLogs(_debugBlueprintLogs);

            // Initialize Room Generator
            _roomGenerator = new RoomGenerator(MasterPath, MasterDictionary, _gridUnitSize, _roomContainer);
            _roomGenerator.ToggleDebugLogs(_debugRoomGeneratorLogs);

            // Initialize Debugging Lists
            _triangulations = new();
            _minimumSpanningTrees = new();
            _randomCycles = new();

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
        public bool GenerateZoneBlueprints(Zone zone)
        {
            // Must have a zone to generate anything
            if (zone == null)
            {
                Debug.LogError("Map Generator Error: Zone Entry Missing for blueprint procedure.");
                return false;
            }

            // Take the volume of the bounding cubic space and return an error if the amount of rooms to spawn is larger than that volume; make sure we have space for needed rooms
            if (!CheckZoneBoundedVolume(zone))
            {
                Debug.LogError($"Map Generator Error: The amount of blueprint rooms desired for zone {zone.Name} exceeds " +
                    $"the bounding box's volume or the bounding box is inverted.");
                return false;
            }

            // ******* Generate Zone Blueprints *******
            // Generate Main Path to boss
            if (!LoadMainPathOperations(zone))
            {
                Debug.LogError($"Map Generator Error: Main Path Generation for {zone.Name} zone failed.");
                return false;
            }

            /*
            // Generate Alternative paths (prize, trial, etc.)
            if (!LoadAltPathOperations(zone))
            {
                Debug.LogWarning($"Map Generator Warning: Alt. Path Generation for {zone.Name} zone failed.");
                return false;
            }
            */
            return true;
        }

        /// <summary>
        /// Wrapper function for generating the Main Path. 
        /// A path that leads to the zone boss, alternative paths, and 
        /// entrances to other zones.
        /// </summary>
        /// <param name="zone">Zone who's Main Path to generate.</param>
        /// <returns>Generation success or failure</returns>
        public bool LoadMainPathOperations(Zone zone)
        {
            if (zone.MainPath == null)      // Throw error if MainPath for zone does not exist
            {
                Debug.LogError($"Map Generator Error: The Main Path for zone {zone.Name} is not assigned.");
                return false;
            }

            // Unique Room Placement
            if (!LoadUniqueRoomOperations(zone))
            {
                Debug.LogError($"Map Generator Error: Placing Unique Rooms failed in {zone.Name} zone.");
                return false;
            }

            /*
            // Divergent Room Placement
            if (!PlaceDivergentRooms(zone))
            {
                Debug.LogError($"Map Generator Error: Placing Divergent Rooms failed in {zone.Name} zone.");
                return false;
            }

            // Generate Delauney Triangulation
            List<Edge> zoneGraph = GenerateContigiousTriangulation(zone);
            if (zoneGraph == null)
            {
                Debug.LogError($"Map Generator Error: MST failed to be found in {zone.Name} zone.");
                return false;
            }

            // Pathfind and Connect Main Path
            if (!ConnectMainPath(zone, zoneGraph))
            {
                Debug.LogError($"Map Generator Error: Main Path could not be connected in {zone.Name} zone.");
                return false;
            }
            */
            if (_debugLogs)
                Debug.Log($"Map Generator: {zone.Name} generated path {zone.MainPath.name} with {zone.MainPath.BlueprintCount()} rooms.");

            return true;
        }

        /// <summary>
        /// Places all Unique Rooms specified in zone.
        /// </summary>
        /// <param name="zone">Zone to place unique rooms in</param>
        /// <returns>Placement success or failure</returns>
        public bool LoadUniqueRoomOperations(Zone zone)
        {
            // 1.) Spawn Fixed Rooms (Rooms that have a set spawn destination)
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                if (entry.PlacementType == RoomPlacementType.Fixed)
                {
                    PathBlueprintData pathBlueprintData = new PathBlueprintData(context, zone.MainPath);
                    pathBlueprintData.LoadIntoMemory();
                    RoomEntryBlueprintData roomEntryBlueprintData = new RoomEntryBlueprintData(context, entry);
                    roomEntryBlueprintData.LoadIntoMemory();
                    BoundsIntBlueprintData boundsIntBlueprintData = new BoundsIntBlueprintData(context, zone.Bounds);
                    boundsIntBlueprintData.LoadIntoMemory();

                    PlaceFixedUniqueBlueprintsOp placeFixedBlueprintOp = new PlaceFixedUniqueBlueprintsOp(_blueprintGenerator, context,
                        pathBlueprintData.OutputPorts[0], roomEntryBlueprintData.OutputPorts[0], boundsIntBlueprintData.OutputPorts[0]);
                    operationQueue.Enqueue(placeFixedBlueprintOp);
                }
            }

            /*
            // 2.) Spawn Constrained Rooms (Rooms that have a unique spawn area)
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                bool hasPlaced = false;

                if (entry.PlacementType == RoomPlacementType.Constrained)
                {
                    int placementAttempts = 0;
                    // Attempt to place the constrained room in it's own bounded zone
                    while (!hasPlaced)
                    {
                        BoundsInt adjustedBounds = _blueprintGenerator.CreateIntersectingBounds(zone.Bounds, entry.Bounds);
                        hasPlaced = _blueprintGenerator.PlaceBoundedUniqueRoomBlueprints(zone.MainPath, entry, adjustedBounds);
                        placementAttempts++;

                        // If constrained room failed to generate a certain number of times then return false
                        if (placementAttempts > _maxPlacementAttempts)
                        {
                            Debug.LogError($"Map Generator Error: Constrained Room blueprints \"{entry}\" exhaused " +
                                $"all of it's attempts to be placed.");
                            return false;
                        }
                    }
                }
            }

            // 3.) Spawn Free Rooms (Rooms that can spawn in any point inside the zone bounds)
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                bool hasPlaced = false;

                if (entry.PlacementType == RoomPlacementType.Free)
                {
                    int placementAttempts = 0;
                    // Attempt to place the free room in the zone's bounded zone
                    while (!hasPlaced)
                    {
                        hasPlaced = _blueprintGenerator.PlaceBoundedUniqueRoomBlueprints(zone.MainPath, entry, zone.Bounds);
                        placementAttempts++;

                        // If free room failed to generate a certain number of times then return false
                        if (placementAttempts > _maxPlacementAttempts)
                        {
                            Debug.LogError($"Map Generator Error: Free Room blueprints \"{entry}\" exhaused all of it's attempts to be placed.");
                            return false;
                        }
                    }
                }
            }
            */
            return true;
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
                    DrawTriangulation();

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
            foreach (List<Edge> edgeList in _triangulations)
            {
                /*      DEPRICATED (NEED DELAUNYTRIANGULATION OBJECT FOR THIS DATA)
                // Draw circumcircles in remaining tetrahedron from triangulation
                foreach (Tetrahedron t in triangulation.Tetrahedra)
                {
                    Gizmos.color = _circumcircleColor;
                    Gizmos.DrawSphere(t.Circumcenter * _gridUnitSize, Mathf.Sqrt(t.CircumradiusSquared) * _gridUnitSize);
                }
                */

                // Draw remaining edges from triangulation
                foreach (Edge e in edgeList)
                {
                    Gizmos.color = _triangulationColor;
                    Gizmos.DrawLine(e.V.Position * _gridUnitSize, e.U.Position * _gridUnitSize);
                }
            }

            foreach (List<Edge> edgeList in _minimumSpanningTrees)
            {
                // Draw the minimum spanning tree of the zone
                foreach (Edge e in edgeList)
                {
                    Gizmos.color = _contiguousGraphColor;
                    Gizmos.DrawLine(e.V.Position * _gridUnitSize, e.U.Position * _gridUnitSize);
                }
            }

            foreach (List<Edge> edgeList in _randomCycles)
            {
                foreach (Edge e in edgeList)
                {
                    Gizmos.color = _randomCyclesColor;
                    Gizmos.DrawLine(e.V.Position * _gridUnitSize, e.U.Position * _gridUnitSize);
                }
            }

            if (_currentEdge is not null)
            {
                Gizmos.color = _currentEdgeColor;
                Gizmos.DrawLine(_currentEdge.V.Position * _gridUnitSize, _currentEdge.U.Position * _gridUnitSize);
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
