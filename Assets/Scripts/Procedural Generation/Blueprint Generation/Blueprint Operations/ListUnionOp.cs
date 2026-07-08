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
    public class ListUnionOp : BlueprintOperation
    {
        public ListUnionOp(MapGenerationContext context, BlueprintGenerator bpg, string listAInput, string listBInput, string listType)
                : base(context, bpg)
        {
            OperationID = $"ListUnionOp:{context.ConsumeOperationID()}";

            // Input Ports
            InputPorts.Add(listAInput);     // 1st list
            InputPorts.Add(listBInput);     // 2nd list
            InputPorts.Add(listType);       // list type

            // Output Ports
            string memoryID1 = context.ConsumeMemoryID().ToString();
            string memoryID2 = context.ConsumeMemoryID().ToString();
            OutputPorts.Add(memoryID1);      // List
            OutputPorts.Add(memoryID2);      // List count
            if (_debugLogs) Debug.Log($"[MapGenerator][BlueprintOperation] ListUnionOp: List<T> space allocated for memory with ID {memoryID1}");
            if (_debugLogs) Debug.Log($"[MapGenerator][BlueprintOperation] ListUnionOp: Int space allocated for memory with ID {memoryID2}");
        }


        public override bool Execute()
        {
            if (!TryGetInput(2, out string listType))
                return false;

            switch (listType)
            {
                case "Edge":
                    if (!TryGetInput(0, out List<Edge> edgeListA))
                        return false;
                    if (!TryGetInput(1, out List<Edge> edgeListB))
                        return false;
                    if (edgeListA is null || edgeListB is null)
                    {
                        LogNullError();
                        return false;
                    }

                    List<Edge> resultEdgeList = TakeListUnion(edgeListA, edgeListB);
                    _context.Malloc(OutputPorts[0], resultEdgeList);
                    _context.Malloc(OutputPorts[1], resultEdgeList.Count);
                    return true;
                case "Blueprint":
                    if (!TryGetInput(0, out List<Blueprint> blueprintListA))
                        return false;
                    if (!TryGetInput(1, out List<Blueprint> blueprintListB))
                        return false;
                    if (blueprintListA is null || blueprintListB is null)
                    {
                        LogNullError();
                        return false;
                    }

                    List<Blueprint> resultBlueprintList = TakeListUnion(blueprintListA, blueprintListB);
                    _context.Malloc(OutputPorts[0], resultBlueprintList);
                    _context.Malloc(OutputPorts[1], resultBlueprintList.Count);
                    return true;
                default:
                    Debug.LogError($"[MapGenerator][BlueprintOperation] ListUnionOp: Invalid Type for {OperationID}.");
                    return false;
            }
        }
        private List<T> TakeListUnion<T>(List<T> listA, List<T> listB)
        {
            return ListUtils.Union(listA, listB);
        }
    }
}
