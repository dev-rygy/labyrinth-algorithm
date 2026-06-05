/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/28/2025
 * Last Modified:   06/04/2026 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Represents the base class for blueprint data used in map generation processes. Data is needed to be cached and loaded
    /// from memory during operations just like registers in a CPU, and this class provides the structure for that data management.
    /// </summary>
    /// <remarks>
    /// This class defines shared members and behaviors for blueprint data, including management of
    /// output ports and debug logging. Derived classes needs implement the abstract LoadIntoMemory method to load
    /// specific blueprint data as required by the map generation context. The class is intended to be extended and is
    /// not instantiated directly.
    /// </remarks>
    public abstract class BlueprintData<T>
    {
        // Global toggle for debug logs in all blueprint data classes; does not currently work atm.
        protected static bool _debugLogs { get; private set; }


        // DataID for is used strictly for debugging purposes.
        public string DataID { get; protected set; }
        
        public List<string> OutputPorts { get; protected set; }

        // Temp data storage for the boolean value before it's loaded into memory.
        protected T _cache;

        // References to required map generation systems
        protected MapGenerationContext _context;

        public BlueprintData(MapGenerationContext context, T value)
        {
            OutputPorts = new List<string>();
            _context = context;
            _cache = value;
        }

        public static void ToggleDebugLogs(bool toggle)
        {
            _debugLogs = toggle;
        }

        public virtual void LoadIntoMemory()
        {
            _context.Malloc(OutputPorts[0], _cache);
        }


        // Modifies the data stored in memory at the output port with the new data provided.
        public void ModifyData(T newData)
        {
            _cache = newData;
            _context.Malloc(OutputPorts[0], newData);
        }
    }
}
