/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/23/2025
 * Last Modified:   04/05/2025 (Ryan)
 * Notes:           Data structure for storing a 3D grid
 *                  Adapted from https://github.com/Bl4ckb0ne/delaunay-triangulation
 *                  Copyright (c) 2015-2019 Simon Zeni (simonzeni@gmail.com)
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary
{
    public class Grid3D<T>
    {
        public Vector3Int Size { get; private set; }
        public Vector3Int Offset { get; set; }

        public BoundsInt Bounds { get; private set; }

        private T[] data;

        public Grid3D(BoundsInt bounds, Vector3Int offset)
        {
            Size = bounds.size;
            Offset = offset;

            data = new T[Size.x * Size.y * Size.z];
            Bounds = bounds;
        }

        public int GetIndex(Vector3Int pos)
        {
            return pos.x + (Size.x * pos.y) + (Size.x * Size.y * pos.z);
        }

        public bool InBounds(Vector3Int pos)
        {
            return Bounds.Contains(pos + Offset);
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
                pos += Offset;
                return data[GetIndex(pos)];
            }
            set
            {
                pos += Offset;
                data[GetIndex(pos)] = value;
            }
        }
    }
}

