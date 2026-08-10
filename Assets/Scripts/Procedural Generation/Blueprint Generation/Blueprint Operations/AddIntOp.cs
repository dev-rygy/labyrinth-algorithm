/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   11/08/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Operation to add two ints together.
    /// </summary>
    public class AddIntOp : BlueprintOperation
    {
        public AddIntOp(MapGenerationContext context, string intAInput, string intBInput, string dataID = "") : base(context)
        {
            OperationID = $"AddIntOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(intAInput);
            InputPorts.Add(intBInput);
            InputPorts.Add(dataID);

            // If a memory ID was passed in for dataID, reuse that slot (in-place accumulation) instead of consuming
            // a brand new one - this is what lets this op double as a loop counter's "i = i + 1" increment.
            if (dataID != "")
                OutputPorts.Add(dataID);
            else
            {
                string memoryID = context.ConsumeMemoryID().ToString();
                OutputPorts.Add(memoryID);
            }
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out int intA))
                return false;
            if (!TryGetInput(1, out int intB))
                return false;

            int sum = intA + intB;

            _context.Set(OutputPorts[0], sum);

            return true;
        }
    }
}
