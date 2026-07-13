/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   07/13/2026 (Ryan)
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
        public ListSelectRandomSetFromOp(MapGenerationContext context, BlueprintGenerator bpg, string listInput, string setSize) : base(context, bpg)
        {
            OperationID = $"SelectRandomSetFromListOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(listInput);      // List
            InputPorts.Add(setSize);        // Integer for how big the random set should be

            // Output Ports
            string memoryID = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID);  // List that carries the random set

            if (_debugLogs) Debug.Log($"[MapGenerator][BlueprintOperation] ListSelectRandomSetFromOp: List<T> space allocated for memory with ID {memoryID}");
        }


        public override bool Execute()
        {
            if (!TryGetInput(0, out object list))
                return false;
            if (!TryGetInput(1, out int setSize))
                return false;

            if (list is null)
            {
                LogNullError();
                return false;
            }

            if (list is List<Edge> edgeList)
            {
                List<Edge> resultListEdge = SelectRandomSetFromList(edgeList, setSize);
                _context.Malloc(OutputPorts[0], resultListEdge);
                _context.AddToRandomCyclesList(resultListEdge);
                return true;
            }
            else if (list is List<Blueprint> blueprintList)
            {
                List<Blueprint> resultListBlueprint = SelectRandomSetFromList(blueprintList, setSize);
                _context.Malloc(OutputPorts[0], resultListBlueprint);
                return true;
            }
            else
            {
                Debug.LogError($"[MapGenerator][BlueprintOperation] ListSelectRandomSetFromOp: Invalid input types for Blueprint select random operation. " +
                    $"Types can only be List<Edge> or List<Blueprint>.");
                return false;
            }
        }
        private List<T> SelectRandomSetFromList<T>(List<T> list, int elementCount)
        {
            return ListUtils.SelectRandomSet(list, elementCount);
        }
    }
}
