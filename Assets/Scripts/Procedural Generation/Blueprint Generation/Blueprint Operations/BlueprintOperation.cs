/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   11/08/2025 (Ryan)
 * Notes:           
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Represents an abstract base class for operations that can be performed as part of a blueprint-based map
    /// generation process.
    /// </summary>
    /// <remarks>
    /// BlueprintOperation defines the contract for executable and **undoable operations** (LATER IMPLEMENTATION) within the map
    /// generation context. Derived classes implement specific operations and manage their input and output ports. Each
    /// operation is associated with a unique identifier and can interact with the map generation context and blueprint
    /// generator.
    /// </remarks>
    /// <remarks>
    /// ** Thread safety is not guaranteed at the moment; instances are intended for use within a single map generation workflow. **
    /// </remarks>
    /// <remarks>
    /// The whole map generator works like a visual scripting graph (think Unreal Blueprints/shader graphs), except
    /// the "nodes" are plain C# objects instead of graph UI elements. Each subclass of BlueprintOperation is one node:
    /// its constructor declares how many inputs/outputs it has, and MapGenerationContext.OperationQueue is the
    /// linear sequence of nodes that gets executed in order (see MapGeneratorController for how the queue is built
    /// and run). A node never talks to another node directly - instead, InputPorts/OutputPorts hold *memory IDs*
    /// (string keys), and the actual values live in the context's shared data cache (MapGenerationContext.Malloc/
    /// TryGet). Wiring node A's output into node B's input just means giving them the same memory ID string. This
    /// indirection is what lets the algorithm be reordered/rewired (and eventually edited in a real node graph UI)
    /// without operations needing to know who is upstream or downstream of them.
    /// </remarks>
    public abstract class BlueprintOperation
    {
        // Global toggle for debug logs in all blueprint operation classes
        private static bool _debugLogs;
        public static bool DebugLogs => _debugLogs;

        // DataID for is used strictly for debugging purposes.
        public string OperationID { get; protected set; }

        // Ports are the auguments and return values of operations. Later on, these will be
        // used to create the connections between operations in a Unity graph.
        public List<string> InputPorts { get; protected set; }
        public List<string> OutputPorts { get; protected set; }

        // References to required map generation systems
        protected MapGenerationContext _context;

        public BlueprintOperation(MapGenerationContext context)
        {
            InputPorts = new List<string>();
            OutputPorts = new List<string>();

            _context = context;
        }

        public static void ToggleDebugLogs(bool toggle)
        {
            _debugLogs = toggle;
        }

        // Every operation must be able to execute when it is loaded into the operation queue.
        // Return true/false to signal success/failure back to whatever is driving the queue (see
        // MapGeneratorController), so a bad step can halt generation instead of silently corrupting later steps.
        public abstract bool Execute();

        // UNDO NOT YET IMPLEMENTED
        // public abstract bool Undo();

        // Resolves an input port to its actual value: InputPorts[inputPortIndex] is a memory ID string, not the
        // value itself, so this looks that ID up in the shared context cache. "required" lets an operation treat a
        // missing/blank port as a soft default (false = optional) instead of a hard failure.
        protected bool TryGetInput<T>(int inputPortIndex, out T value, bool required = true)
        {
            if (inputPortIndex < 0 && inputPortIndex >= InputPorts.Count)
            {
                Debug.LogError($"{OperationID} - Input index {inputPortIndex} was out of range 0 - {InputPorts.Count}.");
                value = default;
                return false;
            }

            string memoryId = InputPorts[inputPortIndex];

            if (string.IsNullOrWhiteSpace(memoryId))
            {
                value = default;

                if (required)
                {
                    Debug.LogError($"{OperationID} - Required input was not assiged at index {inputPortIndex}.");

                    return false;
                }    
                return true;
            }

            if (!_context.TryGet(memoryId, out T storedValue))
            {
                Debug.LogError($"{OperationID} - Input with memory ID ({memoryId}) is not valid in memory.");

                value = default;
                return true;
            }

            value = storedValue;
            return true;
        }

        protected void LogNullError()
        {
            Debug.LogError($"{OperationID} - Failed to execute due to a required value being null.");
        }
    }
}