/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public abstract class BlueprintOperation
    {
        public string OperationID { get; protected set; }

        public List<string> InputPorts { get; protected set; }
        public List<string> OutputPorts { get; protected set; }

        protected BlueprintGenerator _bpg;
        protected MapGenerationContext _context;

        public BlueprintOperation(MapGenerationContext context, BlueprintGenerator bpg)
        {
            InputPorts = new List<string>();
            OutputPorts = new List<string>();

            _context = context;
            _bpg = bpg;
        }

        public abstract bool Execute();

        public abstract bool Undo();

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