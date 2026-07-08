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
    /// Holds data for a string in the map generator's memory.
    /// </summary>
    public class StringBlueprintData : BlueprintData<string>
    {
        public StringBlueprintData(MapGenerationContext context, string value) : base(context, value)
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"StringData:{memoryID}";

            // Output Ports
            OutputPorts.Add(memoryID);      // string value
        }

        public override void LoadIntoMemory()
        {
            base.LoadIntoMemory();

            if (_debugLogs) Debug.Log($"[MapGenerator][BlueprintData] String data loaded into memory with ID {OutputPorts[0]}");
        }
    }
}
