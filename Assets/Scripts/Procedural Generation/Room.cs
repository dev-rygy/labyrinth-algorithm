/*
 * Created By:      Ryan Carpenter
 * Date Created:    10/13/2024
 * Last Modified:   09/23/2026 (Ryan)
 * Notes:           Room data; some values set by the 
 *                  Map Generator and some values pre set
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Math = RyansLibrary.Utilities.Math;

namespace RyansLibrary.Labyrinth
{
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

    public static class RoomRotationExtensions
    {
        /// <summary>
        /// Yaw matrix for a room rotation. Same turn as the room's transform, Quaternion.Euler(0, 90 * rotation, 0).
        /// </summary>
        public static Math.Matrix3x3Int ToMatrix(this RoomRotation rotation)
        {
            return rotation switch
            {
                RoomRotation.Deg0 => Math.Matrix3x3Int.Identity,
                RoomRotation.Deg90 => Math.Matrix3x3Int.RotMatrixY90,
                RoomRotation.Deg180 => Math.Matrix3x3Int.RotMatrixY180,
                RoomRotation.Deg270 => Math.Matrix3x3Int.RotMatrixY270,
                _ => Math.Matrix3x3Int.Identity,
            };
        }

        /// <summary>
        /// Undoes ToMatrix(); maps world offsets back into the room's unrotated (shape) space.
        /// </summary>
        public static Math.Matrix3x3Int ToInverseMatrix(this RoomRotation rotation)
        {
            return ((RoomRotation)((4 - (int)rotation) % 4)).ToMatrix();
        }
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
        [SerializeField, FormerlySerializedAs("IsAvilable")] public bool IsAvailable;     // Prefabs still save the old misspelled name
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

        // Face order shared by Blueprint.EntryPointFlags and RoomCell.Walls: +X, -X, +Z, -Z, +Y, -Y
        private static readonly Vector3Int[] k_faceDirections =
        {
            Vector3Int.right,
            Vector3Int.left,
            Vector3Int.forward,
            Vector3Int.back,
            Vector3Int.up,
            Vector3Int.down,
        };

        [Header("Room Components")]
        [SerializeField] private List<RoomCell> _roomCells;
        public List<RoomCell> RoomCells => _roomCells;
        [SerializeField] public List<SpawnPad> RoomSpawners;

        [Header("Room Properties")]
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
        /// Simply copy the bluePrint room's entranceway flags into the room's open entraceways.
        /// </summary>
        /// <param name="blueprintArray">The blueprint room's entranceway array (6 possible entrances)</param>
        /// <param name="unitIndex">A specific unit space of the room in question</param>
        /// <param name="rotation">Room's rotation in the world; blueprint flags face world directions, walls face the room's own</param>
        public void CopyBlueprintEntranceFlags(Blueprint blueprint, RoomCell cell, RoomRotation rotation = RoomRotation.Deg0)
        {
            if (!cell.IsAvailable)
                return;

            Math.Matrix3x3Int rotationMatrix = rotation.ToMatrix();

            for (int i = 0; i < blueprint.EntryPointFlags.Length; i++) // iterate through all six walls of the room cell
            {
                if (cell.Walls[i].IsExemptFromMutation)
                    continue;

                // World face this wall points at once the room is rotated
                int worldFace = Array.IndexOf(k_faceDirections, rotationMatrix * k_faceDirections[i]);

                if (blueprint.EntryPointFlags[worldFace])
                    ActivateEntranceway(cell.Walls[i]);
                else
                    DeactivateEntranceway(cell.Walls[i]);
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
                if (_roomCells[i].IsAvailable)
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
                if (cell.IsAvailable)
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

                if (cell.IsAvailable)
                    Gizmos.color = _availableCellColor;
                else
                    Gizmos.color = _unavailableCellColor;
                Gizmos.DrawWireCube(cellWorldPos, Vector3.one * 13);
            }
        }
    }
}
