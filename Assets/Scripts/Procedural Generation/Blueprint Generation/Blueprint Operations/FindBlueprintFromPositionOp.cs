/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.Labyrinth;
using System;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class FindBlueprintFromPositionOp : BlueprintOperation
    {
        public FindBlueprintFromPositionOp(MapGenerationContext context, string positionInput) : base(context)
        {
            OperationID = $"FindBlueprintFromPositionOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(positionInput);      // Vector3 position

            // Output Ports
            string memoryID1 = context.ConsumeMemoryID().ToString();
            string memoryID2 = context.ConsumeMemoryID().ToString();

            OutputPorts.Add(memoryID1);      // Blueprint
            OutputPorts.Add(memoryID2);      // bool found
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out Vector3Int position))
                return false;

            bool result = _context.BlueprintDictionary.TryGetValue(position, out Blueprint blueprint);

            if (!result)
            {
                Debug.LogWarning($"[MapGenerator][BlueprintOperation] FindBlueprintFromPositionOp: Blueprint could not be found from position {position}.");
                return false;
            }

            _context.Malloc(OutputPorts[0], blueprint);
            _context.Malloc(OutputPorts[1], result);

            return true;
        }
    }
}
