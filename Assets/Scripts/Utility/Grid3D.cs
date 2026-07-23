/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/23/2025
 * Last Modified:   04/05/2025 (Ryan)
 * Notes:           Data structure for storing a 3D grid
 *                  Adapted from https://github.com/Bl4ckb0ne/delaunay-triangulation
 *                  Copyright (c) 2015-2019 Simon Zeni (simonzeni@gmail.com)
*/
using UnityEngine;

namespace RyansLibrary
{
    /// <summary>
    /// Flat 1D array backing a 3D grid of arbitrary type T, sized to a BoundsInt. Used by SimpleAStar3D to store one
    /// pathfinding Node per cell. Important gotcha: GetIndex/the indexers expect *local* coordinates in the range
    /// [0, Size), not world/room coordinates - callers using an offset bounds (any BoundsInt not positioned at the
    /// origin) must subtract Bounds.position themselves before indexing (see SimpleAStar3D's `offset` field for how
    /// that's handled there). InBoundsExclusive/InBoundsInclusive, by contrast, do take real Bounds-space
    /// coordinates since they check against the stored BoundsInt directly.
    /// </summary>
    public class Grid3D<T>
    {
        public Vector3Int Size { get; private set; }

        public BoundsInt Bounds { get; private set; }

        private T[] data;

        public Grid3D(BoundsInt bounds)
        {
            Size = bounds.size;

            data = new T[Size.x * Size.y * Size.z];
            Bounds = bounds;
        }

        // Flattens a 3D local coordinate into the 1D backing array (row-major, X fastest-varying then Y then Z).
        public int GetIndex(Vector3Int pos)
        {
            return pos.x + (Size.x * pos.y) + (Size.x * Size.y * pos.z);
        }

        public bool InBoundsExclusive(Vector3Int pos)
        {
            return Bounds.Contains(pos);
        }

        public bool InBoundsInclusive(Vector3Int pos)
        {
            return pos.x >= Bounds.xMin && pos.x <= Bounds.xMax &&
                   pos.y >= Bounds.yMin && pos.y <= Bounds.yMax &&
                   pos.z >= Bounds.zMin && pos.z <= Bounds.zMax;
        }

        public T this[int x, int y, int z]
        {
            get
            {
                return this[new Vector3Int(x, y, z)];
            }
            set
            {
                this[new Vector3Int(x, y, z)] = value;
            }
        }

        public T this[Vector3Int pos]
        {
            get
            {
                return data[GetIndex(pos)];
            }
            set
            {
                data[GetIndex(pos)] = value;
            }
        }
    }
}

