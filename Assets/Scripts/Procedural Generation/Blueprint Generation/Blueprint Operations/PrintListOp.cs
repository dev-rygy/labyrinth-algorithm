/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   06/08/2026 (Ryan)
 * Notes:           
*/
using RyansLibrary.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Debug utility operation: logs every element of a list stored in memory to the console, one line per element.
    /// </summary>
    public class PrintListOp : BlueprintOperation
    {
        public PrintListOp(MapGenerationContext context, string listInput, string listType) : base(context)
        {
            OperationID = $"PrintOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(listInput);
            InputPorts.Add(listType);
        }

        public override bool Execute()
        {
            if (!TryGetInput(0, out List<object> list))
                return false;

            if (list is null)
            {
                LogNullError();
                return false;
            }

            // Print List
            PrintList(list);
            return true;
        }

        private void PrintList<T>(List<T> list)
        {
            Debug.Log("Print List:");

            foreach (var item in list)
            {
                Debug.Log(item.ToString());
            }
        }
    }
}
