/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/27/2025
 * Last Modified:   07/13/2026 (Ryan)
 * Notes:           
*/
using RyansLibrary.Graphs;
using RyansLibrary.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class ListDifferenceOp : BlueprintOperation
    {
        public ListDifferenceOp(MapGenerationContext context, BlueprintGenerator bpg, string listAInput, string listBInput)
                : base(context, bpg)
        {
            OperationID = $"ListDifferenceOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(listAInput);     // 1st list
            InputPorts.Add(listBInput);     // 2nd list

            // Output Ports
            string memoryID1 = context.ConsumeMemoryID().ToString();
            string memoryID2 = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID1);      // List
            OutputPorts.Add(memoryID2);      // List count

            if (_debugLogs) Debug.Log($"[MapGenerator][BlueprintOperation] ListDifferenceOp: List<T> space allocated for memory with ID {memoryID1}");
            if (_debugLogs) Debug.Log($"[MapGenerator][BlueprintOperation] ListDifferenceOp: Int space allocated for memory with ID {memoryID2}");
        }


        public override bool Execute()
        {
            if (!TryGetInput(0, out object listA))
                return false;
            if (!TryGetInput(1, out object listB))
                return false;

            if (listA is null || listB is null)
            {
                LogNullError();
                return false;
            }

            if (listA is List<Edge> edgeListA && listB is List<Edge> edgeListB)
            {
                List<Edge> resultEdgeList = TakeListDifference(edgeListA, edgeListB);
                _context.Malloc(OutputPorts[0], resultEdgeList);
                _context.Malloc(OutputPorts[1], resultEdgeList.Count);
                return true;
            }
            else if (listA is List<Blueprint> blueprintListA && listB is List<Blueprint> blueprintListB)
            {
                List<Blueprint> resultBlueprintList = TakeListDifference(blueprintListA, blueprintListB);
                _context.Malloc(OutputPorts[0], resultBlueprintList);
                _context.Malloc(OutputPorts[1], resultBlueprintList.Count);
                return true;
            }
            else
            {
                Debug.LogError($"[MapGenerator][BlueprintOperation] ListDifferenceOp: Invalid input types for Blueprint difference operation. " +
                    $"Types can only be List<Edge> or List<Blueprint>. Both lists must be of the same type.");
                return false;
            }
        }

        private List<T> TakeListDifference<T>(List<T> listA, List<T> listB)
        {
            return ListUtils.Difference(listA, listB);
        }
    }
}
