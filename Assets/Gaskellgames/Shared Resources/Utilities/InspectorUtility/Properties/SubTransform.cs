using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [System.Serializable]
    public class SubTransform
    {
        public Vector3 position;
        public Vector3 eulerAngles;

        public SubTransform()
        {
            position = Vector3.zero;
            eulerAngles = Vector3.zero;
        }

        public SubTransform(Vector3 Position, Vector3 EulerAngles)
        {
            position = Position;
            eulerAngles = EulerAngles;
        }

    } // class end
}
