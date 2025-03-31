/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/23/2025
 * Last Modified:   03/25/2025 (Ryan)
 * Notes:           Holds all the path's in an area and the 
 *                      bounds that they may spawn rooms in
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    // Placement type of each Unique room
    public enum RoomPlacementType
    {
        Fixed,          // Fixed rooms have set placement
        Constrained,    // Constrained rooms can be placed anywhere in a specified bounded area
        Free            // Free rooms can be placed anywhere in the bounds of the area
    }

    // Room entrys for the main path algorithm
    [System.Serializable]
    public struct RoomEntry
    {
        [Header("General Room Parameters")]
        [SerializeField] public GameObject Prefab;
        [SerializeField] public RoomPlacementType PlacementType;
        
        // TODO: Convert parameters below to Vector3Int!
        [Header("Fixed Room Parameters")]
        [SerializeField] public Vector3Int SpawnPosition;      // For use with fixed rooms; all other rooms will ignore this
        [Header("Constrained Room Parameters")]
        [SerializeField] public BoundsInt Bounds;              // For use with constrained rooms; all other rooms will ignore these bounds;
    }

    [CreateAssetMenu(fileName = "Path", menuName = "Scriptable Objects/Area", order = 1)]
    public class Area : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }

        // TODO: Convert Bounding Box Parameters to Vector3Int!
        [field: Header("Bounding Box")]
        [Tooltip("No rooms can spawn outside of these bounds.")]
        [field: SerializeField] public BoundsInt Bounds { get; private set; }

        [field: Header("Main Path")]
        [field: SerializeField] public Path MainPath { get; set; }
        [field: SerializeField] public List<RoomEntry> UniqueRooms { get; private set; }        // Must be spawned no matter what
        [field: SerializeField] public int DivergentRoomsCellOccupancy { get; private set; }

        [field: Header("Alt Paths")]
        [field: SerializeField] public List<Path> Paths { get; set; }
    }
}
