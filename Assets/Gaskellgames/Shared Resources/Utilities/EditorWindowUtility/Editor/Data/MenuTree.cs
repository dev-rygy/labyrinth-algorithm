#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [System.Serializable]
    public class MenuTree
    {
        public string header = "";
        public List<string> pages;
        public int selectionIndex = 0;
        public Color underlineColor = new Color32(179, 179, 179, 255);
        
    } // class end
}

#endif