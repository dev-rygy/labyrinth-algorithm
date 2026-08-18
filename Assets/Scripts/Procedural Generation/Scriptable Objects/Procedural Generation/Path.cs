/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/20/2025
 * Last Modified:   10/23/2025 (Ryan)
 * Notes:           Path in a scriptable object
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public enum PathType
    {
        main,
        prize,
        master
    }

    [System.Serializable]
    public struct PathEntry
    {
        [SerializeField] public GameObject Prefab;
        [SerializeField] [Range(0, 100)] public int Probability;
    }

    /// <summary>
    /// Designer-facing configuration + runtime storage for one corridor of the labyrinth (the zone's main path, or
    /// one of its side/alt paths). Doubles as both: the SerializeField settings above (room-shape prefab pools,
    /// spawn chances, desired length) are read while the BlueprintOperation graph is being built, while
    /// BlueprintList/Rooms are populated during Execute() as generation actually runs (see DrunkardWalkBlueprintOp,
    /// which appends to BlueprintList, and RoomGenerator.ParsePathAndGenerateRooms, which appends to Rooms).
    /// Must have Initialize() called before generation starts and IsInitialized checked before use, since a Path
    /// asset can be shared/reused across multiple generation runs.
    /// </summary>
    [CreateAssetMenu(fileName = "Path", menuName = "Scriptable Objects/Procedural Generation/Path", order = 2)]
    public class Path : ScriptableObject
    {
        // Editor Fields
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public PathType Type { get; private set; }
        [field: SerializeField] public int DesiredPathLength { get; private set; }
        [field: SerializeField] public List<PathEntry> startingRooms { get; private set; }
        [field: SerializeField] public List<PathEntry> rooms1x1x1 { get; private set; }
        [field: SerializeField] public List<PathEntry> rooms2x1x1 { get; private set; }
        [field: SerializeField] public List<PathEntry> rooms1x2x1 { get; private set; }
        [field: SerializeField] public List<PathEntry> rooms2x1x2 { get; private set; }
        [field: SerializeField] public List<PathEntry> rooms2x1x2l { get; private set; }
        [field: SerializeField] public List<PathEntry> endingRooms { get; private set; }

        [field: SerializeField] public bool DrunkardWalkCanGoVertical { get; private set; } = true;


        [field: Header("Room Generation Chance")]
        [Tooltip("The percent chance for a room with a tall shape to spawn when the conditions are met.")]
        [field: SerializeField][field: Range(0, 1)] public float TallRoomSpawnChance { get; private set; } = 0;          // The spawn chance of tall rooms
        [Tooltip("The percent chance for a room with a long shape to spawn when the conditions are met.")]
        [field: SerializeField][field: Range(0, 1)] public float LongRoomSpawnChance { get; private set; } = 0;          // The spawn chance of long rooms
        [Tooltip("The percent chance for a room with a big shape to spawn when the conditions are met.")]
        [field: SerializeField][field: Range(0, 1)] public float BigRoomSpawnChance { get; private set; } = 0;           // The spawn chance of big rooms 
        [Tooltip("The percent chance for a room with an l-shape to spawn when the conditions are met.")]
        [field: SerializeField][field: Range(0, 1)] public float LRoomSpawnChance { get; private set; } = 0;           // The spawn chance of l rooms

        [field: Header("Debug")]
        [field: SerializeField] public Color PathGizmoColor;

        // Data Storage 
        public List<Blueprint> BlueprintList { get; private set; }
        public List<Room> Rooms { get; private set; }
        
        // UNUSED
        // public int startMasterIdx { get; set; }  // Start index in master path
        // ublic int endMasterIdx { get; set; }    // End index in master path

        public bool IsInitialized => CheckInitialize();

        // Constructor for path; gets it's start and end index in the master path
        public void Initialize()
        {
            BlueprintList = new List<Blueprint>();
            Rooms = new List<Room>();

            // startMasterIdx = startIdx;
            // endMasterIdx = endIdx;
        }

        private bool CheckInitialize()
        {
            if (BlueprintList != null && Rooms != null)
                return true;

            return false;
        }

        /// <summary>
        /// Return the number of blueprint rooms along this path.
        /// </summary>
        public int BlueprintCount()
        {
            return BlueprintList.Count;
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
        public void Add(Blueprint blueprint)
        {
            BlueprintList.Add(blueprint);
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
        public void ClearBlueprints()
        {
            BlueprintList.Clear();
        }
    }
}