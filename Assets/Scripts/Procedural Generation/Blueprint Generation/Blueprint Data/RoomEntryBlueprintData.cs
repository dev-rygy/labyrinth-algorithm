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
    /// Holds data for a UniqueRoomEntry in the map generator's memory, exposing both the UniqueRoomEntry itself and its Bounds
    /// as separate output ports so placement operations (PlaceBoundedBlueprintsOp/PlaceFixedBlueprintsOp) can wire
    /// up just the bounds where that's all they need.
    /// </summary>
    public class RoomEntryBlueprintData : BlueprintData<UniqueRoomEntry>
    {

        public RoomEntryBlueprintData(MapGenerationContext context, UniqueRoomEntry value) : base(context, value)
        {
            string memoryID1 = context.ConsumeMemoryID().ToString();
            string memoryID2 = context.ConsumeMemoryID().ToString();
            DataID = $"RoomEntryData:{memoryID1}, {memoryID2}";

            // Output Ports
            OutputPorts.Add(memoryID1);      // UniqueRoomEntry object
            OutputPorts.Add(memoryID2);      // UniqueRoomEntry Bounds object
        }

        public override void LoadIntoMemory()       // Completely overridden to load 2 values, the UniqueRoomEntry and its bounds into memory.
        {
            _context.Set(OutputPorts[0], _cache);
            _context.Set(OutputPorts[1], _cache.Bounds);

            if (_debugLogs) LogDataAllocation();
        }
    }
}
