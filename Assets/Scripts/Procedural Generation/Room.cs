/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   07/07/2026 (Ryan)
 * Notes:           Room data; some values set by the 
 *                  Map Generator and some values pre set
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    // Determines how much grid space a room will ocupy.
    public enum RoomShape
    {
        smallRoom,
        longRoom,
        tallRoom,
        bigRoom,
        lRoom,
        unique
    }

    // Determines its gameplay and purpose.
    public enum RoomType
    {
        general,
        start,
        end,
        prize,
        boss
    }

    // Determines the rotation of the room in the world.
    public enum RoomRotation
    {
        Deg0 = 0,
        Deg90 = 1,
        Deg180 = 2,
        Deg270 = 3,
    }

    // Determines the offset of the room in the world.
    public enum RoomShift
    {
        N = 0,
        S = 1,
        E = 2,
        W = 3,
        Up = 4,
        Down = 5
    }

    [Serializable]
    public struct RoomWall
    {
        [SerializeField] public Transform WallTransform;
        [SerializeField] public bool IsExemptFromMutation;
    }

    [Serializable]
    public struct RoomCell
    {
        [SerializeField] public Vector3Int Position;
        [SerializeField] public bool IsAvilable;
        [SerializeField] public List<RoomWall> Walls;
    }

    /// <summary>
    /// Runtime component on every room prefab. Doesn't decide anything about layout itself - RoomGenerator decides
    /// where/what to spawn, then calls CopyBlueprintEntranceFlags per underlying blueprint cell and Initialize() to
    /// actually open the correct doorways/close the correct walls on this specific prefab instance.
    /// </summary>
    public class Room : MonoBehaviour
    {
        private const int k_wallCount = 6;

        [Header("Room Components")]
        [SerializeField] private List<RoomCell> _roomCells;
        public List<RoomCell> RoomCells => _roomCells;
        //[SerializeField] private List<Transform> _roomWalls;
        //[field: SerializeField] public List<Vector3Int> AvailableCellData { get; private set; }
        [SerializeField] public List<SpawnPad> RoomSpawners;

        [Header("Room Properties")]
        [SerializeField] public RoomShape roomShape;
        [field: SerializeField] public Vector3Int RoomDimensions { get; private set; } = Vector3Int.one;
        [field: SerializeField] public RoomType RoomType { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool _debug = false;
        [SerializeField] private Color _roomBoundsColor = Color.orange;
        [SerializeField] private Color _availableCellColor = Color.green;
        [SerializeField] private Color _unavailableCellColor = Color.red;

        // [unit, face]: a merged room prefab (see RoomGenerator's bigRoom/tallRoom/longRoom shapes) can represent
        // up to 4 of the original 1x1x1 blueprint cells, each with its own 6 faces/walls - this is where those get
        // flattened into one prefab's worth of "which walls should be doors" before AcivateEntranceways() applies it.
        private bool[,] openEntranceways;

        private void Awake()
        {
            // up to 4 possible unit spaces a room can take up; 6 possible faces on each unit
            // Index 1 = Bot-Left Unit (Origin)
            // Index 2 = Bot-Right Unit
            // Index 3 = Top-Right Unit
            // Index 4 = Top-Left Unit
            openEntranceways = new bool[RoomCells.Count, k_wallCount];

            RoomType = RoomType.general;

            // TODO: Make some walls exempt to this for unique rooms especially
            ResetEntranceways();
        }

        // Initialize the Room's entrances and loot
        public void Initialize(RoomType type = RoomType.general)
        {
            RoomType = type;
            AcivateEntranceways();
        }

        /// <summary>
        /// Simply copy the bluePrint room's entranceway flags into the room's open entraceways.
        /// </summary>
        /// <param name="blueprintArray">The blueprint room's entranceway array (6 possible entrances)</param>
        /// <param name="unitIndex">A specific unit space of the room in question</param>
        public void CopyBlueprintEntranceFlags(bool[] blueprintArray, int unitIndex, RoomRotation rotation = RoomRotation.Deg0)
        {
            blueprintArray = RotateEntryFlag(blueprintArray, rotation);

            for (int i = 0; i < blueprintArray.Length; i++) // iterate through all six faces of the Blueprint's flag array
            {
                openEntranceways[unitIndex, i] = blueprintArray[i]; // Copy into room array respectively
            }
        }

        /// <summary>
        /// Apply a horizontal rotation to the blueprint entranceway flags so that they match the room's orientation in the world.
        /// Simply shift the values in the blueprint array around to handle a 90 degree rotation.
        /// </summary>
        /// <param name="entrypointFlagArray">The blueprint array</param>
        /// <param name="rotation">The angle of applied rotation</param>
        /// <returns></returns>
        private bool[] RotateEntryFlag(bool[] entrypointFlagArray, RoomRotation rotation)
        {
            if (rotation == RoomRotation.Deg0)        // If no rotation return original array
                return entrypointFlagArray;

            // A 90-degree yaw swaps which physical wall each blueprint face flag now points at (e.g. the wall that
            // used to face +Z now faces +X), so the flags have to be permuted to match and not just copied.
            bool[] rotatedArray = entrypointFlagArray;

            for (int i = 0; i < (int)rotation; i++)
            {
                rotatedArray = RotateEntryFlagHorizontal90(rotatedArray);
            }

            return rotatedArray;
        }

        private bool[] RotateEntryFlagHorizontal90(bool[] entrypointFlagArray)
        {
            // A 90-degree yaw swaps which physical wall each blueprint face flag now points at (e.g. the wall that
            // used to face +Z now faces +X), so the flags have to be permuted to match and not just copied.
            bool[] rotatedArray = new bool[entrypointFlagArray.Length];

            rotatedArray[0] = entrypointFlagArray[2];        // Positive X to Negative Z
            rotatedArray[1] = entrypointFlagArray[3];        // Negative X to Positive Z
            rotatedArray[2] = entrypointFlagArray[1];        // Positive Z direction the same
            rotatedArray[3] = entrypointFlagArray[0];        // Negative Z direction the same
            rotatedArray[4] = entrypointFlagArray[4];        // Positive Y to Positive X
            rotatedArray[5] = entrypointFlagArray[5];        // Negative Y to Negative X

            return rotatedArray;
        }


        /// <summary>
        /// When called will activate all entranceways that have been flagged as open in the openEntranceways array
        /// </summary>
        private void AcivateEntranceways()
        {
            for (int i = 0; i < _roomCells.Count; i++)     // iterate through all room cells
            {
                if (_roomCells[i].IsAvilable)
                {
                    for (int j = 0; j < k_wallCount; j++)     // iterate through the walls/faces of each unit
                    {
                        // Activate entrance if true in activeEntranceway List
                        if (openEntranceways[i, j] == true && !_roomCells[i].Walls[j].IsExemptFromMutation)
                        {
                            ActivateEntranceway(_roomCells[i].Walls[j]);
                        }
                    }
                }
            }
        }


        private void ResetEntranceways()
        {
            foreach (RoomCell cell in _roomCells)
            {
                if (cell.IsAvilable)
                {
                    foreach (RoomWall wall in cell.Walls)
                    {
                        if (!wall.IsExemptFromMutation)
                            DeactivateEntranceway(wall);
                    }
                }
            }
        }

        private void ActivateEntranceway(RoomWall wall)
        {
            wall.WallTransform.GetChild(0).gameObject.SetActive(true);   // Activate Entranceway
            wall.WallTransform.GetChild(1).gameObject.SetActive(false);  // Deactivate Wall
        }

        private void DeactivateEntranceway(RoomWall wall)
        {
            wall.WallTransform.GetChild(0).gameObject.SetActive(false);  // Deactivate Entranceway
            wall.WallTransform.GetChild(1).gameObject.SetActive(true);   // Activate Wall
        }

        public float GetRoomOccupancy()
        {
            return Math.RectangularVolume(RoomDimensions);
        }

        private void OnDrawGizmos()
        {
            if (!_debug)
                return;

            DrawDimensions();
            DrawAvailableCells();
        }

        private void DrawDimensions()
        {
            // TODO: Replace 13 with scale factor from MapGeneratorController
            int scaleFactor = 13; // This is a temporary scale factor for visualization purposes. Adjust as needed.
            Vector3 roomOffset = new Vector3(1, 0, 1) * (scaleFactor * 0.5f);
            Vector3 center = transform.position + -(roomOffset) + (Vector3)(RoomDimensions * scaleFactor) / 2f;

            Gizmos.color = _roomBoundsColor;
            Gizmos.DrawWireCube(center, RoomDimensions * 13);
            // Gizmos.DrawSphere(center, 0.5f);
        }

        private void DrawAvailableCells()
        {
            foreach (RoomCell cell in _roomCells)
            {
                Vector3 cellWorldPos = transform.position + (13f * 0.5f) * Vector3.up + (Vector3)cell.Position * 13; // Assuming each cell is 13 units apart

                if (cell.IsAvilable)
                    Gizmos.color = _availableCellColor;
                else
                    Gizmos.color = _unavailableCellColor;
                Gizmos.DrawWireCube(cellWorldPos, Vector3.one * 13);
            }
        }
    }
}
