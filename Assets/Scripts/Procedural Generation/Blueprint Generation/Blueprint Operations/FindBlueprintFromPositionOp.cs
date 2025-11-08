using RyansLibrary.Graphs;
using RyansLibrary.Labyrinth;
using System;
using UnityEngine;

public class FindBlueprintFromPositionOp : BlueprintOperation
{
    public FindBlueprintFromPositionOp(MapGenerationContext context, BlueprintGenerator bpg, string positionInput) : base(context, bpg)
    {
        OperationID = $"FindBlueprintFromPositionOp:{context.ConsumeOperationID()}";

        // Input Ports
        InputPorts.Add(positionInput);      // Vector3 position

        // Output Ports
        string memoryID1 = context.ConsumeMemoryID().ToString();
        string memoryID2 = context.ConsumeMemoryID().ToString();

        OutputPorts.Add(memoryID1);      // Blueprint
        OutputPorts.Add(memoryID2);      // bool found

        if (_debugLogs) Debug.Log($"Blueprint space allocated for memory with ID {memoryID1}");
        if (_debugLogs) Debug.Log($"Bool space allocated for memory with ID {memoryID2}");
    }

    public override bool Execute()
    {
        if (!TryGetInput(0, out Vector3Int position))
            return false;

        bool result = _bpg.GetMasterDictionary().TryGetValue(position, out Blueprint blueprint);

        if (!result)
        {
            Debug.LogWarning($"Map Generator Warning: {OperationID} Blueprint could not be found from position.");
            return false;
        }

        _context.Set(OutputPorts[0], blueprint);
        _context.Set(OutputPorts[1], result);

        return true;
    }

    public override bool Undo()
    {
        return false;
    }
}
