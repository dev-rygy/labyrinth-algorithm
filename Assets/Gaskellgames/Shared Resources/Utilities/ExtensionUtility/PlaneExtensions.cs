using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    public static class PlaneExtensions
    {
        /// <summary>
        /// Get a plane at a given point (position) and two relative vectors (up and right)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="upVector"></param>
        /// <param name="rightVector"></param>
        /// <returns></returns>
        public static Plane GetPlaneFromPointAndRelativeVectors(Vector3 point, Vector3 upVector, Vector3 rightVector)
        {
            Vector3 pointA = point + upVector;
            Vector3 pointB = point - ((upVector * 0.5f) + (rightVector * 0.866f));
            Vector3 pointC = point - ((upVector * 0.5f) - (rightVector * 0.866f));

            return new Plane(pointA, pointB, pointC);
        }

    } // class end
}
