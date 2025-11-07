/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
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

        protected void LogInputError(int inputPortIndex, string type = "")
        {
            Debug.LogError($"Map Generator Error: {OperationID} failed to execute due to invalid {type} input with memory ID ({InputPorts[inputPortIndex]}).");
        }

        protected void LogNullError()
        {
            Debug.LogError($"Map Generator Error: {OperationID} failed to execute due to a required value being null.");
        }
    }
}