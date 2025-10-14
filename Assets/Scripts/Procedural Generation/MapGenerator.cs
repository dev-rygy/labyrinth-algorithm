/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   10/03/2025 (Ryan)
 * Notes:           Map Generator
*/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;      // Use Unity Engine's Random not System.Collection's Random

using RyansLibrary.AI;
using RyansLibrary.Geometry;
using RyansLibrary.Graphs;
using RyansLibrary.UnityEditor;

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

        [field: Header("Connection Rooms")]
        // Connection rooms (if null then it will choose randomly from the given path below
        [field: SerializeField] public RoomEntry RoomA { get; set; }
        [field: SerializeField] public RoomEntry RoomB { get; set; }

        [field: Header("Connection Path")]
        [field: SerializeField] public Path ConnectionPath { get; set; }
    }
    #endregion

    /// <summary>
    /// Composition master class that wraps the Blueprint and Room Generator classes to build a contigious map.
    /// </summary>
    public class MapGenerator : MonoBehaviour
    {
        #region Variables
        // ***** CONSTANTS *****
        const string MASTER_PATH_NAME = "Master Path";

        // ***** Singleton Reference *****
        public static MapGenerator Instance { get; private set; }

        // ***** Events *****
        public static event Action OnGenerationStarted;
        public static event Action OnGenerationDone;
        public static event Action OnGenerationFailed;

        // ***** Path Containers *****
        // The Master Path holds a reference to all bluprint rooms in an zone
        public Path MasterPath { get; private set; }

        // Dictionary used for quick access like checking locations for conflicts and checking locations for room shape conditions
        // Keys are in room coords
        public Dictionary<Vector3Int, BlueprintRoom> MasterDictionary { get; private set; }
        
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
        [SerializeField] private bool _useNewDrunkardWalkAlg;

        [Header("Blueprint Settings")]
        [SerializeField] private int maxPlacementAttempts = 50;

        [Header("Zones")]
        [SerializeField] private List<Zone> _zones;

        // Entrys to connect zones together
        [Header("Zone Connection")]
        [SerializeField] private List<ZoneConnectionEntry> _zoneConnections;

        [Header("Debuging")]
        [Space]
        [SerializeField] private Color _boundingBoxColor;
        [SerializeField] private Color _triangulationColor;
        [SerializeField] private Color _circumcircleColor;
        [SerializeField] private Color _contiguousGraphColor;
        [SerializeField] private Color _randomCyclesColor;
        [SerializeField] private Color _currentEdgeColor;

        // ***** Private Variables *****
        // private int _seed;      // TODO: For networking make the host generate this

        private BlueprintGenerator _blueprintGenerator;
        private RoomGenerator _roomGenerator;

        // Debugging
        private bool _debug = false;
        private List<DelaunayTriangulation3D> _triangulations;
        private List<List<Edge>> _minimumSpanningTrees;
        private List<Edge> _randomCycles;
        private Edge _currentEdge;
        private Coroutine _edgeFlashCo;
        private float _flashTime = 0.5f;

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
                GenerateLabyrinth();
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
            _triangulations = new List<DelaunayTriangulation3D>();
            _minimumSpanningTrees = new List<List<Edge>>();
            _randomCycles = new List<Edge>();

            // Initialize the Main Path in each Zone
            foreach (Zone zone in _zones)
                InitializeZone(zone);
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
            zone.MainPath.Initialize();     // Must be done before zone connection blueprints
        }
        #endregion

        /// <summary>
        /// Labyrinth Algorithm, an algorithm that creates a unique blueprint of the map before parsing and generating rooms.
        /// - A blueprint of the map is created by first placing unique rooms that are required for the map (boss room, miniboss, shop, etc.)
        /// - Then, a pathfinding algorithm connects these unique rooms together using boyer-watson alorogithm in conjunction with A*
        /// - Finally, the blueprint is parsed and rooms of different shapes and sizes are choosen based on the space available
        /// 
        /// The map generator is the top level of the system, this script is in charge of generating zones and zone connections.
        /// </summary>
        private void GenerateLabyrinth()
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

            // Generate Blueprint Map For Each Zone
            foreach (Zone zone in _zones)
            {
                if (!GenerateZoneBlueprints(zone))
                {
                    GenerationFailed();
                    return;     // Blueprint failed for zone; stop algorithm
                }
            }

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

            IsGenerating = false;

            // Labyrinth Generation Success
            // Event to signal when map generation is complete
            OnGenerationDone?.Invoke();
        }

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
            if (!GenerateMainPathBlueprint(zone))
            {
                Debug.LogError($"Map Generator Error: Main Path Generation for {zone.Name} zone failed.");
                return false;
            }

            // Generate Alternative paths (prize, trial, etc.)
            if (!GenerateAltPathBlueprints(zone))
            {
                Debug.LogWarning($"Map Generator Warning: Alt. Path Generation for {zone.Name} zone failed.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Wrapper function for generating the Main Path. 
        /// A path that leads to the zone boss, alternative paths, and 
        /// entrances to other zones.
        /// </summary>
        /// <param name="zone">Zone who's Main Path to generate.</param>
        /// <returns>Generation success or failure</returns>
        public bool GenerateMainPathBlueprint(Zone zone)
        {
            if (zone.MainPath == null)      // Throw error if MainPath for zone does not exist
            {
                Debug.LogError($"Map Generator Error: The Main Path for zone {zone.Name} is not assigned.");
                return false;
            }

            // Unique Room Placement
            if (!PlaceUniqueRooms(zone))
            {
                Debug.LogError($"Map Generator Error: Placing Unique Rooms failed in {zone.Name} zone.");
                return false;
            }

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

            if (_debugLogs) 
                Debug.Log($"Map Generator: {zone.Name} generated path {zone.MainPath.name} with {zone.MainPath.BlueprintCount()} rooms.");

            return true;
        }

        /// <summary>
        /// Places all Unique Rooms specified in zone.
        /// </summary>
        /// <param name="zone">Zone to place unique rooms in</param>
        /// <returns>Placement success or failure</returns>
        public bool PlaceUniqueRooms(Zone zone)
        {
            // 1.) Spawn Fixed Rooms (Rooms that have a set spawn destination)
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                if (entry.PlacementType == RoomPlacementType.Fixed)
                {
                    // Attempt to place fixed room (room with specified spawn position) in zone
                    bool hasPlaced = _blueprintGenerator.PlaceFixedUniqueRoomBlueprints(entry, zone.MainPath, zone.Bounds);

                    if (!hasPlaced)     // Only one attempt needed for a fixed room, otherwise generation has failed entirely
                    {
                        // Fixed room failed to generate, stop all operations
                        Debug.LogError($"Map Generator Error: Fixed Room bluprints \"{entry}\" was outside of bounds and could not be placed.");
                        return false;
                    }
                }
            }

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
                        hasPlaced = _blueprintGenerator.PlaceBoundedUniqueRoomBlueprints(entry, zone.MainPath, entry.Bounds);
                        placementAttempts++;

                        // If constrained room failed to generate a certain number of times then return false
                        if (placementAttempts > maxPlacementAttempts)
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
                        hasPlaced = _blueprintGenerator.PlaceBoundedUniqueRoomBlueprints(entry, zone.MainPath, zone.Bounds);
                        placementAttempts++;

                        // If free room failed to generate a certain number of times then return false
                        if (placementAttempts > maxPlacementAttempts)
                        {
                            Debug.LogError($"Map Generator Error: Free Room blueprints \"{entry}\" exhaused all of it's attempts to be placed.");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Place 1x1x1 blueprints randomly in the zone to create more 
        /// variable and random pathways.
        /// </summary>
        /// <param name="zone">The zone to place divergent rooms in</param>
        /// <returns>Placement success or failure</returns>
        public bool PlaceDivergentRooms(Zone zone)
        {
            Path mainPath = zone.MainPath;
            int occupancy = zone.DivergentRoomsCellOccupancy;
            int indexOffset = 1;
            int placementAttempts = 0;

            for (int i = 0; i < occupancy; i += indexOffset)
            {
                // Attempt to spawn blueprints
                bool result = _blueprintGenerator.PlaceBoundedBlueprints(zone.MainPath, zone.Bounds, Vector3Int.one, out Vector3Int spawnPos);

                if (result)     // Successful placement
                {
                    placementAttempts = 0;      // Reset attempts
                    indexOffset = 1;        // Increase iteration
                }
                else            // Failed placement 
                {
                    placementAttempts++;        // Increase attempts
                    indexOffset = 0;        // Stagnate iteration
                }

                // If divergent room failed to generate a certain number of times then return false
                if (placementAttempts > maxPlacementAttempts)
                {
                    Debug.LogError($"Map Generator Error: A divergent room in zone {zone} exhaused all of it's placement attempts.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Takes all generated blueprints in a zone, triangulates using bowyer-watson,
        /// and returns a contiguous and properly connected graph between all of them.
        /// NOTE: Only runs on available blueprint rooms.
        /// </summary>
        /// <param name="zone">The zone to triangulate</param>
        /// <returns>MST graph of zone blueprints; null if not found.</returns>
        private List<Edge> GenerateContigiousTriangulation(Zone zone)
        {
            if (zone == null || zone.MainPath == null)
            {
                Debug.LogError($"Map Generator Error: Error Zone {zone.Name} in invalid for triangulation." +
                    $"Zone is null or MainPath has not been initialized");
                return null;
            }

            DelaunayTriangulation3D triangulation = _blueprintGenerator.GenerateTriangulationFromPath(zone.MainPath);

            if (triangulation == null)      // Triangulation failed
                return null;

            // Store triangulation for debug gizmo
            _triangulations.Add(triangulation);

            // Turn off blueprint room availability for unique rooms 
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                foreach (Vector3Int cell in entry.AvailableCells)
                {
                    Vector3Int actualPos = entry.SpawnPosition + cell;      // Find the actual position in room space of the cell
                    if (MasterDictionary.TryGetValue(actualPos, out BlueprintRoom r))
                        r.Available = false;
                }
            }

            List<Edge> MST = _blueprintGenerator.FindMinimumSpanningTree(triangulation.Edges, triangulation.Edges[0].U);

            if (MST == null)      // MST/Prim's failed
                return null;

            // Store MST for debug gizmo
            _minimumSpanningTrees.Add(MST);

            // TODO: Choose random edges from triangulation
            List<Edge> zoneGraph = new List<Edge>(MST);
            List<Edge> availableEdges = triangulation.Edges.Except(MST).ToList();               // Remove all MST edges from list; difference

            // Choose random edges from the graph with none of the MST edges to add
            // varience to the map
            for (int i = 0; i < zone.RandomCyclesInGraph; i++)
            {
                if (availableEdges.Count <= 0)
                {
                    Debug.LogError("Map Generator Error: Too many random cycles in zone.");
                    return null;
                }

                int randomEdgeIndex = Random.Range(0, availableEdges.Count);
                Edge selectedEdge = availableEdges[randomEdgeIndex];

                zoneGraph.Add(selectedEdge);
                availableEdges.Remove(selectedEdge);
                _randomCycles.Add(selectedEdge);
            }

            return zoneGraph;
        }

        /// <summary>
        /// Pathfind to every blueprint from using a graph and generate more
        /// blueprints along the way.
        /// </summary>
        /// <param name="zone">Zone to connect with A*.</param>
        /// <param name="edges">Edges to connect with pathfinding algorithm</param>
        /// <returns>Pathfinding/Connection success or failure</returns>
        private bool ConnectMainPath(Zone zone, List<Edge> edges)
        {
            if (zone == null || zone.MainPath == null)
            {
                Debug.LogError($"Map Generator Error: Error Zone {zone.Name} in invalid for pathfinding." +
                    $"Zone is null or MainPath has not been initialized");
                return false;
            }

            // Pathfind each edge
            foreach (Edge e in edges)
            {
                _currentEdge = e;

                Vector3Int startPos = new Vector3Int((int)e.U.Position.x, (int)e.U.Position.y, (int)e.U.Position.z);        // Starting Vertex
                Vector3Int endPos = new Vector3Int((int)e.V.Position.x, (int)e.V.Position.y, (int)e.V.Position.z);          // Ending Vertex

                bool result = _blueprintGenerator.PathfindBlueprintFromPath(zone.MainPath, zone.Bounds, startPos, endPos, zone.PathfindingHeuristic);
                if (!result)        // Pathfinding failed for edge
                {
                    Debug.LogError($"Map Generator Error: A* failed to pathfind for edge {e}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Wrapper function for generating alternative paths in zones.
        /// Uses Drunkerd Walk Alg.
        /// <param name="zone">Zone for path to connect to.</param>
        /// </summary>
        public bool GenerateAltPathBlueprints(Zone zone)
        {
            if (zone.MainPath == null)      // Throw error if MainPath for zone does not exist
            {
                Debug.LogError($"Map Generator Error: The Main Path for zone {zone.name} is not assigned.");
                return false;
            }

            // Set base for paths
            int baseIndex = zone.MainPath.BlueprintCount() - 1;

            foreach (Path path in zone.Paths)       // O(n * m) where n is the path and m is the room range
            {
                if (path == null)
                {
                    Debug.LogError($"Map Generator Error: A path {path.Name} for zone {zone.name} is not assigned.");
                    return false;
                }

                // Initialize path
                int pathStartingIndex = baseIndex;
                int pathEndingIndex = baseIndex + path.PathLength;
                path.Initialize(pathStartingIndex, pathEndingIndex);

                // Create new path and walk
                // TODO: Later use Dijkstra maps to find a more controllable starting and ending index
                bool pathPlaced = false;
                // TODO: Store startIndex and endIndex in the path itself
                int startIndex = 1;
                int endIndex = zone.MainPath.BlueprintCount() - 1;      // Start at index 1 as to not choose the starting room of the game
                int randomStartingIndex = Random.Range(startIndex, endIndex);   // Choose a random room respecting the constraints

                // Attempt to place path in range
                Func<int, int> circularIncrement = x => (x < endIndex + 1) ? ++x : x = startIndex;
                for (int i = randomStartingIndex; i != randomStartingIndex - 1; i = circularIncrement(i))
                {
                    // Choose new start room
                    BlueprintRoom startRoom = zone.MainPath.BlueprintRooms[i];
                    path.ClearBlueprintRooms();

                    if (!startRoom.Available)       // Check if start room is available
                        continue;

                    pathPlaced = _blueprintGenerator.BlueprintDrunkardWalk(path, zone.Bounds, startRoom);

                    // Break out of loop to prevent duplicate path placement
                    if (pathPlaced)
                        break;
                }

                if (pathPlaced)
                {
                    baseIndex = pathEndingIndex;    // Reset base index for next path
                    if (_debugLogs) Debug.Log($"Map Generator: {path.name} generated with {path.BlueprintCount()} rooms.");
                }
                else
                {
                    Debug.LogError("Map Generator Error: Path could not be generated off of given rooms. Path obstructed.");
                    return false;
                }
            }

            return true;
        }

        // Generate Zone Connection Paths
        // TODO: Make connection entrys into a type of zone of it's own that intersects two zones together
        // TODO: Possibly handle this generation when both zone blueprints have been already been generated
        public bool GenerateZoneConnectionBlueprints(ZoneConnectionEntry entry)
        {
            if (entry.ConnectionPath == null)
            {
                Debug.LogError("Map Generator Error: Connection path was null.");
                return false;
            }

            // Init. Path
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

            // Handle Room A Placement
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
        public bool GenerateZoneRooms(Zone zone)
        {
            // Must have an zone to generate anything
            if (zone == null)
            {
                Debug.LogError($"Map Generator Error: Zone Entry Missing for room generation procedure.");
                return false;
            }

            // Generate Unique Rooms
            bool result;
            result = GenerateUniqueRooms(zone);
            if (!result)
            {
                Debug.LogError($"Map Generator Error: Unique Room Generation for zone {zone} failed.");
                return false;
            }

            // Generate Rooms along main path
            result = _roomGenerator.ParsePathAndGenerateRooms(zone.MainPath);
            if (!result)
            {
                Debug.LogError($"Map Generator Error: Path Room Generation for path {zone.MainPath} in zone {zone} failed.");
                return false;
            }

            // Generator Rooms along alt. paths
            foreach (Path path in zone.Paths)
            {
                result = _roomGenerator.ParsePathAndGenerateRooms(path);
                if (!result)
                {
                    Debug.LogError($"Map Generator Error: Path Room Generation for path {path} in zone {zone} failed.");
                    return false;
                }
            }

            return true;
        }

        public bool GenerateUniqueRooms(Zone zone)
        {
            foreach (RoomEntry entry in zone.UniqueRooms)
            {
                // Adjust parameters to fit the zone's actual position
                Vector3Int zoneOffset = zone.Bounds.position;
                Vector3Int adjustedSpawnPos = entry.SpawnPosition + zoneOffset;

                if (MasterPath == null || MasterDictionary == null)
                {
                    Debug.Log("Map Generator Error: Masters are null.");
                    return false;
                }

                Room generatedRoom = _roomGenerator.GenerateRoom(entry.Prefab, adjustedSpawnPos, zone.MainPath);

                // TODO: Make this into a new function in the room generator. Make the function check for all rooms inside
                // the unique room.
                // Unique rooms with available cells
                if (entry.AvailableCells != null)
                {
                    for (int i = 0; i < entry.AvailableCells.Count; i++)
                    {
                        if (MasterDictionary.TryGetValue(adjustedSpawnPos + entry.AvailableCells[i], out BlueprintRoom room))
                        {
                            generatedRoom.CopyBlueprintEntranceFlags(room.entrancewayFlags, i, Vector3.zero);
                        }
                        else
                        {
                            Debug.LogError($"Map Generator Error: Could not copy entranceway flags into unique room {entry}.");
                            return false;
                        }
                    }
                }

                generatedRoom.Initialize();
            }

            return true;
        }

        public bool GenerateZoneConnectionRooms(ZoneConnectionEntry entry)
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
                    {
                        generatedRoomA.CopyBlueprintEntranceFlags(room.entrancewayFlags, i, Vector3.zero);
                    }
                    else
                    {
                        Debug.LogError($"Map Generator Error: Could not copy entranceway flags into unique room");
                        return false;
                    }
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
                    {
                        generatedRoomB.CopyBlueprintEntranceFlags(room.entrancewayFlags, i, Vector3.zero);
                    }
                    else
                    {
                        Debug.LogError($"Map Generator Error: Could not copy entranceway flags into unique room");
                        return false;
                    }
                }
            }

            generatedRoomB.Initialize();

            // ******* Spawn Rooms On Connection Path ******
            bool result;
            result = _roomGenerator.ParsePathAndGenerateRooms(entry.ConnectionPath);
            if (!result)
            {
                Debug.LogError($"Map Generator Error: Path Room Generation for zone connection path {entry.ConnectionPath}.");
                return false;
            }

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
            foreach (DelaunayTriangulation3D triangulation in _triangulations)
            {
                // Draw circumcircles in remaining tetrahedron from triangulation
                foreach (Tetrahedron t in triangulation.Tetrahedra)
                {
                    Gizmos.color = _circumcircleColor;
                    Gizmos.DrawSphere(t.Circumcenter * _gridUnitSize, Mathf.Sqrt(t.CircumradiusSquared) * _gridUnitSize);
                }

                // Draw remaining edges from triangulation
                foreach (Edge e in triangulation.Edges)
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

            foreach (Edge e in _randomCycles)
            {
                Gizmos.color = _randomCyclesColor;
                Gizmos.DrawLine(e.V.Position * _gridUnitSize, e.U.Position * _gridUnitSize);
            }

            if (_currentEdge is not null)
            {
                Gizmos.color = _currentEdgeColor;
                Gizmos.DrawLine(_currentEdge.V.Position * _gridUnitSize, _currentEdge.U.Position * _gridUnitSize);
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