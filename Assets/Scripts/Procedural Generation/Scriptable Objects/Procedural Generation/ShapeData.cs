/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/19/2026
 * Last Modified:   08/19/2026 (Ryan)
 * Notes:           Data-only definition of a parsible RoomShape's footprint
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public enum CellState
    {
        Blueprint,
        NoBlueprint,
        DontCare,
    }

    [Serializable]
    public struct ShapeCell
    {
        [SerializeField] public Vector3Int Position;
        [SerializeField] public CellState State;
    }

    /// <summary>
    /// Houses the RoomCells that make up one parsible RoomShape's footprint (relative cell positions only -
    /// no Walls/Transform data, since a ShapeData asset isn't tied to any specific Room prefab instance).
    /// Referenced by Path room-shape entries and, eventually, by the recursive-descent room-parsing algorithm
    /// to match parsed blueprint shapes against.
    /// </summary>
    [CreateAssetMenu(fileName = "ShapeData", menuName = "Scriptable Objects/Procedural Generation/Shape Data", order = 3)]
    public class ShapeData : ScriptableObject
    {
        [field: SerializeField] public List<ShapeCell> RoomCells { get; private set; }
    }
}
