/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/23/2025
 * Last Modified:   07/13/2026 (Ryan)
 * Notes:           3D Delaunay Triangulation Algorithm
 *                  Adapted from https://github.com/Bl4ckb0ne/delaunay-triangulation
 *                  Copyright (c) 2015-2019 Simon Zeni (simonzeni@gmail.com)
*/
using UnityEngine;
using RyansLibrary.Graphs;
using RyansLibrary.Geometry;
using System.Collections.Generic;

namespace RyansLibrary
{
    /// <summary>
    /// 3D Delaunay triangulation via Bowyer-Watson, tetrahedra instead of 2D's triangles: start from one super
    /// tetrahedron containing every point, insert points one at a time discarding every tetrahedron whose
    /// circumsphere contains the new point ("bad" tetrahedra), and re-tetrahedralize the resulting cavity by fanning
    /// new tetrahedra from its boundary faces to the new point (see the pairwise Triangle.AlmostEqual check below for
    /// how those boundary faces are found - the 3D equivalent of DelaunayTriangulation2D's edge-cancellation trick).
    /// Used by TriangulateBlueprints3DOp for zones with real vertical extent; for flat zones
    /// TriangulateBlueprints2DOp is preferred instead, partly because coplanar (same-plane) points can make this
    /// tetrahedra-based approach behave inconsistently - see the TODO on TriangulateBlueprints3DOp.
    /// </summary>
    public class DelaunayTriangulation3D
    {
        // Precision required for checking if a piece of geometry is nearly the same
        private const float k_triangulationPrecision = 0.01f;

        public List<Vertex> Verticies;
        public List<Edge> Edges;
        public List<Triangle> Triangles;
        public List<Tetrahedron> Tetrahedra;

        private List<Triangle> _badTriangles;
        private List<Tetrahedron> _badTetrahedra;

        DelaunayTriangulation3D()
        {
            Edges = new List<Edge>();
            Triangles = new List<Triangle>();
            Tetrahedra = new List<Tetrahedron>();

            _badTriangles = new List<Triangle>();
            _badTetrahedra = new List<Tetrahedron>();
        }

        /// <summary>
        /// Given a list of vertices, triangulate them using BoyerWatson Algorithm in 3D
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        public static DelaunayTriangulation3D Triangulate(List<Vertex> vertices)
        {
            DelaunayTriangulation3D delaunay = new DelaunayTriangulation3D();
            delaunay.Verticies = new List<Vertex>(vertices);
            delaunay.Triangulate();

            return delaunay;
        }

        private void Triangulate()
        {

            // *** Find the absolute minimum and absolute maximum point of all vertices ***
            float minX = Verticies[0].Position.x;        // Min = Very first room vertex
            float minY = Verticies[0].Position.y;
            float minZ = Verticies[0].Position.z;

            float maxX = minX;                          // Max = Very first room vertex
            float maxY = minY;
            float maxZ = minZ;

            foreach (var vertex in Verticies)
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

            foreach (var vertex in Verticies)
            {                      // Loop through all vertices (room midpoints)
                List<Triangle> Triangles = new List<Triangle>();

                // If the tetrahedra contains a vertex in it's circumcircle then it is a bad tetrahedra
                foreach (Tetrahedron t in Tetrahedra)
                {
                    if (t.CircumCircleContains(vertex.Position))       // Check if vertex lies within circumcicle
                    {
                        _badTetrahedra.Add(t);
                        Triangles.Add(new Triangle(t.A, t.B, t.C));     // Make triangles out of each side of the tetrahedron
                        Triangles.Add(new Triangle(t.A, t.B, t.D));
                        Triangles.Add(new Triangle(t.A, t.C, t.D));
                        Triangles.Add(new Triangle(t.B, t.C, t.D));
                    }
                }

                // Find the cavity's boundary faces: each bad tetrahedron contributes its 4 triangular faces, and a
                // face shared between two bad tetrahedra is internal to the cavity (found here via pairwise
                // AlmostEqual and marked bad on both copies), while a face touching only one bad tetrahedron is on
                // the cavity's boundary and survives to be fanned into a new tetrahedron with the inserted vertex.
                // If a Triangle is basically on top of another triangle then it is a bad triangle
                for (int i = 0; i < Triangles.Count; i++)               // Select first triangle
                {
                    for (int j = i + 1; j < Triangles.Count; j++)       // Select second triangle
                    {
                        if (Triangle.AlmostEqual(Triangles[i], Triangles[j], k_triangulationPrecision))       // If both of the triangles are nearly on top of each other
                        {
                            _badTriangles.Add(Triangles[i]);
                            _badTriangles.Add(Triangles[j]);
                        }
                    }
                }

                // Remove all bad tetrahedron and triangles
                Tetrahedra.RemoveAll((Tetrahedron t) => _badTetrahedra.Contains(t));       // Remove all bad tetrahedron
                Triangles.RemoveAll((Triangle t) => _badTriangles.Contains(t));           // Remove all bad triagles

                // Clear lists for next iteration
                _badTetrahedra.Clear();
                _badTriangles.Clear();

                foreach (var triangle in Triangles)
                {                   // Add new tetrahedra after each iteration
                    Tetrahedra.Add(new Tetrahedron(triangle.A, triangle.B, triangle.C, vertex));
                }
            }

            // Remove all tetrahedron that have the points of the original tetrahedron
            Tetrahedra.RemoveAll((Tetrahedron t) => 
                t.ContainsVertex(p1, k_triangulationPrecision) || t.ContainsVertex(p2, k_triangulationPrecision) ||
                t.ContainsVertex(p3, k_triangulationPrecision) || t.ContainsVertex(p4, k_triangulationPrecision));

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
