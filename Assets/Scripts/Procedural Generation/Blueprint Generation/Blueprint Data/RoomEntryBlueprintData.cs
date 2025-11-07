/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class RoomEntryBlueprintData : BlueprintData
    {
        RoomEntry _roomEntry;

        public RoomEntryBlueprintData(MapGenerationContext context, RoomEntry roomEntry) : base(context)
        {
            string memoryID1 = context.ConsumeMemoryID().ToString();
            string memoryID2 = context.ConsumeMemoryID().ToString();
            DataID = $"RoomEntryData:{memoryID1}, {memoryID2}";
            _roomEntry = roomEntry;

            // Output Ports
            OutputPorts.Add(memoryID1);      // RoomEntry
            OutputPorts.Add(memoryID2);      // RoomEntry Bounds
        }

        public override void LoadIntoMemory()
        {
            _context.Set(OutputPorts[0], _roomEntry);
            _context.Set(OutputPorts[1], _roomEntry.Bounds);
            Debug.Log($"RoomEntry Data Loaded Into Memory with ID {OutputPorts[0]}");
            Debug.Log($"Bounds Data Loaded Into Memory with ID {OutputPorts[1]}");
        }
    }
}
