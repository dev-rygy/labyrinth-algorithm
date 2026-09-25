/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/19/2026
 * Last Modified:   08/19/2026 (Ryan)
 * Notes:           Data-only definition of a parsible _roomShape's footprint
*/
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public enum CellState
    {
        Blueprint,
        NoBlueprint,
        DontCare,
        NeedBlueprint
    }

    /// <summary>
    /// Houses the RoomCells that make up one parsible _roomShape's footprint (relative cell positions only -
    /// no Walls/Transform data, since a ShapeData asset isn't tied to any specific Room prefab instance).
    /// Referenced by Path room-shape entries and, eventually, by the recursive-descent room-parsing algorithm
    /// to match parsed blueprint shapes against.
    /// </summary>
    [CreateAssetMenu(fileName = "ShapeData", menuName = "Scriptable Objects/Procedural Generation/Shape Data", order = 3)]
    public class ShapeData : ScriptableObject
    {
        [SerializeField] private Dictionary<Vector3Int, CellState> _cells;
        public Dictionary<Vector3Int, CellState> Cells => _cells;
        [SerializeField] private bool _canRotate = true;
        public bool CanRotate => _canRotate;
        public int CellCount
        // Only count cells marked as 'Blueprint'
        {
            get
            {
                int count = 0;
                foreach (var cell in _cells)
                {
                    if (cell.Value == CellState.Blueprint)
                        count++;
                }
                return count;
            }
        }

    }
}
