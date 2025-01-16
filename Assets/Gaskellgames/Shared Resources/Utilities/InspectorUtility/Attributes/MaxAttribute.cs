using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public class MaxAttribute : PropertyAttribute
    {
        public float max;

        public MaxAttribute(float max)
        {
            this.max = max;
        }

    } // class end
}
