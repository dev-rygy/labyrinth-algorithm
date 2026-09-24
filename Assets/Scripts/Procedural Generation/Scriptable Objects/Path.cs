/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/20/2025
 * Last Modified:   09/17/2026 (Ryan)
 * Notes:           Path in a scriptable object
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public enum PathType
    {
        Main,
        Prize,
        Connection,
        Master
    }

    [Serializable]
    public struct RoomEntry : IWeighted
    {
        [SerializeField] private GameObject _prefab;
        public GameObject Prefab => _prefab;
        [SerializeField] private int _weight;
        public int Weight => _weight;
    }

    [Serializable]
    public struct ShapeEntry : IWeighted
    {
        [SerializeField] private ShapeData _roomShape;
        public ShapeData RoomShape => _roomShape;
        [SerializeField] private int _weight;
        public int Weight => _weight;
        [SerializeField] private List<RoomEntry> _rooms;
        public List<RoomEntry> Rooms => _rooms;
    }

    /// <summary>
    /// Designer-facing configuration + runtime storage for one corridor of the labyrinth (the zone's main path, or
    /// one of its side/alt paths). Doubles as both: the SerializeField settings above (room-shape prefab pools,
    /// spawn chances, desired length) are read while the BlueprintOperation graph is being built, while
    /// BlueprintList/_rooms are populated during Execute() as generation actually runs (see DrunkardWalkBlueprintOp,
    /// which appends to BlueprintList, and RoomGenerator.ParsePathAndGenerateRooms, which appends to _rooms).
    /// Must have Initialize() called before generation starts and IsInitialized checked before use, since a Path
    /// asset can be shared/reused across multiple generation runs.
    /// </summary>
    [CreateAssetMenu(fileName = "Path", menuName = "Scriptable Objects/Procedural Generation/Path", order = 2)]
    public class Path : ScriptableObject
    {
        // Editor Fields
        [SerializeField] private string _name = "New Path";
        public string Name => _name;
        [SerializeField] private PathType _type;
        public PathType Type => _type;

        // TODO: Remove these fields once the map generator graph is implemented.
        [field: SerializeField] public int DesiredPathLength { get; private set; }
        [field: SerializeField] public bool DrunkardWalkCanGoVertical { get; private set; } = true;

        [Header("Shapes & Rooms")]
        [field: SerializeField] public List<ShapeEntry> RoomShapes { get; private set; }

        [field: Header("Debug")]
        [field: SerializeField] public Color PathGizmoColor;

        // Storage
        private List<Blueprint> _blueprintList;
        public List<Blueprint> BlueprintList => _blueprintList;
        private List<Room> _rooms;
        public List<Room> Rooms => _rooms;

        // Return the number of blueprint rooms along this path.
        public int BlueprintCount => _blueprintList.Count;
        // Return the number of rooms along this path.
        public int RoomCount => _rooms.Count;

        public bool IsInitialized
        {
            get
            {
                if (BlueprintList != null && Rooms != null)
                    return true;

                return false;
            }
        }

        // Constructor for path; gets it's start and end index in the master path
        public void Initialize()
        {
            _blueprintList = new List<Blueprint>();
            _rooms = new List<Room>();

            // startMasterIdx = startIdx;
            // endMasterIdx = endIdx;
        }

        /// <summary>
        /// AddBlueprint a blueprint room to the path.
        /// </summary>
        public void AddBlueprint(Blueprint blueprint)
        {
            BlueprintList.Add(blueprint);
        }

        /// <summary>
        /// AddBlueprint a room to the path.
        /// </summary>
        public void AddRoom(Room room)
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

        /// <summary>
        /// Clear the referenced rooms in this path.
        /// Warning: Dangerous unless you know what you're doing
        /// </summary>
        public void ClearRooms()
        {
            Rooms.Clear();
        }
    }
}