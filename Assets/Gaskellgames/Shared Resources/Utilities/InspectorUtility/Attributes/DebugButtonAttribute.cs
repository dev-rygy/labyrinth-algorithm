using System;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class DebugButtonAttribute : PropertyAttribute
    {
        public string buttonLabel;
        public string methodName;
        public string tooltip;
    
        public DebugButtonAttribute(string methodName)
        {
            this.methodName = methodName;
            buttonLabel = methodName;
            tooltip = "";
        }
    
        public DebugButtonAttribute(string methodName, string buttonLabel)
        {
            this.methodName = methodName;
            this.buttonLabel = buttonLabel;
            tooltip = "";
        }
    
        public DebugButtonAttribute(string methodName, string buttonLabel, string tooltip)
        {
            this.methodName = methodName;
            this.buttonLabel = buttonLabel;
            this.tooltip = tooltip;
        }
        
    } // class end
}
