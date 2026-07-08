/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   06/04/2026 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Holds data for an integer in the map generator's memory.
    /// </summary>
    public class IntBlueprintData : BlueprintData<int>
    {

        public IntBlueprintData(MapGenerationContext context, int value) : base(context, value)
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"IntData:{memoryID}";

            // Output Ports
            OutputPorts.Add(memoryID);      // RoomEntry object
        }

        public override void LoadIntoMemory()
        {
            base.LoadIntoMemory();

            if (_debugLogs) Debug.Log($"[MapGenerator][BlueprintData] Int data loaded into memory with ID {OutputPorts[0]}");
        }
    }
}
