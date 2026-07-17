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
    /// Holds data for a BoundsInt in the map generator's memory.
    /// </summary>
    public class BoundsIntBlueprintData : BlueprintData<BoundsInt>
    {

        public BoundsIntBlueprintData(MapGenerationContext context, BoundsInt value) : base(context, value)
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"BoundsIntData:{memoryID}";

            // Output Ports
            OutputPorts.Add(memoryID);
        }

        public override void LoadIntoMemory()
        {
            base.LoadIntoMemory();

            if (_debugLogs) LogDataAllocation();
        }
    }
}
