/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class BoundsIntBlueprintData : BlueprintData
    {
        MapGenerationContext _context;
        BoundsInt _bounds;

        public BoundsIntBlueprintData(MapGenerationContext context, BoundsInt bounds) : base()
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"BoundsIntData:{memoryID}";
            _context = context;
            _bounds = bounds;

            // Output Ports
            OutputPorts.Add(memoryID);
        }

        public override void LoadIntoMemory()
        {
            _context.AllocateMemory(OutputPorts[0], _bounds);
            Debug.Log($"BoundsInt Data Loaded Into Memory with ID {OutputPorts[0]}");
        }
    }
}
