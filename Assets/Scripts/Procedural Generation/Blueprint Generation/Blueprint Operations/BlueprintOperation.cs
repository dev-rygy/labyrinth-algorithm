/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   11/08/2025 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Represents an abstract base class for operations that can be performed as part of a blueprint-based map
    /// generation process.
    /// </summary>
    /// <remarks>
    /// BlueprintOperation defines the contract for executable and undoable operations within the map
    /// generation context. Derived classes implement specific operations and manage their input and output ports. Each
    /// operation is associated with a unique identifier and can interact with the map generation context and blueprint
    /// generator. 
    /// 
    /// ** Thread safety is not guaranteed at the moment; instances are intended for use within a single map generation workflow. **
    /// </remarks>
    public abstract class BlueprintOperation
    {
        // Global toggle for debug logs in all blueprint operation classes
        protected static bool _debugLogs { get; private set; }

        // DataID for is used strictly for debugging purposes.
        public string OperationID { get; protected set; }

        // Ports are the auguments and return values of operations. Later on, these will be
        // used to create the connections between operations in a Unity graph.
        public List<string> InputPorts { get; protected set; }
        public List<string> OutputPorts { get; protected set; }

        // References to required map generation systems
        protected BlueprintGenerator _bpg;
        protected MapGenerationContext _context;

        public BlueprintOperation(MapGenerationContext context, BlueprintGenerator bpg)
        {
            InputPorts = new List<string>();
            OutputPorts = new List<string>();

            _context = context;
            _bpg = bpg;
        }

        public static void ToggleDebugLogs(bool toggle)
        {
            _debugLogs = toggle;
        }

        // Every operation must be able to execute when it is loaded into the operation queue.
        public abstract bool Execute();

        // UNDO NOT IMPLEMENTED
        // public abstract bool Undo();

        protected bool TryGetInput<T>(int inputPortIndex, out T value, bool required = true)
        {
            if (inputPortIndex < 0 && inputPortIndex >= InputPorts.Count)
            {
                Debug.LogError($"Map Generator Error: {OperationID} Input index {inputPortIndex} was out of range 0 - {InputPorts.Count}.");
                value = default;
                return false;
            }

            string memoryId = InputPorts[inputPortIndex];

            if (string.IsNullOrWhiteSpace(memoryId))
            {
                value = default;

                if (required)
                {
                    Debug.LogError($"Map Generator Error: {OperationID} - Required input was not assiged at index {inputPortIndex}.");
                    return false;
                }    
                return true;
            }

            if (!_context.TryGet(memoryId, out T storedValue))
            {
                Debug.LogError($"Map Generator Error: {OperationID} - Input with memory ID ({memoryId}) is not valid in memory.");
                value = default;
                return true;
            }

            value = storedValue;
            return true;
        }

        protected void LogNullError()
        {
            Debug.LogError($"Map Generator Error: {OperationID} - Failed to execute due to a required value being null.");
        }
    }
}