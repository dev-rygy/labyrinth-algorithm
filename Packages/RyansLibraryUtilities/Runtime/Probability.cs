/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/23/2024
 * Last Modified:   07/27/2026 (Ryan)
 * Notes:           Custom Probability Library
*/
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Utilities
{
    [System.Serializable]
    public struct ProbabilityEntry<T>
    {
        public int Probability;
        public T Object;
    }

    public static class Probability<T>
    {
        public static ProbabilityEntry<T> ChooseRandomFromWeights(List<ProbabilityEntry<T>> entries)
        {
            // If the path's room entry list contains no room return null
            if (entries.Count <= 0)
            {
                Debug.LogError("Probability of room weights failed, room list empty.");
                return entries[0];
            }

            // If the path's room entry list contains one room return that room's prefab
            if (entries.Count == 1)
                return entries[0];

            // Choose a random room prefab based on probability
            int totalWeight = 0;
            foreach (ProbabilityEntry<T> entry in entries)
            {
                totalWeight += entry.Probability;
            }

            int roll = Random.Range(0, totalWeight + 1);        // roll 1 - 101; max exclusive
            int runningTotal = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                runningTotal += entries[i].Probability;
                if (roll <= runningTotal)
                    return entries[i];
            }

            Debug.LogError("Probability of room weights failed, unknown error.");
            return entries[0];
        }
    }
}
