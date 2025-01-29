/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/23/2025
 * Last Modified:   01/28/2025 (Ryan)
 * Notes:           Holds all the path's in an area and the 
 *                      bounds that they may spawn rooms in
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    [CreateAssetMenu(fileName = "Path", menuName = "Scriptable Objects/Area", order = 1)]
    public class Area : ScriptableObject
    {
        public enum RoomPlacementType
        {
            Static,     // Static rooms have a set placement
            Dynamic,
            Kinematic
        }

        [System.Serializable]
        public struct RoomEntry
        {
            [SerializeField] public GameObject Prefab;
            [SerializeField] public RoomPlacementType Type;
            [Header("Static Room Parameters")]
            [SerializeField] public Vector3 SpawnPosition;      // For use with static rooms; all other rooms will ignore this
            [Header("Kinematic Room Parameters")]
            [SerializeField] public Vector3 UpperBound;         // For use with kinematic rooms; all other rooms will ignore these bounds;
            [SerializeField] public Vector3 LowerBound;             // Upper and lower bound must be within the range of the area bounds
        }

        [field: SerializeField] public string Name { get; private set; }

        [field: Header("Bounding Box")]
        [Tooltip("No rooms can spawn past this coordinate point.")]
        [field: SerializeField] public Vector3 LowerBound { get; set; }    // Lower bound; no rooms can spawn beyond this point
        [Tooltip("No rooms can spawn past this coordinate point.")]
        [field: SerializeField] public Vector3 UpperBound { get; set; }       // Upper bound; no rooms can spawn beyond this point

        [field: Header("Main Path")]
        [field: SerializeField] public Path MainPath { get; set; }
        [field: SerializeField] public List<RoomEntry> UniqueRooms { get; set; }

        [field: Header("Alt Paths")]
        [field: SerializeField] public List<Path> Paths { get; set; }


    }
}
