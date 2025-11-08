/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public abstract class BlueprintData
    {
        protected static bool _debugLogs { get; private set; }

        public static void ToggleDebugLogs(bool toggle)
        {
            _debugLogs = toggle;
        }

        public string DataID { get; protected set; }

        public List<string> OutputPorts { get; protected set; }

        protected MapGenerationContext _context;

        public BlueprintData(MapGenerationContext context)
        {
            OutputPorts = new List<string>();
            _context = context;
        }

        public abstract void LoadIntoMemory();

        public void ModifyData(object newData)
        {
            _context.Set(OutputPorts[0], newData);
        }
    }
}
