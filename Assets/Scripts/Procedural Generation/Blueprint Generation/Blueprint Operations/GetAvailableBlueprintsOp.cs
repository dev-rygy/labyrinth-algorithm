/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Operation that filters a blueprint list down to only the rooms whose Available flag matches the requested value. 
    /// Used to separate rooms that already claimed blueprints.
    /// </summary>
    public class GetAvailableBlueprintsOp : BlueprintOperation
    {
        public GetAvailableBlueprintsOp(MapGenerationContext context, string blueprintListInput, string availableToggleInput)
                : base(context)
        {
            OperationID = $"GetAvailableBlueprintsOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(blueprintListInput);
            InputPorts.Add(availableToggleInput);

            // Output Ports
            string memoryID = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID);
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out List<Blueprint> list))
                return false;
            if (!TryGetInput(1, out bool availbility))
                return false;

            if (list is null)
            {
                LogNullError();
                return false;
            }

            List<Blueprint> availableBlueprintList = GetAvailableBlueprints(list, availbility);

            _context.Malloc(OutputPorts[0], availableBlueprintList);

            return true;
        }

        private List<Blueprint> GetAvailableBlueprints(List<Blueprint> list, bool availbility)
        {
            return list.Where(bp => (bp.Claimed == availbility)).ToList();
        }
    }
}
