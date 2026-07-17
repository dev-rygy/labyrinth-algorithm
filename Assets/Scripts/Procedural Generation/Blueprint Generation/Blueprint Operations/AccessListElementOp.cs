/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   07/13/2026 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using RyansLibrary.Graphs;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class AccessListElementOp : BlueprintOperation
    {
        public AccessListElementOp(MapGenerationContext context, string indexInput, string listInput) : base(context)
        {
            OperationID = $"AccessListElementOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(indexInput);     // Index of element
            InputPorts.Add(listInput);      // The list itself

            // Output Ports
            string memoryID = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID);      // Element
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out int index))
                return false;
            if (!TryGetInput(1, out object list))
                return false;

            if (list is List<Edge> edgeList)
            {
                Edge element = edgeList[index];
                _context.Malloc(OutputPorts[0], element);
                return true;
            }
            else if (list is List<Blueprint> blueprintList)
            {
                Blueprint element = blueprintList[index];
                _context.Malloc(OutputPorts[0], element);
                return true;
            }
            else
            {
                Debug.LogError($"{OperationID} - Invalid input for operation. Types can only be List<Edge> or List<Blueprint>.");
                return false;
            }
        }
    }
}
