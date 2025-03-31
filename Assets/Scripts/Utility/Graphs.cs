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
        public Vertex U { get; set; }
        public Vertex V { get; set; }

        public Edge() { }

        public Edge(Vertex u, Vertex v)
        {
            U = u;
            V = v;
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

    public static class PrimsAlgorithm
    {
        // Derived Class from this file
        public class Edge : Graphs.Edge
        {
            public float Distance { get; private set; }     // Distance represents an edges weight

            public Edge(Vertex u, Vertex v) : base(u, v)
            {
                Distance = Vector3.Distance(u.Position, v.Position);
            }

            // Edges are equal if they are right on top of each other
            public static bool operator == (Edge left, Edge right)
            {
                return (left.U == right.U && left.V == right.V)
                    || (left.U == right.V && left.V == right.U);
            }

            // Edges are equal if they are not on top of each other
            public static bool operator != (Edge left, Edge right)
            {
                return !(left == right);
            }

            // Is the object this edge?
            public override bool Equals(object obj)
            {
                if (obj is Edge e)
                {
                    return this == e;
                }

                return false;
            }

            // Is the object this edge?
            public bool Equals(Edge e)
            {
                return this == e;
            }

            public override int GetHashCode()
            {
                return U.GetHashCode() ^ V.GetHashCode();
            }
        }

        // Prim's algorithm to find the minimum spanning tree; Chapter 21.2 in Intro. To Alg.
        public static List<Edge> MinimumSpanningTree(List<Edge> edges, Vertex start)
        {
            HashSet<Vertex> openSet = new HashSet<Vertex>();        // list of verticies not yet visited that can be travered to
            HashSet<Vertex> closedSet = new HashSet<Vertex>();      // keeps track of visited verticies basically

            foreach (var edge in edges)
            {
                openSet.Add(edge.U);
                openSet.Add(edge.V);
            }

            closedSet.Add(start);       // starting vertex

            List<Edge> results = new List<Edge>();      // Minimum spanning tree edges

            while (openSet.Count > 0)          // Stop when there is no more nodes to visit
            {
                bool chosen = false;
                Edge chosenEdge = null;
                float minWeight = float.PositiveInfinity;       // Init. the minimum to inf. for obvious reasons

                foreach (var edge in edges)
                {
                    // Make sure the edge we are checking has verticies not already been visited
                    int closedVertices = 0;
                    if (!closedSet.Contains(edge.U))
                        closedVertices++;
                    if (!closedSet.Contains(edge.V))
                        closedVertices++;

                    // The edge must have one and ONLY one non visited vertex for it to be checked
                    if (closedVertices != 1)
                        continue;

                    // If the edge has a smaller weight than the minimum then replace the minimum
                    if (edge.Distance < minWeight)
                    {
                        chosenEdge = edge;
                        chosen = true;
                        minWeight = edge.Distance;
                    }
                }

                if (!chosen)        // If there is no edge that has a min then break out of loo; likely error?
                    break;

                results.Add(chosenEdge);
                openSet.Remove(chosenEdge.U);
                openSet.Remove(chosenEdge.V);
                closedSet.Add(chosenEdge.U);
                closedSet.Add(chosenEdge.V);
            }

            return results;
        }
    }
}
