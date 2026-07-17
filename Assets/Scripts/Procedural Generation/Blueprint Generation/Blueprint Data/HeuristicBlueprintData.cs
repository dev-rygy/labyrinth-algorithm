/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   06/04/2026 (Ryan)
 * Notes:           
*/
using RyansLibrary.AI;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Holds data for enum Heuristic in the map generator's memory.
    /// </summary>
    public class HeuristicBlueprintData : BlueprintData<Heuristic>
    {
        public HeuristicBlueprintData(MapGenerationContext context, Heuristic value) : base(context, value)
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"HeuristicData:{memoryID}";

            // Output Ports
            OutputPorts.Add(memoryID);      // Heuristic object
        }

        public override void LoadIntoMemory()
        {
            base.LoadIntoMemory();

            if (_debugLogs) LogDataAllocation();
        }
    }
}
