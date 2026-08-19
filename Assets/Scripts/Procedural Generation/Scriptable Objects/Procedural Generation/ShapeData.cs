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

    /// <summary>
    /// Vector3Int -> CellState with O(1) lookup. _keys/_values are the actual serialized source of truth
    /// (what ShapeCellDictionaryDrawer edits); the Dictionary is a read-only cache lazily rebuilt from them,
    /// invalidated in OnAfterDeserialize. Deliberately NOT a Dictionary subclass: a "OnBeforeSerialize copies
    /// FROM the live dictionary" design silently discards Inspector edits, since edits only ever touch
    /// _keys/_values and nothing pushes them into the dictionary except OnAfterDeserialize - the next
    /// serialize-from-dictionary pass (which happens far more often than an explicit user Save) overwrites
    /// them right back to the stale state. Confirmed live in-editor: SerializedProperty.arraySize++ followed
    /// by ApplyModifiedProperties reported success, but a freshly constructed SerializedObject on the same
    /// asset showed the increment had been silently reverted.
    /// </summary>
    [Serializable]
    public class ShapeCellDictionary : ISerializationCallbackReceiver
    {
        [SerializeField] private List<Vector3Int> _keys = new List<Vector3Int>();
        [SerializeField] private List<CellState> _values = new List<CellState>();

        [NonSerialized] private Dictionary<Vector3Int, CellState> _lookup;

        public int Count => _keys.Count;

        public Dictionary<Vector3Int, CellState>.KeyCollection Keys
        {
            get
            {
                EnsureLookup();
                return _lookup.Keys;
            }
        }

        public bool TryGetValue(Vector3Int position, out CellState state)
        {
            EnsureLookup();
            return _lookup.TryGetValue(position, out state);
        }

        public bool ContainsKey(Vector3Int position)
        {
            EnsureLookup();
            return _lookup.ContainsKey(position);
        }

        public Dictionary<Vector3Int, CellState>.Enumerator GetEnumerator()
        {
            EnsureLookup();
            return _lookup.GetEnumerator();
        }

        private void EnsureLookup()
        {
            if (_lookup != null)
                return;

            _lookup = new Dictionary<Vector3Int, CellState>(_keys.Count);
            int count = Mathf.Min(_keys.Count, _values.Count);
            for (int i = 0; i < count; i++)
                _lookup[_keys[i]] = _values[i]; // last entry wins if a position is duplicated
        }

        // No-op: _keys/_values are already the serialized source of truth, nothing needs copying into them.
        public void OnBeforeSerialize() { }

        // _keys/_values may have just changed (Inspector edit, Undo, asset reload) - drop the cache so the
        // next lookup rebuilds it instead of reading stale entries.
        public void OnAfterDeserialize()
        {
            _lookup = null;
        }
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
        [field: SerializeField] public ShapeCellDictionary RoomCells { get; private set; } = new ShapeCellDictionary();
    }
}
