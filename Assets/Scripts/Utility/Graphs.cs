/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/23/2025
 * Last Modified:   03/31/2025 (Ryan)
 * Notes:           Common structures and methods from graphs
 *                  Adapted from https://github.com/Bl4ckb0ne/delaunay-triangulation
 *                  Copyright (c) 2015-2019 Simon Zeni (simonzeni@gmail.com)
*/
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Graphs
{
    /// <summary>
    /// Describes the vertex of a graph, 3D position
    /// </summary>
    public class Vertex : IEquatable<Vertex>
    {
        public Vector3 Position { get; private set; }

        public Vertex() { }

        public Vertex(Vector3 position)
        {
            Position = position;
        }

        // If vertices are almost on top of each other
        public static bool AlmostEqual(Vertex left, Vertex right, float precision)
        {
            return (left.Position - right.Position).sqrMagnitude < precision;
        }

        public override bool Equals(object obj)
        {
            if (obj is Vertex v)
            {
                return Position == v.Position;
            }

            return false;
        }

        public bool Equals(Vertex other)
        {
            return Position == other.Position;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }

    /// <summary>
    /// A vertex that holds data; child class of Vertex
    /// </summary>
    /// <typeparam name="T">Data to store in vertex</typeparam>
    public class Vertex<T> : Vertex
    {
        public T Data { get; private set; }

        public Vertex(T data)
        {
            Data = data;
        }

        public Vertex(Vector3 position, T data) : base(position)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Describes a graph edge, line segment
    /// </summary>
    public class Edge : IEquatable<Edge>
    {
        public float Distance { get; private set; }     // Distance represents an edges weight

        public Vertex U { get; set; }
        public Vertex V { get; set; }

        public Edge() { }

        public Edge(Vertex u, Vertex v)
        {
            U = u;
            V = v;
            Distance = Vector3.Distance(u.Position, v.Position);
        }

        // Edges are equal if they are right on top of each other
        public static bool operator == (Edge left, Edge right)
        {
            return (left.U == right.U || left.U == right.V)
                && (left.V == right.U || left.V == right.V);
        }

        // Edges are not equal if they are not on top of each other
        public static bool operator != (Edge left, Edge right)
        {
            return !(left == right);
        }

        // If the edges are almost on top of each other
        public static bool AlmostEqual(Edge left, Edge right, float precision)
        {
            return (Vertex.AlmostEqual(left.U, right.U, precision) || Vertex.AlmostEqual(left.V, right.U, precision))
                && (Vertex.AlmostEqual(left.U, right.V, precision) || Vertex.AlmostEqual(left.V, right.U, precision));
        }

        // Is this object an Edge?
        public override bool Equals(object obj)
        {
            if (obj is Edge e)
            {
                return this == e;
            }

            return false;
        }

        public bool Equals(Edge e)
        {
            return this == e;
        }

        public override int GetHashCode()
        {
            return U.GetHashCode() ^ V.GetHashCode();
        }
    }
}
