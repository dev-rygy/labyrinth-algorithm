using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public class RangeMinMaxAttribute : PropertyAttribute
    {
        public float min;
        public float max;
        public string minLabel;
        public string maxLabel;
        public bool subLabels;
        
        public RangeMinMaxAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
            subLabels = false;
            minLabel = "";
            maxLabel = "";
        }
        
        public RangeMinMaxAttribute(float min, float max, bool subLabels)
        {
            this.min = min;
            this.max = max;
            this.subLabels = subLabels;
            minLabel = MathUtility.RoundFloat(min, 3).ToString();
            maxLabel = MathUtility.RoundFloat(max, 3).ToString();
        }

        public RangeMinMaxAttribute(float min, float max, string minLabel, string maxLabel)
        {
            this.min = min;
            this.max = max;
            subLabels = true;
            this.minLabel = minLabel;
            this.maxLabel = maxLabel;
        }
        
    } // class end
}
