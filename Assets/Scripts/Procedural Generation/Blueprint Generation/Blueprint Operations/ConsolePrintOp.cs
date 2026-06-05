/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class ConsolePrintOp : BlueprintOperation
    {
        public ConsolePrintOp(MapGenerationContext context, BlueprintGenerator bpg, string messageInput) : base(context, bpg)
        {
            OperationID = $"PrintOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(messageInput);
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out string msg))
                return false;

            Debug.Log($"Map Generator Print: {msg}");

            return true;
        }
    }
}
