/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   06/04/2026 (Ryan)
 * Notes:           
*/
using RyansLibrary.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Provides context and state management for procedural map generation operations, including operation sequencing,
    /// memory storage, and debugging data structures.
    /// </summary>
    /// <remarks>
    /// MapGenerationContext tracks the execution of blueprint operations through queues and history
    /// stacks, manages unique identifiers for operations and memory, and stores intermediate results such as
    /// triangulations, minimum spanning trees, and random cycles. It also offers methods for memory access and
    /// manipulation, enabling operations to share data and results. 
    /// </remarks>
    /// <remarks>
    /// WARNING: This class is not yet thread-safe; concurrent access
    /// should be externally synchronized if used in multithreaded scenarios.
    /// </remarks>
    public sealed class MapGenerationContext
    {
        public static event Action<List<Edge>> OnNewTriangulation;
        public static event Action<List<Edge>> OnNewMST;
        public static event Action<List<Edge>> OnNewRandomCycles;

        // ***** Blueprint Container *****
        // Dictionary used for quick access like checking locations for conflicts and checking locations for room shape conditions
        // Keys are in room coords
        private Dictionary<Vector3Int, Blueprint> _blueprintDictionary;
        public Dictionary<Vector3Int, Blueprint> BlueprintDictionary => _blueprintDictionary;

        private int _operationIDCounter = 10000;
        public int OperationIDCounter => _operationIDCounter;
        private int _memoryIDCounter = 20000;
        public int MemoryIDCounter => _memoryIDCounter;

        // Stores blueprint operations to be executed later
        private LinkedList<BlueprintOperation> _operationQueue;
        public LinkedList<BlueprintOperation> OperationQueue => _operationQueue;

        // UNIMPLEMENTED: Later this can be used for backtracking or undoing operations
        private Stack<BlueprintOperation> _operationHistory;
        public Stack<BlueprintOperation> OperationHistory => _operationHistory;

        // Debugging Lists - These lists are intended to store intermediate results for debugging and visualization purposes

        // Stores outputs from blueprint operations
        private Dictionary<string, object> _dataCache;

        public MapGenerationContext()
        {
            // Holds arguments and return values from operations
            _dataCache = new();

            // Holds all blueprint objects in scene
            _blueprintDictionary = new();

            // Initialize operations
            _operationQueue = new();
            _operationHistory = new();
        }

        /// <summary>
        /// Returns the next operation in the queue without removing it.
        /// </summary>
        /// <remarks>Use this method to inspect the next operation scheduled for processing without
        /// modifying the queue. If the queue contains no operations, the method returns null.</remarks>
        /// <returns>The first operation in the queue, or null if the queue is empty.</returns>
        public BlueprintOperation OperationQueuePeek()
        {
            return OperationQueue?.First.Value;
        }

        /// <summary>
        /// Adds a blueprint operation to the end of the operation queue for later processing.
        /// </summary>
        /// <param name="op">The blueprint operation to enqueue. Cannot be null.</param>
        public void OperationQueueEnqueue(BlueprintOperation op)
        {
            OperationQueue?.AddLast(op);

            if (BlueprintOperation.DebugLogs)
            {
                Debug.Log($"{op.OperationID} - Operation loaded into memory.");
            }
        }

        /// <summary>
        /// Adds the specified operation to the front of the operation queue.
        /// </summary>
        /// <param name="op">The operation to add to the front of the queue. Cannot be null.</param>
        public void OperationQueueAddFront(BlueprintOperation op)
        {
            OperationQueue?.AddFirst(op);
        }

        /// <summary>
        /// Removes and returns the next operation from the operation queue.
        /// </summary>
        /// <returns>The next <see cref="BlueprintOperation"/> in the queue, or <see langword="null"/> if the queue is empty.</returns>
        public BlueprintOperation OperationQueueDequeue()
        {
            // Check to see if queue has atleast one operation stored
            if (OperationQueue?.First is null)
                return null;

            // Get the first operation in the queue and remove it from the queue
            BlueprintOperation op = OperationQueue?.First.Value;
            OperationQueue?.RemoveFirst();
            return op;
        }

        /// <summary>
        /// Attempts to reposition the operation queue and history to the specified operation, moving forward or
        /// backward as needed.
        /// </summary>
        /// <remarks>The method moves operations between the queue and history to reach the specified
        /// target. If the target operation is not found, or if the operation ID format is invalid, the method returns
        /// false. The jump is limited to prevent infinite loops and will abort if the max limit is reached.</remarks>
        /// <param name="targetOperationID">The unique identifier of the target operation to jump to. Must be in the format "prefix:number" and
        /// correspond to an operation present in the queue or history.</param>
        /// <returns>true if the jump successfully reaches the target operation; otherwise, false.</returns>
        public bool Jump(string targetOperationID)
        {
            // Check to see if queue has operations
            if (OperationQueue.Count <= 0)
            {
                Debug.LogError("Attempted to perform jump operation while the operation queue was empty.");
                return false;
            }

            // Validate target operation ID format
            if (!TryParseOpNum(targetOperationID, out int targetOperationNum))
            {
                Debug.LogError($"Invalid target operation ID format ({targetOperationID}). Expected \"prefix:number\".");
                return false;
            }

            // Guard against infinite loops:
            // You can move at most (queue.Count + history.Count) times before repeating states.
            int maxMoves = OperationQueue.Count + OperationHistory.Count + 1;

            for (int moves = 0; moves < maxMoves; moves++)
            {
                BlueprintOperation current = OperationQueuePeek();
                if (current == null)
                {
                    Debug.LogError($"Operation queue exhausted before reaching target ({targetOperationID}).");
                    return false;
                }

                // Check if we've reached the target operation; end jump cycle
                if (current.OperationID == targetOperationID)
                    return true;

                if (!TryParseOpNum(current.OperationID, out int currNum))
                {
                    Debug.LogError($"Invalid operation ID format in queue ({current.OperationID}).");
                    return false;
                }

                // Decide direction (FORWARD or REVERSE) based on numerical comparison of operation IDs
                if (targetOperationNum > currNum)
                {
                    // Move forward: queue -> history
                    BlueprintOperation op = OperationQueueDequeue();
                    if (op is null)
                        return false;

                    OperationHistory.Push(op);

                    // If the queue became empty, we can't reach anything further
                    if (OperationQueue.Count == 0)
                    {
                        Debug.LogError($"Target operation not found in queue ({targetOperationID}).");
                        return false;
                    }
                }
                else
                {
                    // Move backward: history -> queue front
                    if (OperationHistory.Count == 0)
                    {
                        Debug.LogError($"Operation history exhausted while attempting reverse jump to ({targetOperationID}).");
                        return false;
                    }

                    BlueprintOperation op = OperationHistory.Pop();
                    OperationQueueAddFront(op);
                }
            }

            Debug.LogError($"Jump aborted (guard limit hit). Target not found: {targetOperationID}.");
            return false;

            static bool TryParseOpNum(string operationId, out int num)
            {
                num = default;
                if (string.IsNullOrWhiteSpace(operationId))
                    return false;

                int colon = operationId.LastIndexOf(':');
                if (colon < 0 || colon == operationId.Length - 1)
                    return false;

                return int.TryParse(operationId[(colon + 1)..], out num);
            }
        }
        
        /// <summary>
        /// Attempts to retrieve a value associated with the specified memory ID and type from the internal memory store.
        /// </summary>
        /// <remarks>If the memory entry is not found or is not of the specified type, a warning is logged
        /// and the output value is set to its default.</remarks>
        /// <typeparam name="T">The type of the value to retrieve.</typeparam>
        /// <param name="memoryID">The unique identifier for the memory entry to retrieve. Cannot be null.</param>
        /// <param name="value">When this method returns, contains the value associated with the specified memory ID if found and of type
        /// <typeparamref name="T">; otherwise, the default value for <typeparamref name="T">.</param>
        /// <returns>true if a value of type <typeparamref name="T"> was found for the specified memory ID; otherwise, false.</returns>
        public bool TryGet<T>(string memoryID, out T value)
        {
            if (_dataCache is null)
            {
                Debug.LogError($"Memory object not set.");
                value = default;
                return false;
            }

            // Check to see if memory contains the requested ID and if the value is of the expected type
            if (_dataCache.TryGetValue(memoryID, out object obj) && obj is T castValue)
            {
                value = castValue;
                return true;
            }

            Debug.LogWarning($"Data with memory ID ({memoryID}) could not be found.");
            value = default;
            return false;
        }

        /// <summary>
        /// Manupulate or allocate the stored value in a specified memory slot.
        /// </summary>
        /// <param name="memoryID">The unique identifier for the memory entry to set. Cannot be null.</param>
        /// <param name="value">The value to associate with the specified memory identifier. Can be any object.</param>
        public void Set(string memoryID, object value)
        {
            if (_dataCache is null)
            {
                Debug.LogError($"Memory object not set.");
                return;
            }

            _dataCache[memoryID] = value;
        }

        /// <summary>
        /// Determines whether the memory contains an entry with the specified identifier.
        /// </summary>
        /// <param name="memoryID">The unique identifier of the memory entry to locate. Cannot be null.</param>
        /// <returns>true if an entry with the specified identifier exists in the memory; otherwise, false.</returns>
        public bool Contains(string memoryID)
        {
            if (_dataCache is null)
            {
                Debug.LogError($"Memory object not set.");
                return false;
            }

            return _dataCache.ContainsKey(memoryID);
        }

        /// <summary>
        /// Removes the entry with the specified memory identifier from the memory collection, if it exists.
        /// </summary>
        /// <param name="memoryID">The unique identifier of the memory entry to remove. Cannot be null.</param>
        public void Remove(string memoryID)
        {
            if (_dataCache is null)
            {
                Debug.LogError($"Memory object not set.");
                return;
            }

            if (_dataCache.ContainsKey(memoryID))
                _dataCache.Remove(memoryID);
        }

        /// <summary>
        /// Clears all stored data, including memory, debugging lists, and operation history.
        /// </summary>
        /// <remarks>Call this method to reset the internal state of the object, removing all accumulated
        /// results and pending operations. After calling this method, the object will be in a clean state, as if newly
        /// initialized.</remarks>
        public void ClearAll()
        {
            // Clear memory
            ClearMemory();

            // Clear operation collections
            ClearOperationCollections();

            // Clear Master Lists
            ClearDictionary();
        }

        public void ClearOperationCollections()
        {
            OperationQueue?.Clear();
            OperationHistory?.Clear();
        }

        public void ClearDictionary()
        {
            _blueprintDictionary?.Clear();
        }

        /// <summary>
        /// Clears all data stored in the memory object associated with the current context.
        /// </summary>
        /// <remarks>This method should be called only when it is safe to discard all memory data.</remarks>
        internal void ClearMemory()
        {
            if (_dataCache is null)
            {
                Debug.LogError($"Memory object not set.");
                return;
            }

            _dataCache.Clear();
        }

        /// <summary>
        /// Retrieves the next available operation identifier and advances the internal counter.
        /// </summary>
        /// <remarks>This method is not thread-safe. If called concurrently from multiple threads,
        /// operation identifiers may not be unique.</remarks>
        /// <returns>The current operation identifier before incrementing. Each call returns a unique integer value.</returns>
        public int ConsumeOperationID()
        {
            return _operationIDCounter++;
        }

        /// <summary>
        /// Generates and returns the next available memory identifier.
        /// </summary>
        /// <remarks>This method is not thread-safe. If called concurrently from multiple threads,
        /// operation identifiers may not be unique.</remarks>
        /// <returns>The next integer value representing a unique memory identifier.</returns>
        public int ConsumeMemoryID()
        {
            return _memoryIDCounter++;
        }

        /// <summary>
        /// Adds a triangulation, represented as a list of edges, to the collection of triangulations to be used for debugging purposes.
        /// </summary>
        /// <param name="triangulation">The list of edges that defines the triangulation to add. Cannot be null.</param>
        public void InvokeNewTriangulation(List<Edge> triangulation)
        {
            OnNewTriangulation?.Invoke(triangulation);
        }

        /// <summary>
        /// Adds a MST, represented as a list of edges, to the collection of MSTs to be used for debugging purposes.
        /// </summary>
        /// <param name="mst">The list of edges that defines the minimum spanning tree to add. Cannot be null.</param>
        public void InvokeNewMST(List<Edge> mst)
        {
            OnNewMST?.Invoke(mst);
        }

        /// <summary>
        /// Adds a random cycle, represented as a list of edges, to the collection of random cycles to be used for debugging purposes.
        /// </summary>
        /// <param name="rcList">The list of edges that defines the random cycle to add. Cannot be null.</param>
        public void InvokeNewRandonCycles(List<Edge> rcList)
        {
            OnNewRandomCycles?.Invoke(rcList);
        }

        public int GetOperationQueueCount()
        {
            if (OperationQueue is null)
            {
                Debug.LogError("Operation Queue is not yet assigned.");
                return 0;
            }

            return OperationQueue.Count;
        }

        public int GetOperationHistoryCount() 
        {
            if (OperationHistory is null)
            {
                Debug.LogError("Operation History is not yet assigned.");
                return 0;
            }

            return OperationHistory.Count; 
        }
    }
}
