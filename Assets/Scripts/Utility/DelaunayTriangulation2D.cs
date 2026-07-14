/*
 * Created By:      Ryan Carpenter
 * Date Created:    07/14/2026
 * Last Modified:   07/14/2026 (Ryan)
 * Notes:           2D Delaunay Triangulation Algorithm
 *                  Adapted from https://github.com/Bl4ckb0ne/delaunay-triangulation
 *                  Copyright (c) 2015-2019 Simon Zeni (simonzeni@gmail.com)
*/
using UnityEngine;
using RyansLibrary.Graphs;
using RyansLibrary.Geometry;
using System.Collections.Generic;

namespace RyansLibrary
{
    public class DelaunayTriangulation2D
    {
        // Precision required for checking if a piece of geometry is nearly the same
        private const float TRIANGULATION_PRECISION = 0.01f;

        public List<Vertex> Vertices;
        public List<Edge> Edges;
        public List<Triangle> Triangles;

        private List<Triangle> _badTriangles;

        DelaunayTriangulation2D()
        {
            Edges = new List<Edge>();
            Triangles = new List<Triangle>();

            _badTriangles = new List<Triangle>();
        }

        /// <summary>
        /// Given a list of vertices, triangulate them using BoyerWatson Algorithm in 3D
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static DelaunayTriangulation2D Triangulate(List<Vertex> vertices)
        {
            DelaunayTriangulation2D delaunay = new DelaunayTriangulation2D();
            delaunay.Vertices = new List<Vertex>(vertices);
            delaunay.Triangulate();

            return delaunay;
        }

        private void Triangulate()
        {
            Triangle superTriangle = new Triangle();

            // *** Find the absolute minimum and absolute maximum point of all vertices ***
            float minX = Vertices[0].Position.x;       // Min = Very first room vertex
            float minY = Vertices[0].Position.y;

            float maxX = minX;                          // Max = Very first room vertex
            float maxY = minY;

            foreach (var vertex in Vertices)
            {
                if (vertex.Position.x < minX)
                    minX = vertex.Position.x;

                if (vertex.Position.x > maxX)
                    maxX = vertex.Position.x;

                if (vertex.Position.y < minY)
                    minY = vertex.Position.y;

                if (vertex.Position.y > maxY)
                    maxY = vertex.Position.y;
            }

            // Calculate absolute difference
            float dx = maxX - minX;
            float dy = maxY - minY;
            float deltaMax = Mathf.Max(dx, dy) * 2;

            // Create *Super Tetrahedra* that encapsulates all vertices
            Vertex p1 = new Vertex(new Vector3(minX - 1, minY - 1));
            Vertex p2 = new Vertex(new Vector3(maxX + deltaMax, minY - 1));
            Vertex p3 = new Vertex(new Vector3(minX - 1, maxY + deltaMax));

            Triangles.Add(new Triangle(p1, p2, p3));

            // Loop through all vertices (room midpoints)
            foreach (var vertex in Vertices)
            {
                // If the tetrahedra contains a vertex in it's circumcircle then it is a bad tetrahedra
                foreach (Triangle triangle in Triangles)
                {
                    if (triangle.CircumCircleContains(vertex.Position))       // Check if vertex lies within circumcicle
                    {
                        _badTriangles.Add(triangle);
                    }
                }

                List<Edge> polygon = new List<Edge>();

                // Find boundary edges
                foreach (Triangle triangle in _badTriangles)
                {
                    Edge ab = new Edge(triangle.A, triangle.B);
                    Edge bc = new Edge(triangle.B, triangle.C);
                    Edge ca = new Edge(triangle.C, triangle.A);


                    AddEdge(polygon, ab);
                    AddEdge(polygon, bc);
                    AddEdge(polygon, ca);
                }

                // Remove all bad triangles
                Triangles.RemoveAll((Triangle t) => _badTriangles.Contains(t));           // Remove all bad triagles

                // Clear lists for next iteration
                _badTriangles.Clear();

                // Create new triangles
                foreach (Edge edge in polygon)
                {
                    Triangles.Add(
                        new Triangle(
                            edge.U,
                            edge.V,
                            vertex));
                }
            }

            // Remove triangles connected to super triangle
            Triangles.RemoveAll(t =>
                t.ContainsVertex(p1, TRIANGULATION_PRECISION) ||
                t.ContainsVertex(p2, TRIANGULATION_PRECISION) ||
                t.ContainsVertex(p3, TRIANGULATION_PRECISION));

            HashSet<Edge> edgeSet = new HashSet<Edge>();

            // Convert all remaining triangles to edges and add both to their respective data structures
            foreach (Triangle triangle in Triangles)
            {
                Edge ab = new Edge(triangle.A, triangle.B);
                Edge bc = new Edge(triangle.B, triangle.C);
                Edge ca = new Edge(triangle.C, triangle.A);


                if (edgeSet.Add(ab))
                    Edges.Add(ab);

                if (edgeSet.Add(bc))
                    Edges.Add(bc);

                if (edgeSet.Add(ca))
                    Edges.Add(ca);
            }
        }

        private void AddEdge(List<Edge> edges, Edge edge)
        {
            // If edge already exists it is internal and should be removed
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i] == edge)
                {
                    edges.RemoveAt(i);
                    return;
                }
            }

            edges.Add(edge);
        }
    }
}