/*
 * Created By:      Ryan Carpenter
 * Date Created:    04/03/2025
 * Last Modified:   04/03/2025 (Ryan)
 * Notes:           Prim's Algorithm
 *                  Chapter 21.2 in Intro. To Alg.
*/
using System.Collections.Generic;

namespace RyansLibrary.Graphs
{
    /// <summary>
    /// Classic Prim's MST algorithm (grow-a-tree-from-a-start-vertex, always take the cheapest edge leaving the
    /// tree). Used by FindMSTOp to reduce a labyrinth zone's Delaunay triangulation down to the minimum set of
    /// corridors needed to connect every room, before random cycle edges get layered back on top for loops.
    /// O(E*V) here (rescans every edge each iteration) rather than a heap-based O(E log V) - fine at the room
    /// counts a single zone deals with, but worth knowing if zones ever get large.
    /// </summary>
    public static class PrimsAlgorithm
    {
        // Prim's algorithm to find the minimum spanning tree
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
