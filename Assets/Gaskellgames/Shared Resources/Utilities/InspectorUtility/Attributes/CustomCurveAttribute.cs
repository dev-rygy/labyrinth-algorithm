using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public class CustomCurveAttribute : PropertyAttribute
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
 
        public CustomCurveAttribute()
        {
            R = 050;
            G = 179;
            B = 000;
            A = 255;
        }
 
        public CustomCurveAttribute(byte r = 000, byte g = 028, byte b = 045, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
        
    } // class end
}
