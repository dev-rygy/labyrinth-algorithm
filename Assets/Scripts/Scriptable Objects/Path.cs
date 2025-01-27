/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/20/2025
 * Last Modified:   01/26/2025 (Ryan)
 * Notes:           Path in a scriptable object
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public enum PathType
    {
        master,
        main,
        prize
    }

    [System.Serializable]
    public struct PathEntry
    {
        public GameObject Prefab;
        [Range(0, 100)] public int Probability;
    }

    [CreateAssetMenu(fileName = "Path", menuName = "Scriptable Objects/Path", order = 2)]
    public class Path : ScriptableObject
    {
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public PathType Type { get; private set; }
        [field: SerializeField] public int PathLength { get; private set; }
        [field: SerializeField] public List<PathEntry> startingRooms { get; private set; }
        [field: SerializeField] public List<PathEntry> rooms1x1x1 { get; private set; }
        [field: SerializeField] public List<PathEntry> rooms2x1x1 { get; private set; }
        [field: SerializeField] public List<PathEntry> rooms1x2x1 { get; private set; }
        [field: SerializeField] public List<PathEntry> rooms2x1x2 { get; private set; }
        [field: SerializeField] public List<PathEntry> endingRooms { get; private set; }
            
        [field: Header("Room Generation Chance")]
        [Tooltip("The percent chance for a room with a tall shape to spawn when the conditions are met.")]
        [field: SerializeField][field: Range(0, 1)] public float TallRoomSpawnChance { get; private set; } = 0;          // The spawn chance of tall rooms
        [Tooltip("The percent chance for a room with a long shape to spawn when the conditions are met.")]
        [field: SerializeField][field: Range(0, 1)] public float LongRoomSpawnChance { get; private set; } = 0;          // The spawn chance of long rooms
        [Tooltip("The percent chance for a room with a big shape to spawn when the conditions are met.")]
        [field: SerializeField][field: Range(0, 1)] public float BigRoomSpawnChance { get; private set; } = 0;           // The spawn chance of big rooms 

        public List<BlueprintRoom> BlueprintRooms { get; private set; }
        public List<Room> Rooms { get; private set; }
        public int startMasterIdx { get; set; }  // Start index in master path
        public int endMasterIdx { get; set; }    // End index in master path

        // Constructor for path; gets it's start and end index in the master path
        public void Initialize(int startIdx, int endIdx)
        {
            BlueprintRooms = new List<BlueprintRoom>();
            Rooms = new List<Room>();

            startMasterIdx = startIdx;
            endMasterIdx = endIdx;
        }

        /// <summary>
        /// Return the number of blueprint rooms along this path.
        /// </summary>
        public int BlueprintCount()
        {
            return BlueprintRooms.Count;
        }

        /// <summary>
        /// Return the number of rooms along this path.
        /// </summary>
        public int RoomCount()
        {
            return Rooms.Count;
        }

        /// <summary>
        /// Add a blueprint room to the path.
        /// </summary>
        public void Add(BlueprintRoom room)
        {
            BlueprintRooms.Add(room);
        }

        /// <summary>
        /// Add a room to the path.
        /// </summary>
        public void Add(Room room)
        {
            Rooms.Add(room);
        }

        /// <summary>
        /// Clear the referenced blueprint rooms in this path.
        /// Warning: Dangerous unless you know what you're doing
        /// </summary>
        public void ClearBluePrintRooms()
        {
            BlueprintRooms.Clear();
        }
    }
}