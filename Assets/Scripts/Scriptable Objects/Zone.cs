/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/23/2025
 * Last Modified:   10/23/2025 (Ryan)
 * Notes:           Holds all the path's in an zone and the 
 *                      bounds that they may spawn rooms in
*/
using System.Collections.Generic;
using UnityEngine;

using RyansLibrary.AI;

namespace RyansLibrary.Labyrinth
{
    // Placement type of each Unique room
    public enum RoomPlacementType
    {
        Fixed,          // Fixed rooms have set placement
        Constrained,    // Constrained rooms can be placed anywhere in a specified bounded zone
        Free            // Free rooms can be placed anywhere in the bounds of the zone
    }

    // Room entrys for the main path algorithm
    [System.Serializable]
    public class RoomEntry
    {
        [field: Header("General Room Parameters")]
        [field: SerializeField] public GameObject Prefab { get; set; }
        [field: SerializeField] public RoomPlacementType PlacementType { get; set; }

        [field: Header("Fixed Room Parameters")]
        [field: SerializeField] public Vector3Int SpawnPosition { get; set; }               // For use with fixed rooms; all other rooms will ignore this
        [field: SerializeField] public List<Vector3Int> AvailableCells { get; set; }        // List of cells the room gen algorithm can pathfind to; null = all rooms

        [field: Header("Constrained Room Parameters")]
        [field: SerializeField] public BoundsInt Bounds { get; set; }                       // For use with constrained rooms; all other rooms will ignore these bounds
    }

    [CreateAssetMenu(fileName = "Path", menuName = "Scriptable Objects/Zone", order = 1)]
    public class Zone : ScriptableObject
    {
        [field: Header("General Settings")]
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public Heuristic DefaultPathfindingHeuristic { get; private set; }

        // TODO: Convert Bounding Box Parameters to Vector3Int!
        [field: Header("Bounding Box")]
        [Tooltip("No rooms can spawn outside of these bounds.")]
        [field: SerializeField] public BoundsInt Bounds { get; set; }

        [field: Header("Main Path")]
        [field: SerializeField] public Path MainPath { get; set; }
        [field: SerializeField] public List<RoomEntry> UniqueRooms { get; private set; }        // Must be spawned no matter what
        [field: SerializeField] public int DivergentRoomsCellOccupancy { get; private set; }
        [field: SerializeField] public int RandomCyclesInGraph { get; private set; }

        [field: Header("Alt Paths")]
        [field: SerializeField] public List<Path> Paths { get; set; }
    }
}
