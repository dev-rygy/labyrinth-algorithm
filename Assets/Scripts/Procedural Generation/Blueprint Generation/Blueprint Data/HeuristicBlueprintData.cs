/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.AI;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class HeuristicBlueprintData : BlueprintData
    {
        Heuristic heuristic;

        public HeuristicBlueprintData(MapGenerationContext context, Heuristic heuristic) : base(context)
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"HeuristicData:{memoryID}";
            this.heuristic = heuristic;

            // Output Ports
            OutputPorts.Add(memoryID);      // Heuristic
        }

        public override void LoadIntoMemory()
        {
            _context.Set(OutputPorts[0], heuristic);
            if (_debugLogs) Debug.Log($"Heuristic Data Loaded Into Memory with ID {OutputPorts[0]}");
        }
    }
}
