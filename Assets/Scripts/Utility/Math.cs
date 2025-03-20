/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2024
 * Last Modified:   03/19/2024 
 * Notes:           Custom Math Library
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary
{
    public static class Math
    {
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


        /// <summary>
        /// Checks if a number is even or odd.
        /// </summary>
        /// <param name="n">Number to check</param>
        /// <returns>True if the argument passed in is an even number.</returns>
        public static bool IsEven(int n)
        {
            return (n % 2 == 0);
        }
    }
}
