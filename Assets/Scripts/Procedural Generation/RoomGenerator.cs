/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/14/2026
 * Last Modified:   09/14/2026 (Ryan)
 * Notes:           Room Generator
*/

using RyansLibrary.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class RoomGenerator
    {
        private MapGenerationContext _context;
        private BlueprintParser _parser;
        private int _gridUnitSize;          // Conventional size of a 1:1 pathEntry
        private Transform _roomContainer;   // GameObject that will hold rooms

        public RoomGenerator(MapGenerationContext context, int gridUnitSize, Transform roomContainer)
        {
            _context = context;
            _parser = new BlueprintParser(_context.BlueprintDictionary);

            _gridUnitSize = gridUnitSize;
            _roomContainer = roomContainer;
        }

        public void ParsePathAndGenerateRooms(Path path)
        {
            foreach (Blueprint bp in path.BlueprintList)
            {
                if (!bp.Available)
                    continue;

                // Parse blueprints and return all candidates
                List<ShapeCandidate> candidates = _parser.CheckValidShapes(bp, path.RoomShapes.Select(e => e.RoomShape).ToList());

                // Sort candidates into buckets based on ShapeData
                Dictionary<ShapeData, List<ShapeCandidate>> buckets = BucketAllCandidates(candidates);

                // Choose a random candidate from a weighted selection
                ShapeCandidate candidate = PickWeightedCandidate(path.RoomShapes, buckets);

                // Choose a random room from a ShapeData and spawn
                PathEntry pathEntry = RandomRoomSelection(path.RoomShapes, candidate.Shape);
                Room room = GenerateRoom(path, pathEntry.Prefab, candidate.Cell);
            }
        }

        private Dictionary<ShapeData, List<ShapeCandidate>> BucketAllCandidates(List<ShapeCandidate> candidates)
        {
            if (candidates == null)
                return null;

            Dictionary<ShapeData, List<ShapeCandidate>> buckets = new();
            foreach (var candidate in candidates)
            {
                // Create a new bucket
                if (!buckets.TryGetValue(candidate.Shape, out var bucket))
                {
                    bucket = new List<ShapeCandidate>();
                    buckets.Add(candidate.Shape, bucket);
                }

                // Add to an existing bucket
                bucket.Add(candidate);
            }

            return buckets;
        }

        // TODO: Chage the probability code in Probability.cs to use an Interface instead of a ProbabilityEntry
        private ShapeCandidate PickWeightedCandidate(List<RoomShapeEntry> entries, Dictionary<ShapeData, List<ShapeCandidate>> buckets)
        {
            // Only shapes that actually produced candidates get an entry
            List<ProbabilityEntry<List<ShapeCandidate>>> weightedBuckets = new();
            foreach (var entry in entries)
            {
                if (!buckets.TryGetValue(entry.RoomShape, out var bucket))
                    continue;

                // Probability is 0-1, Probability is an int weight
                int weight = Mathf.RoundToInt(entry.Probability * 100);

                weightedBuckets.Add(new ProbabilityEntry<List<ShapeCandidate>>
                {
                    Probability = weight,
                    Object = bucket
                });
            }

            // ChooseRandomFromWeights reads entries[0] on an empty list
            if (weightedBuckets.Count <= 0)
                return null;

            List<ShapeCandidate> chosenBucket = Probability<List<ShapeCandidate>>.ChooseRandomFromWeights(weightedBuckets).Object;
            return chosenBucket[Random.Range(0, chosenBucket.Count)];
        }

        /// <summary>
        /// Picks one pathEntry for the given shape, weighted by each PathEntry's Probability.
        /// </summary>
        private PathEntry RandomRoomSelection(List<RoomShapeEntry> entries, ShapeData shape)
        {
            if (shape == null)
                return new PathEntry();

            // Gather every pathEntry that can be built with this shape
            List<ProbabilityEntry<PathEntry>> weightedRooms = new();
            foreach (var entry in entries)
            {
                if (entry.RoomShape != shape || entry.Rooms == null)
                    continue;

                foreach (var room in entry.Rooms)
                {
                    // Skip rooms that can never spawn or have no prefab to spawn
                    if (room.Probability <= 0 || room.Prefab == null)
                        continue;

                    weightedRooms.Add(new ProbabilityEntry<PathEntry>
                    {
                        Probability = room.Probability,
                        Object = room
                    });
                }
            }

            // ChooseRandomFromWeights reads entries[0] on an empty list
            if (weightedRooms.Count <= 0)
            {
                Debug.LogError($"No spawnable rooms found for shape {shape.name}.");
                return new PathEntry();
            }

            return Probability<PathEntry>.ChooseRandomFromWeights(weightedRooms).Object;
        }

        private Room GenerateRoom(Path path, GameObject prefab, Vector3Int placementPosition)
        {
            Quaternion rotation = Quaternion.identity;      // TODO: set rotation
            Room generatedRoom = Object.Instantiate(prefab, ConvertToWorldCoords(placementPosition), rotation, _roomContainer).GetComponent<Room>();

            generatedRoom.transform.parent = _roomContainer;
            path.Add(generatedRoom);
            return generatedRoom;
        }

        #region Utility
        // Vector based conversion from room -> world coords
        private Vector3 ConvertToWorldCoords(Vector3Int roomCoords)
        {
            int xComp = roomCoords.x * _gridUnitSize;
            int yComp = roomCoords.y * _gridUnitSize;
            int zComp = roomCoords.z * _gridUnitSize;
            return new Vector3(xComp, yComp, zComp);
        }
        #endregion
    }
}
