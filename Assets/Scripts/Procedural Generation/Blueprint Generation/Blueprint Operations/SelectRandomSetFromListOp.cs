using RyansLibrary.Labyrinth;
using RyansLibrary.Utils;
using RyansLibrary.Graphs;
using System.Collections.Generic;
using UnityEngine;

public class SelectRandomSetFromListOp : BlueprintOperation
{
    public SelectRandomSetFromListOp(MapGenerationContext context, BlueprintGenerator bpg, string listInput, string elementCountInput, string listType) : base(context, bpg)
    {
        OperationID = $"SelectRandomFromListOp:{context.ConsumeOperationID()}";
        _bpg = bpg;
        _context = context;

        // Input Ports
        InputPorts.Add(listInput);
        InputPorts.Add(elementCountInput);
        InputPorts.Add(listType);

        // Output Ports
        OutputPorts.Add(context.ConsumeMemoryID().ToString());
    }


    public override bool Execute()
    {
        int elementCount = (int)_context.GrabFromMemory(InputPorts[1]);
        string listType = (string)_context.GrabFromMemory(InputPorts[2]);

        switch (listType)
        {
            case "Edge":
                List<Edge> listEdge = _context.GrabFromMemory(InputPorts[0]) as List<Edge>;
                List<Edge> resultListEdge = SelectRandomSetFromList(listEdge, elementCount);
                _context.AllocateOrModifyMem(OutputPorts[0], resultListEdge);
                _context.AddToRandomCyclesList(resultListEdge);
                return true;
            case "Blueprint":
                List<Blueprint> listBlueprint = _context.GrabFromMemory(InputPorts[0]) as List<Blueprint>;
                List<Blueprint> resultListBlueprint = SelectRandomSetFromList(listBlueprint, elementCount);
                _context.AllocateOrModifyMem(OutputPorts[0], resultListBlueprint);
                return true;
            default:
                Debug.LogError("Blueprint Operator Error: Invalid Type for Select Random Set Operator.");
                return false;
        }
    }

    public override bool Undo()
    {
        return false;
    }

    private List<T> SelectRandomSetFromList<T>(List<T> list, int elementCount)
    {
        return ListUtils.SelectRandomSet(list, elementCount);
    }
}
