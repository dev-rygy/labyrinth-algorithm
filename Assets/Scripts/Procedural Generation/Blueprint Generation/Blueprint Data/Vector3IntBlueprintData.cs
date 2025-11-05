/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class Vector3IntBlueprintData : BlueprintData
    {
        Vector3Int vector3Int;

        public Vector3IntBlueprintData(MapGenerationContext context, Vector3Int vector3Int) : base(context)
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"Vector3IntData:{memoryID}";
            this.vector3Int = vector3Int;

            // Output Ports
            OutputPorts.Add(memoryID);      // RoomEntry
        }

        public override void LoadIntoMemory()
        {
            _context.AllocateMemory(OutputPorts[0], vector3Int);
            Debug.Log($"Vector3Int Data Loaded Into Memory with ID {OutputPorts[0]}");
        }
    }
}
