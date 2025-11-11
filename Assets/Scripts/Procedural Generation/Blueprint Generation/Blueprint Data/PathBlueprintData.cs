/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   11/09/2025 (Ryan)
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
            string pathMemoryID = context.ConsumeMemoryID().ToString();
            DataID = $"PathData:{pathMemoryID}";
            _path = path;

            string lengthMemoryID = context.ConsumeMemoryID().ToString();
            DataID = $"PathData:{lengthMemoryID}";

            string blueprintListID = context.ConsumeMemoryID().ToString();
            DataID = $"PathData:{blueprintListID}";

            // Output Ports
            OutputPorts.Add(pathMemoryID);
            OutputPorts.Add(lengthMemoryID);
            OutputPorts.Add(blueprintListID);
        }

        public override void LoadIntoMemory()
        {
            _context.Set(OutputPorts[0], _path);
            _context.Set(OutputPorts[1], _path.BlueprintCount());
            _context.Set(OutputPorts[2], _path.BlueprintList);
            if (_debugLogs) Debug.Log($"Path Data Loaded Into Memory with ID {OutputPorts[0]}");
        }
    }
}
