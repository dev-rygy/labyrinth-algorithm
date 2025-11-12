/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class IntBlueprintData : BlueprintData
    {
        int integer;

        public IntBlueprintData(MapGenerationContext context, int integer) : base(context)
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"IntData:{memoryID}";
            this.integer = integer;

            // Output Ports
            OutputPorts.Add(memoryID);      // RoomEntry
        }

        public override void LoadIntoMemory()
        {
            _context.Set(OutputPorts[0], integer);
            if (_debugLogs) Debug.Log($"Int Data Loaded Into Memory with ID {OutputPorts[0]}");
        }
    }
}
