/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/23/2025
 * Last Modified:   07/13/2025 (Ryan)
 * Notes:           2D and 3D Geometry
 *                  Adapted from https://github.com/Bl4ckb0ne/delaunay-triangulation
 *                  Copyright (c) 2015-2019 Simon Zeni (simonzeni@gmail.com)
*/
using System;
using UnityEngine;
using RyansLibrary.Graphs;

namespace RyansLibrary.Geometry
{
    public class Tetrahedron : IEquatable<Tetrahedron>
    {
        public Vertex A { get; set; }       // Custom vertex class
        public Vertex B { get; set; }       // Custom vertex class
        public Vertex C { get; set; }       // Custom vertex class
        public Vertex D { get; set; }       // Custom vertex class

        public Vector3 Circumcenter { get; private set; }
        public float CircumradiusSquared { get; private set; }

        public Tetrahedron(Vertex a, Vertex b, Vertex c, Vertex d)
        {
            A = a;
            B = b;
            C = c;
            D = d;

            // TODO: solve case where Tetrahedron is coplanar
            CalculateCircumsphere();
        }

        // Creates a circumsphere from a tetrahedron
        //http://mathworld.wolfram.com/Circumsphere.html
        void CalculateCircumsphere()
        {

            // Signed volume inside tetrahedron
            float a = new Matrix4x4(
                new Vector4(A.Position.x, B.Position.x, C.Position.x, D.Position.x),        // Col0
                new Vector4(A.Position.y, B.Position.y, C.Position.y, D.Position.y),        // Col1
                new Vector4(A.Position.z, B.Position.z, C.Position.z, D.Position.z),        // Col2
                new Vector4(1, 1, 1, 1)                                                     // Col3
            ).determinant;

            // Squared distance of each vertex from origin
            float aPosSqr = A.Position.sqrMagnitude;
            float bPosSqr = B.Position.sqrMagnitude;
            float cPosSqr = C.Position.sqrMagnitude;
            float dPosSqr = D.Position.sqrMagnitude;

            // D_x
            float Dx = new Matrix4x4(
                new Vector4(aPosSqr, bPosSqr, cPosSqr, dPosSqr),
                new Vector4(A.Position.y, B.Position.y, C.Position.y, D.Position.y),
                new Vector4(A.Position.z, B.Position.z, C.Position.z, D.Position.z),
                new Vector4(1, 1, 1, 1)
            ).determinant;

            // D_y
            float Dy = -(new Matrix4x4(
                new Vector4(aPosSqr, bPosSqr, cPosSqr, dPosSqr),
                new Vector4(A.Position.x, B.Position.x, C.Position.x, D.Position.x),
                new Vector4(A.Position.z, B.Position.z, C.Position.z, D.Position.z),
                new Vector4(1, 1, 1, 1)
            ).determinant);

            // D_z
            float Dz = new Matrix4x4(
                new Vector4(aPosSqr, bPosSqr, cPosSqr, dPosSqr),
                new Vector4(A.Position.x, B.Position.x, C.Position.x, D.Position.x),
                new Vector4(A.Position.y, B.Position.y, C.Position.y, D.Position.y),
                new Vector4(1, 1, 1, 1)
            ).determinant;

            float c = new Matrix4x4(
                new Vector4(aPosSqr, bPosSqr, cPosSqr, dPosSqr),
                new Vector4(A.Position.x, B.Position.x, C.Position.x, D.Position.x),
                new Vector4(A.Position.y, B.Position.y, C.Position.y, D.Position.y),
                new Vector4(A.Position.z, B.Position.z, C.Position.z, D.Position.z)
            ).determinant;

            // Find the center of the circumsphere from everything else above
            Circumcenter = new Vector3(
                Dx / (2 * a),
                Dy / (2 * a),
                Dz / (2 * a)
            );

            // Circumradius
            CircumradiusSquared = ((Dx * Dx) + (Dy * Dy) + (Dz * Dz) - (4 * a * c)) / (4 * a * a);
        }

        public bool ContainsVertex(Vertex v, float precision)
        {
            return Vertex.AlmostEqual(v, A, precision)
                || Vertex.AlmostEqual(v, B, precision)
                || Vertex.AlmostEqual(v, C, precision)
                || Vertex.AlmostEqual(v, D, precision);
        }

        // Check if the vertex lies within the circum circle by checking it's difference from the circumcurcle radius
        public bool CircumCircleContains(Vector3 v)
        {
            Vector3 dist = v - Circumcenter;
            return dist.sqrMagnitude <= CircumradiusSquared;
        }

        public static bool operator ==(Tetrahedron left, Tetrahedron right)
        {
            return (left.A == right.A || left.A == right.B || left.A == right.C || left.A == right.D)
                && (left.B == right.A || left.B == right.B || left.B == right.C || left.B == right.D)
                && (left.C == right.A || left.C == right.B || left.C == right.C || left.C == right.D)
                && (left.D == right.A || left.D == right.B || left.D == right.C || left.D == right.D);
        }

        public static bool operator !=(Tetrahedron left, Tetrahedron right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is Tetrahedron t)
            {
                return this == t;
            }

            return false;
        }

        // Required from IEquitable
        public bool Equals(Tetrahedron t)
        {
            return this == t;
        }

        public override int GetHashCode()
        {
            return A.GetHashCode() ^ B.GetHashCode() ^ C.GetHashCode() ^ D.GetHashCode();
        }
    }

    public class Triangle
    {
        public Vertex A { get; set; }
        public Vertex B { get; set; }
        public Vertex C { get; set; }

        public Vector2 Circumcenter;
        public float CircumradiusSquared;

        public Triangle() { }

        public Triangle(Vertex u, Vertex v, Vertex w)
        {
            A = u;
            B = v;
            C = w;

            CalculateCircumcircle();
        }

        public bool ContainsVertex(Vertex vertex, float precision)
        {
            return Vertex.AlmostEqual(vertex, A, precision) ||
                   Vertex.AlmostEqual(vertex, B, precision) ||
                   Vertex.AlmostEqual(vertex, C, precision);
        }

        public bool CircumCircleContains(Vector3 point)
        {
            Vector2 p = new Vector2(point.x, point.y);

            return (p - Circumcenter).sqrMagnitude <= CircumradiusSquared;
        }

        private void CalculateCircumcircle()
        {
            float d =
                2 *
                (A.Position.x * (B.Position.y - C.Position.y) +
                 B.Position.x * (C.Position.y - A.Position.y) +
                 C.Position.x * (A.Position.y - B.Position.y));


            float ux =
                ((A.Position.sqrMagnitude * (B.Position.y - C.Position.y)) +
                 (B.Position.sqrMagnitude * (C.Position.y - A.Position.y)) +
                 (C.Position.sqrMagnitude * (A.Position.y - B.Position.y)))
                 / d;


            float uy =
                ((A.Position.sqrMagnitude * (C.Position.x - B.Position.x)) +
                 (B.Position.sqrMagnitude * (A.Position.x - C.Position.x)) +
                 (C.Position.sqrMagnitude * (B.Position.x - A.Position.x)))
                 / d;


            Circumcenter = new Vector2(ux, uy);


            CircumradiusSquared =
                (Circumcenter -
                new Vector2(A.Position.x, A.Position.y))
                .sqrMagnitude;
        }

        public static bool operator ==(Triangle left, Triangle right)
        {
            return (left.A == right.A || left.A == right.B || left.A == right.C)
                && (left.B == right.A || left.B == right.B || left.B == right.C)
                && (left.C == right.A || left.C == right.B || left.C == right.C);
        }

        public static bool operator !=(Triangle left, Triangle right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is Triangle e)
            {
                return this == e;
            }
            return false;
        }

        public bool Equals(Triangle e)
        {
            return this == e;
        }

        public override int GetHashCode()
        {
            return A.GetHashCode() ^ B.GetHashCode() ^ C.GetHashCode();
        }

        // If both triangles are almost exactly in the same place
        public static bool AlmostEqual(Triangle left, Triangle right, float precision)
        {
            return (Vertex.AlmostEqual(left.A, right.A, precision) || Vertex.AlmostEqual(left.A, right.B, precision) || Vertex.AlmostEqual(left.A, right.C, precision))
                && (Vertex.AlmostEqual(left.B, right.A, precision) || Vertex.AlmostEqual(left.B, right.B, precision) || Vertex.AlmostEqual(left.B, right.C, precision))
                && (Vertex.AlmostEqual(left.C, right.A, precision) || Vertex.AlmostEqual(left.C, right.B, precision) || Vertex.AlmostEqual(left.C, right.C, precision));
        }
    }
}
