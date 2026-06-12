/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   11/08/2025 (Ryan)
 * Notes:           
*/
using RyansLibrary.Graphs;
using RyansLibrary.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class ListSelectRandomSetFromOp : BlueprintOperation
    {
        public ListSelectRandomSetFromOp(MapGenerationContext context, BlueprintGenerator bpg, string listInput, string elementCountInput, string listType) : base(context, bpg)
        {
            OperationID = $"SelectRandomSetFromListOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(listInput);
            InputPorts.Add(elementCountInput);
            InputPorts.Add(listType);

            // Output Ports
            string memoryID = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID);
            if (_debugLogs) Debug.Log($"List<Edge> space allocated for memory with ID {memoryID}");
        }


        public override bool Execute()
        {
            if (!TryGetInput(1, out int elementCount))
                return false;
            if (!TryGetInput(2, out string listType))
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

                    List<Edge> resultListEdge = SelectRandomSetFromList(edgeList, elementCount);
                    _context.Malloc(OutputPorts[0], resultListEdge);
                    _context.AddToRandomCyclesList(resultListEdge);
                    return true;
                case "Blueprint":
                    if (!TryGetInput(1, out List<Blueprint> blueprintList))
                        return false;
                    if (blueprintList is null)
                    {
                        LogNullError();
                        return false;
                    }

                    List<Blueprint> resultListBlueprint = SelectRandomSetFromList(blueprintList, elementCount);
                    _context.Malloc(OutputPorts[0], resultListBlueprint);
                    return true;
                default:
                    Debug.LogError($"Map Generator Error: Invalid Type for {OperationID}.");
                    return false;
            }
        }
        private List<T> SelectRandomSetFromList<T>(List<T> list, int elementCount)
        {
            return ListUtils.SelectRandomSet(list, elementCount);
        }
    }
}
