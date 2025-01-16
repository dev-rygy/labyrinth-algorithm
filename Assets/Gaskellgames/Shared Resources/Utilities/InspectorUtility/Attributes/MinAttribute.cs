using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public class MinAttribute : PropertyAttribute
    {
        public float min;

        public MinAttribute(float min)
        {
            this.min = min;
        }

    } // class end
}
