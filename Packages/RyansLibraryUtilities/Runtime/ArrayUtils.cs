/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/02/2025
 * Last Modified:   10/02/2025 (Ryan)
 * Notes:           
*/
using System;

namespace RyansLibrary.Utilities
{
    public class ArrayUtils
    {
        /// <summary>
        /// Find the index that satisfies the predicate but start at the starting index
        /// </summary>
        /// <typeparam name="T">Any element type the array contains</typeparam>
        /// <param name="array">The array to search through</param>
        /// <param name="startIndex">The element to start at in the array</param>
        /// <param name="match">A predicate function to check for any sort of match</param>
        /// <returns>The index of the next element found</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static int FindIndexCircular<T>(T[] array, int startIndex, Predicate<T> match)
        {
            // Exceptions
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (match == null)
                throw new ArgumentNullException(nameof(match));
            if (array.Length == 0)
                return -1;
            if (startIndex < 0 || startIndex >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            int index = startIndex;
            int attempts = 0;
            while (attempts < array.Length)
            {
                if (match(array[index]))        // Check if the array element matches
                    return index;

                index = (index + 1) % array.Length;           // Circle back in array if end is reached
                attempts++;
            }

            return -1;      // If element was not found then return -1
        }
    }
}
