/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class StringBlueprintData : BlueprintData
    {
        string stringData;

        public StringBlueprintData(MapGenerationContext context, string stringData) : base(context)
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"StringData:{memoryID}";
            this.stringData = stringData;

            // Output Ports
            OutputPorts.Add(memoryID);
        }

        public override void LoadIntoMemory()
        {
            _context.Set(OutputPorts[0], stringData);
            if (_debugLogs) Debug.Log($"String Data Loaded Into Memory with ID {OutputPorts[0]}");
        }
    }
}
