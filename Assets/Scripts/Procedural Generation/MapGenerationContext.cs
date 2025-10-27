/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   10/26/2025 (Ryan)
 * Notes:           
*/
using System.Collections.Generic;

public class MapGenerationContext
{
    private Dictionary<string, object> _memory;

    public int OperationIDCounter { get; private set; } = 0;
    public int MemoryIDCounter { get; private set; } = 0;
    public int OutputIDCounter { get; private set; } = 0;

    // Get Data
    public object GrabFromMemory(string inputID)
    {
        if (_memory.TryGetValue(inputID, out object value))
            return value;

        return null;
    }

    public void AllocateOrModifyMem(string id, object data)
    {
        bool mod = ModifyInputMemory(id, data);

        if (!mod)
        {
            AllocateInputMem(id, data);
        }
    }

    // Create New Data
    public void AllocateInputMem(string id, object data)
    {
        _memory.Add(id, data);
    }

    // Change Data
    public bool ModifyInputMemory(string id, object data)
    {
        if (_memory.ContainsKey(id))
        {
            _memory[id] = data;
            return true;
        }
        else
            return false;
    }

    public int ConsumeOperationID()
    {
        return OperationIDCounter++;
    }

    public int ConsumeMemoryID()
    {
        return MemoryIDCounter++;
    }
}
