/*
 * Created By:      Ryan Carpenter
 * Date Created:    04/03/2025
 * Last Modified:   04/03/2025 (Ryan)
 * Notes:           A* Pathfinding Algorithm
 *                  Adapted from https://github.com/Bl4ckb0ne/delaunay-triangulation
 *                  Copyright (c) 2015-2019 Simon Zeni (simonzeni@gmail.com)
*/
using UnityEngine;
using System.Collections.Generic;

using Priority_Queue;
using UnityEditor.SceneManagement;     // Needed for SimplePriorityQueue

namespace RyansLibrary.AI
{
    public enum Heuristic
    {
        Euclidean,
        Manhattan,
        Chebyshev,
        Dijkstra
    }

    public class SimpleAStar3D
    {
        private class Node
        {
            public Vector3Int Position { get; private set; }
            public Node Parent { get; set; }

            // Node Scores
            public float gScore;
            public float hScore;
            public float fScore;

            public bool Traversable { get; set; }

            public Node(Vector3Int position)
            {
                Position = position;
                Traversable = true;     // Default Traversable to true
            }
        }

        private readonly Vector3Int[] neighbors =
        {
            new Vector3Int(1, 0, 0),        // Right
            new Vector3Int(-1, 0, 0),       // Left
            new Vector3Int(0, 0, 1),        // Forward
            new Vector3Int(0, 0, -1),       // Back
            new Vector3Int(0, 1, 0),        // Top
            new Vector3Int(0, -1, 0)        // Bot
        };

        private SimplePriorityQueue<Node, float> openSet;
        private HashSet<Node> closedSet;
        private Stack<Vector3Int> path;
        private Grid3D<Node> grid;

        public SimpleAStar3D(BoundsInt bounds, Vector3Int offset)
        {
            grid = new Grid3D<Node>(bounds, offset);
            var size = grid.Size;

            openSet = new SimplePriorityQueue<Node, float>();
            closedSet = new HashSet<Node>();
            path = new Stack<Vector3Int>();

            // Init Grid
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3Int position = new Vector3Int(x, y, z);
                        grid[position] = new Node(position);              // Filling grid with nodes
                    }
                }
            }
        }

        public List<Vector3Int> FindPath(Vector3Int start, Vector3Int end, HashSet<Vector3Int> obstructions, Heuristic heristicType = Heuristic.Euclidean)
        {
            // Reset Grid
            ResetNodes(obstructions);

            // Clear storage
            openSet.Clear();
            closedSet.Clear();
            path.Clear();

            if (!grid.InBounds(start))      // Make sure the starting position is inside the given bounds
            {
                Debug.LogError("A* Error: starting position lies outside the given bounds.");
                return null;
            }
            if (!grid.InBounds(end))        // Make sure the ending position is inside the given bounds
            {
                Debug.LogError("A* Error: ending position lies outside the given bounds.");
                return null;
            }

            // Set the cost of start node and push into the queue 
            Node startNode = grid[start];
            startNode.gScore = 0;
            startNode.hScore = HeuristicFunction(startNode, grid[end], heristicType);
            startNode.fScore = startNode.gScore + startNode.hScore;
            openSet.Enqueue(startNode, startNode.fScore);               // 0 priority is the fScore of the starting node

            while (openSet.Count > 0)
            {
                // Pop off the last node from open set queue store in closed set
                Node current = openSet.Dequeue();
                closedSet.Add(current);

                // If the current node is at the end reconstruct the final path and return
                if (current.Position == end)
                    return ReconstructPath(current);

                // Look at all neighbor nodes and evaluate
                foreach (var offset in neighbors)
                {
                    if (!grid.InBounds(current.Position + offset))      // Keep in bounds
                        continue;

                    var neighbor = grid[current.Position + offset];

                    if (closedSet.Contains(neighbor))                   // Closed set contains neighbor
                        continue;

                    if (!neighbor.Traversable)                          // Can you traverse over neighbor? No, then skip node
                        continue;

                    float newGScore = current.gScore + 1;      // Compute new g(n) using manhatten distance

                    if (newGScore < neighbor.gScore)
                    {
                        neighbor.Parent = current;
                        neighbor.gScore = newGScore;
                        neighbor.hScore = HeuristicFunction(neighbor, grid[end], heristicType);
                        neighbor.fScore = neighbor.gScore + neighbor.hScore;        // TODO: Add weights to heuristic later for varied results

                        if (openSet.TryGetPriority(neighbor, out float existingPriority))
                            openSet.UpdatePriority(neighbor, neighbor.fScore);
                        else
                            openSet.Enqueue(neighbor, neighbor.fScore);
                    }
                }
            }

            Debug.LogError("A* Error: Pathfinding failed.");
            return null;
        }

        List<Vector3Int> ReconstructPath(Node node)
        {
            List<Vector3Int> result = new List<Vector3Int>();

            while (node != null)
            {
                path.Push(node.Position);
                node = node.Parent;
            }

            while (path.Count > 0)
            {
                result.Add(path.Pop());
            }

            return result;
        }

        private void ResetNodes(HashSet<Vector3Int> obstructions)
        {
            var size = grid.Size;

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3Int position = new Vector3Int(x, y, z);
                        var node = grid[position];                   // Fill Position
                        node.Parent = null;                         // No Parent
                        node.gScore = float.PositiveInfinity;       // Infinate cost for scores
                        node.hScore = float.PositiveInfinity;
                        node.fScore = float.PositiveInfinity;

                        // Cannot traverse nodes in obstuctions set
                        if (obstructions.Contains(position))
                        {
                            node.Traversable = false;
                        }
                        else
                            node.Traversable = true;
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the heuristic function given two points and a type.
        /// Many different types of heuristic functions can change the behavior and
        /// accuracy of the A* path.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        private float HeuristicFunction(Node current, Node end, Heuristic h)
        {
            switch(h)
            {
                case Heuristic.Euclidean:
                    return Math.EuclideonDistance3D(current.Position, end.Position);
                case Heuristic.Manhattan:
                    return Math.ManhattanDistance3D(current.Position, end.Position);
                case Heuristic.Chebyshev:
                    return Math.ChebyshevDistance3D(current.Position, end.Position);
                case Heuristic.Dijkstra:
                    return 0;
                default:
                    return 0;
            }
        }
    }


}
