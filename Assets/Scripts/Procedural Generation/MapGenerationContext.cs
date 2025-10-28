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

    public int OperationIDCounter { get; private set; } = 10000;
    public int MemoryIDCounter { get; private set; } = 10000;
    public int OutputIDCounter { get; private set; } = 10000;

    public MapGenerationContext()
    {
        _memory = new Dictionary<string, object>();
    }

    // Get Data
    public object GrabFromMemory(string inputID)
    {
        if (_memory.TryGetValue(inputID, out object value))
            return value;

        return null;
    }

    public void AllocateOrModifyMem(string id, object data)
    {
        bool mod = ModifyMemory(id, data);

        if (!mod)
        {
            AllocateMemory(id, data);
        }
    }

    // Create New Data
    public void AllocateMemory(string id, object data)
    {
        _memory.Add(id, data);
    }

    // Change Data
    public bool ModifyMemory(string id, object data)
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
