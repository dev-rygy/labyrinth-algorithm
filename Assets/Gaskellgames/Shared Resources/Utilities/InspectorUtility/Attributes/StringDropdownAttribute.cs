using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public class StringDropdownAttribute : PropertyAttribute
    {
        public string[] list { get; private set; }
        
        public StringDropdownAttribute(params string[] list)
        {
            this.list = list;
        }
        
    } // class end
}
