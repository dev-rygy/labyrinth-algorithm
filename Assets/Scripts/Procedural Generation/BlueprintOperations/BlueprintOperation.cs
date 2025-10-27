/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2025
 * Last Modified:   10/26/2025 (Ryan)
 * Notes:           
*/
using System;
using System.Collections.Generic;

/*
public interface IBlueprintOperationPort
{
    string ID { get; set; }
    object Data { get; set; }
}

[Serializable]
public struct BlueprintOperationPort<T> : IBlueprintOperationPort
{
    public string ID { get; set; }
    public T Data { get; set; }

    object IBlueprintOperationPort.Data
    {
        get => Data;
        set => Data = (T)value;
    }
}
*/

public abstract class BlueprintOperation
{
    public string OperationID { get; protected set; }

    public List<string> InputPorts { get; protected set; }
    public List<string> OutputPorts { get; protected set; }

    public abstract bool Execute();

    public abstract bool Undo();
}