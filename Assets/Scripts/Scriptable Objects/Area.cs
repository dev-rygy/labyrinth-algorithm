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
