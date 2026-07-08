/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   06/04/2026 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class PathBlueprintData : BlueprintData<Path>
    {
        public PathBlueprintData(MapGenerationContext context, Path value) : base(context, value)
        {
            string pathMemoryID = context.ConsumeMemoryID().ToString();
            DataID = $"PathData:{pathMemoryID}";

            string lengthMemoryID = context.ConsumeMemoryID().ToString();
            DataID = $"PathData:{lengthMemoryID}";

            string blueprintListID = context.ConsumeMemoryID().ToString();
            DataID = $"PathData:{blueprintListID}";

            // Output Ports
            OutputPorts.Add(pathMemoryID);          // Path
            OutputPorts.Add(lengthMemoryID);        // Path Length
            OutputPorts.Add(blueprintListID);       // Blueprint List
        }

        public override void LoadIntoMemory()
        {
            _context.Malloc(OutputPorts[0], _cache);
            _context.Malloc(OutputPorts[1], _cache.BlueprintCount());
            _context.Malloc(OutputPorts[2], _cache.BlueprintList);

            if (_debugLogs) Debug.Log($"[MapGenerator][BlueprintData] Path data loaded into memory with ID {OutputPorts[0]}");
        }
    }
}
