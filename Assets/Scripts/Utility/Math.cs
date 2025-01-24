/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/26/2024
 * Last Modified:   10/26/2024 
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
        /// Find the volume of a rectangular prism
        /// </summary>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>A float volume</returns>
        public static float RectangularVolume(float length, float width, float height)
        {
            return length * width * height;
        }
    }
}
