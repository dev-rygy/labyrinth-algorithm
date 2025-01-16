using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public class LabelOverrideAttribute : PropertyAttribute
    {
        public string label;

        public LabelOverrideAttribute(string label)
        {
            this.label = label;
        }
        
    } // class end
}
