/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class BoolBlueprintData : BlueprintData
    {
        bool boolean;

        public BoolBlueprintData(MapGenerationContext context, bool boolean) : base(context)
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"BoolData:{memoryID}";
            this.boolean = boolean;

            // Output Ports
            OutputPorts.Add(memoryID);      // RoomEntry
        }

        public override void LoadIntoMemory()
        {
            _context.Set(OutputPorts[0], boolean);
            if (_debugLogs) Debug.Log($"Bool Data Loaded Into Memory with ID {OutputPorts[0]}");
        }
    }
}
