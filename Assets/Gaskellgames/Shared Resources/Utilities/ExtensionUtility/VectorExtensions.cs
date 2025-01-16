using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    public static class VectorExtensions
    {
        public static Vector3 ClosestPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd,
            bool clampToLine = true)
        {
            Vector3 hypotenuse = point - lineStart;
            Vector3 lineDirection = (lineEnd - lineStart).normalized;
            float lineDistance = Vector3.Distance(lineStart, lineEnd);
            float angle = Vector3.Dot(lineDirection, hypotenuse);

            if (angle <= 0)
            {
                return lineStart;
            }

            if (angle >= lineDistance)
            {
                return lineEnd;
            }

            Vector3 distanceAlongLine = lineDirection * angle;
            Vector3 closestPoint = lineStart + distanceAlongLine;

            return closestPoint;
        }

        public static bool IsPointOnLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 lineDirection = lineEnd - lineStart;
            float lineLength = lineDirection.magnitude;
            float projectLengthA = Vector3.Dot(point - lineStart, lineDirection.normalized);
            float projectLengthB = Vector3.Dot(point - lineEnd, -lineDirection.normalized);

            return !(lineLength < projectLengthA || lineLength < projectLengthB);
        }

    } // class end
}
