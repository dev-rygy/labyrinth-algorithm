/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/23/2025
 * Last Modified:   03/31/2025 (Ryan)
 * Notes:           3D Delaunay Triangulation Algorithm
 *                  Adapted from https://github.com/Bl4ckb0ne/delaunay-triangulation
 *                  Copyright (c) 2015-2019 Simon Zeni (simonzeni@gmail.com)
*/
using UnityEngine;
using RyansLibrary.Graphs;
using RyansLibrary.Geometry;
using RyansLibrary.Labyrinth;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;

namespace RyansLibrary
{
    public class DelaunayTriangulation3D
    {
        // Precision required for checking if a piece of geometry is nearly the same
        private const float TRIANGULATION_PRECISION = 0.01f;

        // Derived class from Graphs.cs
        public class Edge : Graphs.Edge
        {
            public bool IsBad { get; set; }

            public Edge() : base() { }

            public Edge(Vertex u, Vertex v) : base(u, v) { }
        }

        // Derived class from Geometry.cs
        public class Tetrahedron : Geometry.Tetrahedron
        {
            public bool IsBad { get; set; }

            public Tetrahedron(Vertex a, Vertex b, Vertex c, Vertex d) : base(a, b, c, d) { }
        }

        // Derived class from Geometry.cs
        public class Triangle : Geometry.Triangle
        {
            public bool IsBad { get; set; }
            public Triangle(Vertex u, Vertex v, Vertex w) : base(u, v, w) { }
        }

        public List<Vertex> Vertices { get; private set; }
        public List<Edge> Edges { get; private set; }
        public List<Triangle> Triangles { get; private set; }
        public List<Tetrahedron> Tetrahedra { get; private set; }

        DelaunayTriangulation3D()
        {
            Edges = new List<Edge>();
            Triangles = new List<Triangle>();
            Tetrahedra = new List<Tetrahedron>();
        }

        /// <summary>
        /// Given a list of vertices, triangulate them using BoyerWatson Algorithm in 3D
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static DelaunayTriangulation3D Triangulate(List<Vertex> vertices)
        {
            DelaunayTriangulation3D delaunay = new DelaunayTriangulation3D();
            delaunay.Vertices = new List<Vertex>(vertices);
            delaunay.Triangulate();

            return delaunay;
        }

        void Triangulate()
        {
            float minX = Vertices[0].Position.x;        // Min = Very first room vertex
            float minY = Vertices[0].Position.y;
            float minZ = Vertices[0].Position.z;

            float maxX = minX;                          // Max = Very first room vertex
            float maxY = minY;
            float maxZ = minZ;

            // Find the absolute minimum and absolute maximum point of all vertices
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

                if (vertex.Position.z < minZ)
                    minZ = vertex.Position.z;

                if (vertex.Position.z > maxZ)
                    maxZ = vertex.Position.z;
            }

            // Calculate absolute difference
            float dx = maxX - minX;
            float dy = maxY - minY;
            float dz = maxZ - minZ;
            float deltaMax = Mathf.Max(dx, dy, dz) * 2;

            // Create *Super Tetrahedra* that encapsulates all vertices
            Vertex p1 = new Vertex(new Vector3(minX - 1, minY - 1, minZ - 1));
            Vertex p2 = new Vertex(new Vector3(maxX + deltaMax, minY - 1, minZ - 1));
            Vertex p3 = new Vertex(new Vector3(minX - 1, maxY + deltaMax, minZ - 1));
            Vertex p4 = new Vertex(new Vector3(minX - 1, minY - 1, maxZ + deltaMax));

            Tetrahedra.Add(new Tetrahedron(p1, p2, p3, p4));

            foreach (var vertex in Vertices)
            {                      // Loop through all vertices (room midpoints)
                List<Triangle> triangles = new List<Triangle>();

                // If the tetrahedra contains a vertex in it's circumcircle then it is a bad tetrahedra
                foreach (var t in Tetrahedra)
                {
                    if (t.CircumCircleContains(vertex.Position))       // Check if vertex lies within circumcicle
                    {
                        t.IsBad = true;
                        triangles.Add(new Triangle(t.A, t.B, t.C));     // Make triangles out of each side of the tetrahedron
                        triangles.Add(new Triangle(t.A, t.B, t.D));
                        triangles.Add(new Triangle(t.A, t.C, t.D));
                        triangles.Add(new Triangle(t.B, t.C, t.D));
                    }
                }

                // If a Triangle is basically on top of another triangle then it is a bad triangle
                for (int i = 0; i < triangles.Count; i++)               // Select first triangle
                {
                    for (int j = i + 1; j < triangles.Count; j++)       // Select second triangle
                    {
                        if (Triangle.AlmostEqual(triangles[i], triangles[j], TRIANGULATION_PRECISION))       // If both of the triangles are nearly on top of each other
                        {
                            triangles[i].IsBad = true;
                            triangles[j].IsBad = true;
                        }
                    }
                }

                // Remove all bad tetrahedron and triangles
                Tetrahedra.RemoveAll((Tetrahedron t) => t.IsBad);       // Remove all bad tetrahedron
                triangles.RemoveAll((Triangle t) => t.IsBad);           // Remove all bad triagles

                foreach (var triangle in triangles)
                {                   // Add new tetrahedra after each iteration
                    Tetrahedra.Add(new Tetrahedron(triangle.U, triangle.V, triangle.W, vertex));
                }
            }

            // Remove all tetrahedron that have the points of the original tetrahedron
            Tetrahedra.RemoveAll((Tetrahedron t) => 
                t.ContainsVertex(p1, TRIANGULATION_PRECISION) || t.ContainsVertex(p2, TRIANGULATION_PRECISION) || 
                t.ContainsVertex(p3, TRIANGULATION_PRECISION) || t.ContainsVertex(p4, TRIANGULATION_PRECISION));

            HashSet<Triangle> triangleSet = new HashSet<Triangle>();
            HashSet<Edge> edgeSet = new HashSet<Edge>();

            // Convert all remaining triangles to edges and add both to their respective data structures
            foreach (var t in Tetrahedra)
            {

                // Store triangles
                var abc = new Triangle(t.A, t.B, t.C);
                var abd = new Triangle(t.A, t.B, t.D);
                var acd = new Triangle(t.A, t.C, t.D);
                var bcd = new Triangle(t.B, t.C, t.D);

                if (triangleSet.Add(abc))
                {
                    Triangles.Add(abc);
                }

                if (triangleSet.Add(abd))
                {
                    Triangles.Add(abd);
                }

                if (triangleSet.Add(acd))
                {
                    Triangles.Add(acd);
                }

                if (triangleSet.Add(bcd))
                {
                    Triangles.Add(bcd);
                }

                // Convert triangles to edges and store edges
                var ab = new Edge(t.A, t.B);
                var bc = new Edge(t.B, t.C);
                var ca = new Edge(t.C, t.A);
                var da = new Edge(t.D, t.A);
                var db = new Edge(t.D, t.B);
                var dc = new Edge(t.D, t.C);

                if (edgeSet.Add(ab))
                {
                    Edges.Add(ab);
                }

                if (edgeSet.Add(bc))
                {
                    Edges.Add(bc);
                }

                if (edgeSet.Add(ca))
                {
                    Edges.Add(ca);
                }

                if (edgeSet.Add(da))
                {
                    Edges.Add(da);
                }

                if (edgeSet.Add(db))
                {
                    Edges.Add(db);
                }

                if (edgeSet.Add(dc))
                {
                    Edges.Add(dc);
                }
            }
        }
    }
}
