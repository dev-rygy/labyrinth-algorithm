/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/23/2025
 * Last Modified:   01/26/2025 (Ryan)
 * Notes:           Holds all the path's in an area and the 
 *                      bounds that they may spawn rooms in
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    [CreateAssetMenu(fileName = "Path", menuName = "Scriptable Objects/Area", order = 1)]
    public class Area : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }

        [field: Header("Bounding Box")]
        [Tooltip("No rooms can spawn past this coordinate point.")]
        [field: SerializeField] public Vector3 LowerBound { get; set; }    // Lower bound; no rooms can spawn beyond this point
        [Tooltip("No rooms can spawn past this coordinate point.")]
        [field: SerializeField] public Vector3 UpperBound { get; set; }       // Upper bound; no rooms can spawn beyond this point

        [field: Header("Paths")]
        [field: SerializeField] public Path MainPath { get; set; }
        [field: SerializeField] public List<Path> Paths { get; set; }


    }
}
