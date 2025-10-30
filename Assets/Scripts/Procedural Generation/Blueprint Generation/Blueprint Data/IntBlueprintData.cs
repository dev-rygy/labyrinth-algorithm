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
        MapGenerationContext _context;
        int integer;

        public IntBlueprintData(MapGenerationContext context, int integer) : base()
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"IntData:{memoryID}";
            _context = context;
            this.integer = integer;

            // Output Ports
            OutputPorts.Add(memoryID);      // RoomEntry
        }

        public override void LoadIntoMemory()
        {
            _context.AllocateMemory(OutputPorts[0], integer);
            Debug.Log($"Int Data Loaded Into Memory with ID {OutputPorts[0]}");
        }
    }
}
