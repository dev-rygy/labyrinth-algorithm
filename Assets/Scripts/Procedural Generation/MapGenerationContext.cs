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
        private Dictionary<string, object> _memory;

        public int OperationIDCounter { get; private set; } = 10000;
        public int MemoryIDCounter { get; private set; } = 20000;
        public int OutputIDCounter { get; private set; } = 30000;

        public List<List<Edge>> Triangulations { get; private set; }
        public List<List<Edge>> MinimumSpanningTrees { get; private set; }
        public List<List<Edge>> RandomCycles { get; private set; }

        public MapGenerationContext()
        {
            _memory = new Dictionary<string, object>();

            // Initialize Debugging Lists
            Triangulations = new();
            MinimumSpanningTrees = new();
            RandomCycles = new();
        }

        // Get Data
        public object GrabFromMemory(string memoryID)
        {
            if (_memory.TryGetValue(memoryID, out object value))
                return value;

            return null;
        }

        public void AllocateOrModifyMem(string id, object data)
        {
            bool mod = ModifyMemory(id, data);

            if (!mod)
            {
                AllocateMemory(id, data);
            }
        }

        // Create New Data
        public void AllocateMemory(string id, object data)
        {
            _memory.Add(id, data);
        }

        // Change Data
        public bool ModifyMemory(string id, object data)
        {
            if (_memory.ContainsKey(id))
            {
                _memory[id] = data;
                return true;
            }
            else
                return false;
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
