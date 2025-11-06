using RyansLibrary.Labyrinth;
using RyansLibrary.Graphs;
using System.Collections.Generic;
using UnityEngine;

public class AccessListElementOp : BlueprintOperation
{
    public AccessListElementOp(MapGenerationContext context, BlueprintGenerator bpg, string indexInput, string listInput, string listType) : base(context, bpg)
    {
        OperationID = $"AccessListElementOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(indexInput);
        InputPorts.Add(listInput);
        InputPorts.Add(listType);

        // Output Ports
        OutputPorts.Add(context.ConsumeMemoryID().ToString());
    }

    public override bool Execute()
    {
        int index = (int)_context.GrabFromMemory(InputPorts[0]);
        string listType = (string)_context.GrabFromMemory(InputPorts[2]);

        switch (listType)
        {
            case "Edge":
                List<Edge> edgeList = _context.GrabFromMemory(InputPorts[1]) as List<Edge>;
                Edge edgeElement = edgeList[index];
                _context.AllocateOrModifyMem(OutputPorts[0], edgeElement);
                return true;
            case "Blueprint":
                List<Blueprint> blueprintList = _context.GrabFromMemory(InputPorts[1]) as List<Blueprint>;
                Blueprint blueprintElement = blueprintList[index];
                _context.AllocateOrModifyMem(OutputPorts[0], blueprintElement);
                return true;
            default:
                Debug.LogError("Blueprint Operator Error: Invalid Type for Union Operator.");
                return false;
        }
    }

    public override bool Undo()
    {
        return false;
    }
}
