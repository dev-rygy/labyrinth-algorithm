using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class Vector3IntBlueprintData : BlueprintData
    {
        MapGenerationContext _context;
        Vector3Int vector3Int;

        public Vector3IntBlueprintData(MapGenerationContext context, Vector3Int vector3Int) : base()
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"Vector3IntData:{memoryID}";
            _context = context;
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
