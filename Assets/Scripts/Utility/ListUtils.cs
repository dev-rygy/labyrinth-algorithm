/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/23/2025
 * Last Modified:   10/23/2025 (Ryan)
 * Notes:           
*/
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;      // Use Unity Engine's Random not System.Collection's Random

namespace RyansLibrary.Utils
{
    public class ListUtils
    {
        public static List<T> Union<T>(List<T> A, List<T> B)
        {
            return A.Union(B).ToList();
        }

        public static List<T> Intersection<T>(List<T> A, List<T> B)
        {
            return A.Intersect(B).ToList();
        }

        public static List<T> Difference<T>(List<T> A, List<T> B)
        {
            return A.Except(B).ToList();
        }

        public static T SelectRandomElement<T>(List<T> list)
        {
            int randomIndex = Random.Range(0, list.Count);
            return list[randomIndex];
        }

        public static List<T> SelectRandomSet<T>(List<T> list, int elementCount)
        {
            List<T> selectedElements = new List<T>();
            List<T> availableElements = new List<T>(list);

            if (elementCount > availableElements.Count)
            {
                Debug.LogError("Amount of random element selections desired exceeds the amount of elements available.");
                return null;
            }

            // Choose random edges from the graph with none of the MST edges to add varience to the map
            for (int i = 0; i < elementCount; i++)
            {
                int randomIndex = Random.Range(0, availableElements.Count);
                T selectedElement = availableElements[randomIndex];

                selectedElements.Add(selectedElement);            // Add selected edge to result
                availableElements.Remove(selectedElement);        // Remove selected edge from pool so it does not get selected again
            }

            return selectedElements;
        }
    }
}
