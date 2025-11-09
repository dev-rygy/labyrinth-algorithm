/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   11/08/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class AddIntOp : BlueprintOperation
    {
        public AddIntOp(MapGenerationContext context, BlueprintGenerator bpg, string intAInput, string intBInput, string dataID = "") : base(context, bpg)
        {
            OperationID = $"AddIntOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(intAInput);
            InputPorts.Add(intBInput);
            InputPorts.Add(dataID);

            if (dataID != "")
                OutputPorts.Add(dataID);
            else
            {
                string memoryID = context.ConsumeMemoryID().ToString();
                OutputPorts.Add(memoryID);
                if (_debugLogs) Debug.Log($"Int space allocated for memory with ID {memoryID}");
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

        public override bool Undo()
        {
            return false;
        }
    }
}
