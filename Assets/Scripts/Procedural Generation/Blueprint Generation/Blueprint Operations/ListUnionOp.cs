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
    /// <summary>
    /// Set union: merges two lists of either Edge or Blueprint into one. Its main use is recombining the
    /// MST with the randomly re-selected loop edges (ListSelectRandomSetFromOp) into the single final edge list that
    /// gets walked to carve corridors - see MapGeneratorController.LoadMainPathConnectionsOperations.
    /// </summary>
    public class ListUnionOp : BlueprintOperation
    {
        public ListUnionOp(MapGenerationContext context, string listAInput, string listBInput)
                : base(context)
        {
            OperationID = $"ListUnionOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(listAInput);     // 1st list
            InputPorts.Add(listBInput);     // 2nd list

            // Output Ports
            string memoryID1 = context.ConsumeMemoryID().ToString();
            string memoryID2 = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID1);      // List
            OutputPorts.Add(memoryID2);      // List count
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
                List<Edge> resultEdgeList = TakeListUnion(edgeListA, edgeListB);
                _context.Set(OutputPorts[0], resultEdgeList);
                _context.Set(OutputPorts[1], resultEdgeList.Count);
                return true;
            }
            else if (listA is List<Blueprint> blueprintListA && listB is List<Blueprint> blueprintListB)
            {
                List<Blueprint> resultBlueprintList = TakeListUnion(blueprintListA, blueprintListB);
                _context.Set(OutputPorts[0], resultBlueprintList);
                _context.Set(OutputPorts[1], resultBlueprintList.Count);
                return true;
            }
            else
            {
                Debug.LogError($"ListUnionOp: Invalid input types for Blueprint union operation. " +
                    $"Types can only be List<Edge> or List<Blueprint>. Both lists must be of the same type.");
                return false;
            }
        }

        private List<T> TakeListUnion<T>(List<T> listA, List<T> listB)
        {
            return ListUtils.Union(listA, listB);
        }
    }
}
