/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   06/04/2026 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Holds data for a bool in the map generator's memory.
    /// </summary>
    public class BoolBlueprintData : BlueprintData<bool>
    {
        public BoolBlueprintData(MapGenerationContext context, bool value) : base(context, value)
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"BoolData:{memoryID}";

            // Output Ports
            OutputPorts.Add(memoryID);      // bool value
        }

        public override void LoadIntoMemory()
        {
            base.LoadIntoMemory();

            if (_debugLogs) Debug.Log($"Bool Data Loaded Into Memory with ID {OutputPorts[0]}");
        }
    }
}
