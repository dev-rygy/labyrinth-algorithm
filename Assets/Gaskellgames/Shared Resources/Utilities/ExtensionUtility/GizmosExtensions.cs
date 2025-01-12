using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    public static class GizmosExtensions
    {
        /// <summary>
        /// Draws an arrow from the origin in a direction, with a set magnitude. The head's size is based on a multiplier of the arrows magnitude (length)
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="dir"></param>
        /// <param name="anglesRange"></param>
        /// <param name="radius"></param>
        /// <param name="maxSteps"></param>
        public static void DrawWireArc(Vector3 origin, Vector3 dir, float anglesRange, float radius, float maxSteps = 20)
        {
            var srcAngles = GetAnglesFromDir(origin, dir);
            var initialPos = origin;
            var posA = initialPos;
            var stepAngles = anglesRange / maxSteps;
            var angle = srcAngles - anglesRange / 2;
            for (var i = 0; i <= maxSteps; i++)
            {
                var rad = Mathf.Deg2Rad * angle;
                var posB = initialPos;
                posB += new Vector3(radius * Mathf.Cos(rad), 0, radius * Mathf.Sin(rad));

                Gizmos.DrawLine(posA, posB);

                angle += stepAngles;
                posA = posB;
            }

            Gizmos.DrawLine(posA, initialPos);
        }

        private static float GetAnglesFromDir(Vector3 position, Vector3 dir)
        {
            var forwardLimitPos = position + dir;
            var srcAngles = Mathf.Rad2Deg * Mathf.Atan2(forwardLimitPos.z - position.z, forwardLimitPos.x - position.x);

            return srcAngles;
        }

        /// <summary>
        /// Draws an arrow from the origin in a direction, with a set magnitude. The head's size is based on a multiplier of the arrows magnitude (length)
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="direction"></param>
        /// <param name="magnitude"></param>
        /// <param name="headMultiplier"></param>
        /// <param name="arrowHeadAngle"></param>
        public static void DrawWireArrow(Vector3 origin, Vector3 direction, float magnitude = 1f,
            float headMultiplier = 0.25f,
            float arrowHeadAngle = 30.0f)
        {
            // arrow body
            Vector3 normalisedDirection = direction.normalized;
            Vector3 arrowTip = origin + (normalisedDirection * magnitude);
            Gizmos.DrawLine(origin, arrowTip);

            // arrow head
            Vector3 leftDirection = (Quaternion.LookRotation(normalisedDirection) *
                                     Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1)).normalized;
            Vector3 rightDirection = (Quaternion.LookRotation(normalisedDirection) *
                                      Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1)).normalized;
            Vector3 arrowTipLeft = arrowTip + (leftDirection * magnitude * headMultiplier);
            Vector3 arrowTipRight = arrowTip + (rightDirection * magnitude * headMultiplier);
            Gizmos.DrawLine(arrowTip, arrowTipLeft);
            Gizmos.DrawLine(arrowTip, arrowTipRight);
        }

        /// <summary>
        /// Draws a plane at a given point (position) and two relative vectors (up and right)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="up"></param>
        /// <param name="right"></param>
        /// <param name="size"></param>
        /// <param name="lines"></param>
        public static void DrawWirePlane(Vector3 point, Vector3 up, Vector3 right, Vector2 size, int lines = 3)
        {
            // calculate scale
            Vector3 scaleX = Vector3.one * size.x / (lines * 2.0f);
            Vector3 scaleY = Vector3.one * size.y / (lines * 2.0f);

            // calculate gaps between lines
            Vector3 lineGapX = Vector3.Scale(right, scaleX);
            Vector3 lineGapY = Vector3.Scale(up, scaleY);

            // calculate base start and end point for lines
            Vector3 startX = point - (lineGapY * lines);
            Vector3 endX = point + (lineGapY * lines);
            Vector3 startY = point - (lineGapX * lines);
            Vector3 endY = point + (lineGapX * lines);

            // draw lines in ...
            for (int i = -lines; i <= lines; i++)
            {
                // line gaps
                Vector3 xGap = lineGapX * i;
                Vector3 yGap = lineGapY * i;

                // ... X axis
                Gizmos.DrawLine(startX + xGap, endX + xGap);

                // ... Y axis
                Gizmos.DrawLine(startY + yGap, endY + yGap);
            }
        }

        /// <summary>
        /// Draws a line from a point to the normal intersection point between the line and the point.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="lineStart"></param>
        /// <param name="lineEnd"></param>
        public static void DrawPointLineIntersection(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 intersectionPoint = VectorExtensions.ClosestPointOnLine(point, lineStart, lineEnd);
            Gizmos.DrawLine(intersectionPoint, point);
        }

        /// <summary>
        /// Draws a line from a point to the normal intersection point between the plane and the point.
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="point"></param>
        public static void DrawPointPlaneIntersection(Vector3 point, Plane plane)
        {
            Vector3 closestPoint = plane.ClosestPointOnPlane(point);
            Gizmos.DrawLine(point, closestPoint);
        }

    } // class end
}