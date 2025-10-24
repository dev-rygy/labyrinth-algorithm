using System;
using UnityEditor;
using UnityEngine;
using Unity.GraphToolkit.Editor;

[Serializable]
class MySimpleNode : Node 
{
    protected override void OnDefinePorts(IPortDefinitionContext context)
    {
        context.AddInputPort<float>("Input").Build();
        context.AddOutputPort<float>("Output").Build();
    }
}
