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
    /// Holds data for a RoomEntry in the map generator's memory.
    /// </summary>
    public class RoomEntryBlueprintData : BlueprintData<RoomEntry>
    {

        public RoomEntryBlueprintData(MapGenerationContext context, RoomEntry value) : base(context, value)
        {
            string memoryID1 = context.ConsumeMemoryID().ToString();
            string memoryID2 = context.ConsumeMemoryID().ToString();
            DataID = $"RoomEntryData:{memoryID1}, {memoryID2}";

            // Output Ports
            OutputPorts.Add(memoryID1);      // RoomEntry object
            OutputPorts.Add(memoryID2);      // RoomEntry Bounds object
        }

        public override void LoadIntoMemory()       // Completely overridden to load 2 values, the RoomEntry and its bounds into memory.
        {
            _context.Malloc(OutputPorts[0], _cache);
            _context.Malloc(OutputPorts[1], _cache.Bounds);

            if (_debugLogs) LogDataAllocation();
        }
    }
}
