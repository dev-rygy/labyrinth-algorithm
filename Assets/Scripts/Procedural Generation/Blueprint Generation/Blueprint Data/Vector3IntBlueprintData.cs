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
    /// Holds data for a Vector3Int in the map generator's memory.
    /// </summary>
    public class Vector3IntBlueprintData : BlueprintData<Vector3Int>
    {
        public Vector3IntBlueprintData(MapGenerationContext context, Vector3Int value) : base(context, value)
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"Vector3IntData:{memoryID}";

            // Output Ports
            OutputPorts.Add(memoryID);      // Vector3Int object
        }

        public override void LoadIntoMemory()
        {
            base.LoadIntoMemory();

            if (_debugLogs) LogDataAllocation();
        }
    }
}
