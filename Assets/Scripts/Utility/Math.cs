/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2024
 * Last Modified:   04/04/2024 (Ryan) 
 * Notes:           Custom Math Library
*/
using UnityEngine;

namespace RyansLibrary
{
    public static class Math
    {
        #region Arithmetic
        /// <summary>
        /// Checks if a number is even or odd.
        /// </summary>
        /// <param name="n">Number to check</param>
        /// <returns>True if the argument passed in is an even number.</returns>
        public static bool IsEven(int n)
        {
            return (n % 2 == 0);
        }

        /// <summary>
        /// Squares a number
        /// </summary>
        /// <param name="n">Number</param>
        /// <returns>Squared result</returns>
        public static float Squared(float n)
        {
            return n * n;
        }

        /// <summary>
        /// Squares a number
        /// </summary>
        /// <param name="n">Number</param>
        /// <returns>Squared result</returns>
        public static int Squared(int n)
        {
            return n * n;
        }
        #endregion

        #region Geometric
        /// <summary>
        /// Finds the Euclideon Distance between two points.
        /// https://en.wikipedia.org/wiki/Euclidean_distance
        /// </summary>
        /// <param name="a">Point 1</param>
        /// <param name="b">Point 2</param>
        /// <returns>Distance</returns>
        public static float EuclideonDistance3D(Vector3 a, Vector3 b)
        {
            // D(a, b) = sqrt[ (x_2 - x_1)^2 + (y_2 - y_1)^2 + (z_2 - z_1)^2 ]
            return Vector3.Distance(a, b);
        }

        /// <summary>
        /// Finds the Manhattan Distance between two points.
        /// https://xlinux.nist.gov/dads/HTML/manhattanDistance.html
        /// </summary>
        /// <param name="a">Point 1</param>
        /// <param name="b">Point 2</param>
        /// <returns>Distance</returns>
        public static float ManhattanDistance3D(Vector3 a, Vector3 b)
        {
            // D(a, b) = | x_2 - x_1 | + | y_2 - y_1 | + | z_2 - z_1 |
            return Mathf.Abs(b.x - a.x) + Mathf.Abs(b.y - a.y) + Mathf.Abs(b.z - a.z);
        }

        /// <summary>
        /// Finds the Chebyshev Distance between two points.
        /// https://en.wikipedia.org/wiki/Chebyshev_distance
        /// </summary>
        /// <param name="a">Point 1</param>
        /// <param name="b">Point 2</param>
        /// <returns>Distance</returns>
        public static float ChebyshevDistance3D(Vector3 a, Vector3 b)
        {
            // D(a, b) = max[ | x_2 - x_1 |, | y_2 - y_1 |, | z_2 - z_1 | ]
            return Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), Mathf.Abs(b.z - a.z));
        }
        #endregion

        #region Volume
        /// <summary>
        /// Find the Volume of a cube.
        /// </summary>
        /// <param name="length">Cube side length.</param>
        /// <returns>A float volume</returns>
        public static float CubicVolume(float length)
        {
            return Mathf.Pow(length, 3);
        }

        /// <summary>
        /// Find the volume of a rectangular prism, Component Based
        /// </summary>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>A float volume</returns>
        public static float RectangularVolume(float length, float width, float height)
        {
            return length * width * height;
        }

        /// <summary>
        /// Find the volume of a rectangular prism, Vector Based
        /// </summary>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>A float volume</returns>
        public static float RectangularVolume(Vector3 dimensions)
        {
            return dimensions.x * dimensions.y * dimensions.z;
        }
        #endregion
    }
}
