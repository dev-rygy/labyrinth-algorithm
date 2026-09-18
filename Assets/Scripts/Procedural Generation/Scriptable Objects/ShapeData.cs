/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/19/2026
 * Last Modified:   08/19/2026 (Ryan)
 * Notes:           Data-only definition of a parsible _roomShape's footprint
*/
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public enum CellState
    {
        Blueprint,
        NoBlueprint,
        DontCare,
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
        [SerializedDictionary("Cell Position", "Cell State")]
        public SerializedDictionary<Vector3Int, CellState> Cells;
        public int CellCount        // Only count cells marked as 'Blueprint'
        {
            get
            {
                int count = 0;
                foreach (var cell in Cells)
                {
                    if (cell.Value == CellState.Blueprint)
                        count++;
                }
                return count;
            }
        }

    }
}
