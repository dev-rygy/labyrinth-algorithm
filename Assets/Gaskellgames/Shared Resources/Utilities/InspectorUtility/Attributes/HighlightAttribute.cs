using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    public class HighlightAttribute : PropertyAttribute
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
        
        public HighlightAttribute()
        {
            R = 255;
            G = 000;
            B = 000;
            A = 255;
        }
        
        public HighlightAttribute(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    } // class end
}
