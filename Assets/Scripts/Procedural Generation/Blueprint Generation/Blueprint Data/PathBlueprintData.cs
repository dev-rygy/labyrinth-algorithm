/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class PathBlueprintData : BlueprintData
    {
        Path _path;

        public PathBlueprintData(MapGenerationContext context, Path path) : base(context)
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"PathData:{memoryID}";
            _path = path;

            // Output Ports
            OutputPorts.Add(memoryID);
        }

        public override void LoadIntoMemory()
        {
            _context.Set(OutputPorts[0], _path);
            Debug.Log($"Path Data Loaded Into Memory with ID {OutputPorts[0]}");
        }
    }
}
