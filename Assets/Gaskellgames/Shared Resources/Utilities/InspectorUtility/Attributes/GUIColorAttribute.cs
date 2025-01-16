using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public class GUIColorAttribute : PropertyAttribute
    {
        public enum Target
        {
            All,
            Content,
            Background
        }
        public Target target;
        
        public byte R;
        public byte G;
        public byte B;
        public byte A;
 
        public GUIColorAttribute()
        {
            R = 223;
            G = 179;
            B = 000;
            A = 255;
            target = Target.All;
        }
 
        public GUIColorAttribute(byte r = 000, byte g = 028, byte b = 045, byte a = 255, Target target = Target.All)
        {
            R = r;
            G = g;
            B = b;
            A = a;
            this.target = target;
        }
        
    } // class end
}
