using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class PathBlueprintData : BlueprintData
    {
        MapGenerationContext _context;
        Path _path;

        public PathBlueprintData(MapGenerationContext context, Path path) : base()
        {
            string memoryID = context.ConsumeMemoryID().ToString();
            DataID = $"PathData:{memoryID}";
            _context = context;
            _path = path;

            // Output Ports
            OutputPorts.Add(memoryID);
        }

        public override void LoadIntoMemory()
        {
            _context.AllocateMemory(OutputPorts[0], _path);
            Debug.Log($"Path Data Loaded Into Memory with ID {OutputPorts[0]}");
        }
    }
}
