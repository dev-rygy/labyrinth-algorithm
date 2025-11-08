/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using RyansLibrary.Graphs;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public sealed class MapGenerationContext
    {
        public int OperationIDCounter { get; private set; } = 10000;
        public int MemoryIDCounter { get; private set; } = 20000;

        public LinkedList<BlueprintOperation> OperationQueue { get; private set; }
        public Stack<BlueprintOperation> OperationHistory { get; private set; }

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

        public BlueprintOperation OperationQueuePeek()
        {
            return OperationQueue?.First.Value;
        }

        public void OperationQueueEnqueue(BlueprintOperation op)
        {
            OperationQueue.AddLast(op);
        }

        public void OperationQueueAddFront(BlueprintOperation op)
        {
            OperationQueue.AddFirst(op);
        }

        public BlueprintOperation OperationQueueDequeue()
        {
            if (OperationQueue.First is null)
                return null;

            BlueprintOperation op = OperationQueue.First.Value;
            OperationQueue.RemoveFirst();
            return op;
        }

        public bool Jump(string targetOperationID)
        {
            // Check to see if queue has operations
            if (OperationQueue.Count <= 0)
            {
                Debug.LogError("Map Generator Error: Attempted to perform jump operation while the operation queue was empty.");
                return false;
            }

            int targetOperationNum = int.Parse(targetOperationID.Split(':')[1]);

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
                        Debug.LogError("Map Generator Error: Operation history exhausted while attempting reverse jump.");
                        return false;
                    }

                    BlueprintOperation op = OperationHistory.Pop();
                    OperationQueueAddFront(op);
                }
            }
            return true;
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

        internal void Clear() => _memory.Clear();

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
