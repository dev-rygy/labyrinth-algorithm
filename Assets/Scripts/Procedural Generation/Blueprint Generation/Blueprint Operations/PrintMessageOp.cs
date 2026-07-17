/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   10/28/2025 (Ryan)
 * Notes:           
*/
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class PrintMessageOp : BlueprintOperation
    {
        public PrintMessageOp(MapGenerationContext context, string messageInput) : base(context)
        {
            OperationID = $"PrintOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(messageInput);
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out string msg))
                return false;

            Debug.Log($"[MapGenerator][Info] {msg}");

            return true;
        }
    }
}
