/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   07/07/2026 (Ryan)
 * Notes:           Room data; some values set by the 
 *                  Map Generator and some values pre set
*/
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

    public enum RoomRotation
    {
        Deg0 = 0,
        Deg90 = 1,
        Deg180 = 2,
        Deg270 = 3,
    }

    /// <summary>
    /// Runtime component on every room prefab. Doesn't decide anything about layout itself - RoomGenerator decides
    /// where/what to spawn, then calls CopyBlueprintEntranceFlags per underlying blueprint cell and Initialize() to
    /// actually open the correct doorways/close the correct walls on this specific prefab instance.
    /// </summary>
    public class Room : MonoBehaviour
    {
        [Header("Room Components")]
        [SerializeField] private List<Transform> _roomWalls;
        [field: SerializeField] public List<Vector3Int> AvailableCellData { get; private set; }
        [SerializeField] public List<SpawnPad> RoomSpawners;

        [Header("Room Properties")]
        [SerializeField] public RoomShape roomShape;
        [field: SerializeField] public Vector3Int RoomDimensions { get; private set; } = Vector3Int.one;
        [field: SerializeField] public RoomType RoomType { get; private set; }

        [Header("Debug")]
        [SerializeField] private bool _debug = false;
        [SerializeField] private Color _roomBoundsColor = Color.red;
        [SerializeField] private Color _availableCellColor = Color.green;

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
            openEntranceways = new bool[4, 6];

            RoomType = RoomType.general;
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
            {
                if (_debug) Debug.Log($"Room {gameObject.name} was not rotated.");
                return entrypointFlagArray;
            }

            // A 90-degree yaw swaps which physical wall each blueprint face flag now points at (e.g. the wall that
            // used to face +Z now faces +X), so the flags have to be permuted to match and not just copied.
            bool[] rotatedArray = entrypointFlagArray;

            for (int i = 0; i < (int)rotation; i++)
            {
                rotatedArray = RotateEntryFlagHorizontal90(rotatedArray);
            }

            if (_debug) Debug.Log($"Room {gameObject.name} has been rotated by 90 degrees.");
            
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
            int enListIdx = 0;              // iterator for activeEntranceway List
            for (int i = 0; i < 4; i++)     // iterate through 4 possible unit spaces
            {
                for (int j = 0; j < 6; j++)     // iterate through the faces of each unit
                {
                    enListIdx = (i * 6) + j;
                    if (openEntranceways[i, j] == true)   // Activate entrance if true in activeEntranceway List
                        ActivateEntranceway(enListIdx);
                }
            }
        }

        /// <summary>
        /// Activate an Entranceway in the entranceway list
        /// </summary>
        /// <param name="entranceNum"></param>
        // _roomWalls is expected to be laid out in the prefab in the same flattened [unit*6 + face] order as
        // openEntranceways, and each wall transform's child 0/1 are expected to be the open-doorway
        // mesh/collider and the solid-wall mesh/collider respectively - swapping which one is active is how a
        // face becomes a walkable doorway instead of a solid wall.
        private void ActivateEntranceway(int entranceNum)
        {
            _roomWalls[entranceNum].GetChild(0).gameObject.SetActive(true);   // Activate Entranceway
            _roomWalls[entranceNum].GetChild(1).gameObject.SetActive(false);  // Deactivate Wall
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
            foreach (Vector3Int cell in AvailableCellData)
            {
                Vector3 cellWorldPos = transform.position + (13f * 0.5f) * Vector3.up + (Vector3)cell * 13; // Assuming each cell is 13 units apart
                Gizmos.color = _availableCellColor;
                Gizmos.DrawWireCube(cellWorldPos, Vector3.one * 13);
            }
        }
    }
}
