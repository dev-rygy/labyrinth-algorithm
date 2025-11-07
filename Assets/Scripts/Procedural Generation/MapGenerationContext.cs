/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using RyansLibrary.Graphs;

namespace RyansLibrary.Labyrinth
{
    public sealed class MapGenerationContext
    {
        public int OperationIDCounter { get; private set; } = 10000;
        public int MemoryIDCounter { get; private set; } = 20000;

        public List<List<Edge>> Triangulations { get; private set; }
        public List<List<Edge>> MinimumSpanningTrees { get; private set; }
        public List<List<Edge>> RandomCycles { get; private set; }

        public LinkedList<BlueprintOperation> OperationQueue { get; private set; }
        public Stack<BlueprintOperation> OperationHistory { get; private set; }

        private Dictionary<string, object> _memory;

        public MapGenerationContext()
        {
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
            return OperationQueue.First.Value;
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
            BlueprintOperation op = OperationQueue.First.Value;
            OperationQueue.RemoveFirst();

            return op;
        }

        public void Jump(string operatorID)
        {
            int targetOperationNum = int.Parse(operatorID.Split(':')[1]);

            while (OperationQueuePeek().OperationID != operatorID)
            {
                int currOperationNum = int.Parse(OperationQueuePeek().OperationID.Split(':')[1]);


                if (targetOperationNum > currOperationNum)       // Skip to operation
                {
                    OperationHistory.Push(OperationQueueDequeue());
                }
                else                                            // Reverse to operation
                {
                    OperationQueueAddFront(OperationHistory.Pop());
                }
            }
        }

        public bool TryGet<T>(string memoryID, out T value)
        {
            if (_memory.TryGetValue(memoryID, out object obj) && obj is T castValue)
            {
                value = castValue;
                return true;
            }

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
