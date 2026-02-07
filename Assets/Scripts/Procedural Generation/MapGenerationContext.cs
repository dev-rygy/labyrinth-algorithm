/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   11/08/2025 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using RyansLibrary.Graphs;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Provides context and state management for procedural map generation operations, including operation sequencing,
    /// memory storage, and debugging data structures.
    /// </summary>
    /// <remarks>MapGenerationContext tracks the execution of blueprint operations through queues and history
    /// stacks, manages unique identifiers for operations and memory, and stores intermediate results such as
    /// triangulations, minimum spanning trees, and random cycles. It also offers methods for memory access and
    /// manipulation, enabling operations to share data and results. This class is not thread-safe; concurrent access
    /// should be externally synchronized if used in multithreaded scenarios.</remarks>
    public sealed class MapGenerationContext
    {
        public int OperationIDCounter { get; private set; } = 10000;
        public int MemoryIDCounter { get; private set; } = 20000;

        public LinkedList<BlueprintOperation> OperationQueue { get; private set; }
        public Stack<BlueprintOperation> OperationHistory { get; private set; }

        // Debugging Lists - These lists are intended to store intermediate results for debugging and visualization purposes
        public List<List<Edge>> Triangulations { get; private set; }
        public List<List<Edge>> MinimumSpanningTrees { get; private set; }
        public List<List<Edge>> RandomCycles { get; private set; }

        // Private Variables
        private Dictionary<string, object> _memory;

        public MapGenerationContext()
        {
            // Holds arguements and return values from operations
            _memory = new Dictionary<string, object>();

            // Initialize Debugging Lists
            Triangulations = new();
            MinimumSpanningTrees = new();
            RandomCycles = new();

            // Initialize operations
            OperationQueue = new();
            OperationHistory = new();
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
        /// Attempts to move the operation queue to the target operation, skipping intermediate operations as needed.
        /// </summary>
        /// <remarks>This method allows direct navigation to a specific operation within the queue, either
        /// forward or backward. If the operation queue is empty or the operation history is exhausted during a reverse
        /// jump, the method returns false and logs an error. The jump is performed by dequeuing or re-adding operations
        /// as necessary until the target operation is reached.</remarks>
        /// <param name="targetOperationID">The unique identifier of the target operation to jump to. Must be in the format "prefix:number" and
        /// correspond to an operation present in the queue or history.</param>
        /// <returns>true if the jump to the specified operation was successful; otherwise, false.</returns>
        public bool Jump(string targetOperationID)
        {
            // Check to see if queue has operations
            if (OperationQueue.Count <= 0)
            {
                Debug.LogError("Map Generator / Context Error: Attempted to perform jump operation while the operation queue was empty.");
                return false;
            }

            // Validate target operation ID format
            if (!TryParseOpNum(targetOperationID, out int targetOperationNum))
            {
                Debug.LogError($"Map Generator / Context Error: Invalid target operation ID format ({targetOperationID}). Expected \"prefix:number\".");
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
                    Debug.LogError($"Map Generator / Context Error: Operation queue exhausted before reaching target ({targetOperationID}).");
                    return false;
                }

                // Check if we've reached the target operation; end jump cycle
                if (current.OperationID == targetOperationID)
                    return true;

                if (!TryParseOpNum(current.OperationID, out int currNum))
                {
                    Debug.LogError($"Map Generator / Context Error: Invalid operation ID format in queue ({current.OperationID}).");
                    return false;
                }

                // Decide direction (FORWARD or REVERSE) based on numerical comparison of operation IDs
                if (targetOperationNum > currNum)
                {
                    // Move forward: queue -> history
                    BlueprintOperation op = OperationQueueDequeue();
                    if (op == null)
                        return false;

                    OperationHistory.Push(op);

                    // If the queue became empty, we can't reach anything further
                    if (OperationQueue.Count == 0)
                    {
                        Debug.LogError($"Map Generator Error: Target operation not found in queue ({targetOperationID}).");
                        return false;
                    }
                }
                else
                {
                    // Move backward: history -> queue front
                    if (OperationHistory.Count == 0)
                    {
                        Debug.LogError($"Map Generator Error: Operation history exhausted while attempting reverse jump to ({targetOperationID}).");
                        return false;
                    }

                    BlueprintOperation op = OperationHistory.Pop();
                    OperationQueueAddFront(op);
                }
            }

            /* OLD CODE (DELETE LATER)
            // Loop until target operation is found
            while (OperationQueuePeek().OperationID != targetOperationID)
            {
                // Get the current operation's ID number
                int currOperationNum = int.Parse(OperationQueuePeek().OperationID.Split(':')[1]);

                // Step forward in queue; Forward Jump
                if (targetOperationNum > currOperationNum)
                {
                    BlueprintOperation op = OperationQueueDequeue();
                    if (op is null)
                        return false;

                    OperationHistory.Push(op);
                }
                // Step backward in queue; Reverse Jump
                else
                {
                    if (OperationHistory.Count <= 0)
                    {
                        Debug.LogError("Map Generator / Context Error: Operation history exhausted while attempting reverse jump.");
                        return false;
                    }

                    BlueprintOperation op = OperationHistory.Pop();
                    OperationQueueAddFront(op);
                }
            }
            return true;
            */

            Debug.LogError($"Map Generator Error: Jump aborted (guard limit hit). Target not found: {targetOperationID}.");
            return false;
        }

        // Local helpers
        private bool TryParseOpNum(string operationId, out int num)
        {
            num = default;
            if (string.IsNullOrWhiteSpace(operationId)) 
                return false;

            int colon = operationId.LastIndexOf(':');
            if (colon < 0 || colon == operationId.Length - 1) 
                return false;

            return int.TryParse(operationId[(colon + 1)..], out num);
        }


        public bool TryGet<T>(string memoryID, out T value)
        {
            if (_memory.TryGetValue(memoryID, out object obj) && obj is T castValue)
            {
                value = castValue;
                return true;
            }

            Debug.LogWarning($"Map Generator Warning: Data with memory ID ({memoryID}) could not be found.");
            value = default; 
            return false;
        }

        public void Set(string memoryID, object value)
        {
            _memory[memoryID] = value;
        }

        public bool Contains(string memoryID) => _memory.ContainsKey(memoryID);

        public void Remove(string memoryID)
        {
            if (_memory.ContainsKey(memoryID))
                _memory.Remove(memoryID);
        }

        internal void ClearMemory() => _memory.Clear();

        public void ClearAll()
        {
            // Holds arguements and return values from operations
            _memory.Clear();

            // Initialize Debugging Lists
            Triangulations.Clear();
            MinimumSpanningTrees.Clear();
            RandomCycles.Clear();

            // Initialize operations
            OperationQueue.Clear();
            OperationHistory.Clear();
        }

        public int ConsumeOperationID()
        {
            return OperationIDCounter++;
        }

        public int ConsumeMemoryID()
        {
            return MemoryIDCounter++;
        }

        public void AddToTriangulationsList(List<Edge> triangulation)
        {
            Triangulations.Add(triangulation);
        }

        public void AddToMSTList(List<Edge> mst)
        {
            MinimumSpanningTrees.Add(mst);
        }

        public void AddToRandomCyclesList(List<Edge> rcList)
        {
            RandomCycles.Add(rcList);
        }
    }
}
