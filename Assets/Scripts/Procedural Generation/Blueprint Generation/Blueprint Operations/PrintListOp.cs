/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   06/08/2026 (Ryan)
 * Notes:           
*/
using RyansLibrary.Graphs;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class PrintListOp : BlueprintOperation
    {
        public PrintListOp(MapGenerationContext context, BlueprintGenerator bpg, string listInput, string listType) : base(context, bpg)
        {
            OperationID = $"PrintOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(listInput);
            InputPorts.Add(listType);
        }

        public override bool Execute()
        {
            if (!TryGetInput(1, out string listType))
                return false;

            switch (listType)
            {
                case "Edge":
                    if (!TryGetInput(0, out List<Edge> edgeList))
                        return false;

                    if (edgeList is null)
                    {
                        LogNullError();
                        return false;
                    }

                    // Print List
                    PrintList(edgeList);

                    return true;

                case "Blueprint":
                    if (!TryGetInput(0, out List<Blueprint> blueprintList))
                        return false;

                    if (blueprintList is null)
                    {
                        LogNullError();
                        return false;
                    }

                    // Print List
                    PrintList(blueprintList);

                    return true;
                default:
                    Debug.LogError($"[MapGenerator][BlueprintOperation] PrintListOp: Invalid Type for {OperationID}.");
                    return false;
            }
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
