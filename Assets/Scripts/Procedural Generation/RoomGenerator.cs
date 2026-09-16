/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/14/2026
 * Last Modified:   09/14/2026 (Ryan)
 * Notes:           Room Generator
*/

using RyansLibrary.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// A simple hash map with buckets.
    /// </summary>
    /// <typeparam name="L">Key</typeparam>
    /// <typeparam name="B">BucketCollection Object</typeparam>
    public class BucketCollection<L, B>
    {
        private Dictionary<L, List<B>> _bucketDict;

        public BucketCollection()
        {
            _bucketDict = new Dictionary<L, List<B>>();
        }

        public void AddItemToBucket(L bucketKey, B bucketItem)
        {
            // Create a new bucket
            if (!_bucketDict.TryGetValue(bucketKey, out var bucket))
            {
                // Bucket is null so initialize
                bucket = new List<B>();
                _bucketDict.Add(bucketKey, bucket);
            }

            // Add to an existing bucket
            bucket.Add(bucketItem);
        }

        public bool TryGetBucket(L bucketKey, out List<B> bucket)
        {
            if (_bucketDict.TryGetValue(bucketKey, out bucket))
            {
                return true;
            }

            bucket = null;
            return false;
        }
    }

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
                BucketCollection<ShapeData, ShapeCandidate> buckets = BucketAllCandidates(candidates);

                // Choose a random candidate from a weighted selection
                ShapeCandidate candidate = PickWeightedCandidate(path.RoomShapes, buckets);

                if (candidate == null)
                    continue;

                // Choose a random room from a ShapeData and spawn
                RoomEntry pathEntry = RandomRoomSelection(path.RoomShapes, candidate.Shape);
                if (pathEntry.Prefab == null)
                    continue;

                Vector3Int placementPosition = bp.Position - candidate.Cell;
                Room room = GenerateRoom(path, pathEntry.Prefab, placementPosition);
            }
        }

        private BucketCollection<ShapeData, ShapeCandidate> BucketAllCandidates(List<ShapeCandidate> candidates)
        {
            if (candidates == null)
                return null;

            BucketCollection<ShapeData, ShapeCandidate> buckets = new();
            foreach (var candidate in candidates)
            {
                buckets.AddItemToBucket(candidate.Shape, candidate);
            }

            return buckets;
        }

        // TODO: Chage the probability code in Probability.cs to use an Interface instead of a ProbabilityEntry
        private ShapeCandidate PickWeightedCandidate(List<RoomShapeEntry> entries, BucketCollection<ShapeData, ShapeCandidate> buckets)
        {
            if (buckets == null)
                return null;

            // Only shapes that actually produced candidates get an entry
            List<ProbabilityEntry<List<ShapeCandidate>>> weightedBuckets = new();
            foreach (var entry in entries)
            {
                if (!buckets.TryGetBucket(entry.RoomShape, out var bucket))
                    continue;

                weightedBuckets.Add(new ProbabilityEntry<List<ShapeCandidate>>
                {
                    Probability = entry.Probability,
                    Object = bucket
                });
            }

            if (weightedBuckets.Count <= 0)
                return null;

            // Choose a random bucket with weights
            List<ShapeCandidate> chosenBucket = Probability<List<ShapeCandidate>>.ChooseRandomFromWeights(weightedBuckets).Object;

            // Choose a random candidate w/o weights
            return chosenBucket[Random.Range(0, chosenBucket.Count)];
        }

        /// <summary>
        /// Picks one pathEntry for the given shape, weighted by each RoomEntry's Probability.
        /// </summary>
        private RoomEntry RandomRoomSelection(List<RoomShapeEntry> entries, ShapeData shape)
        {
            if (shape == null)
                return new RoomEntry();

            // Gather every pathEntry that can be built with this shape
            List<ProbabilityEntry<RoomEntry>> weightedRooms = new();
            foreach (var entry in entries)
            {
                if (entry.RoomShape != shape || entry.Rooms == null)
                    continue;

                foreach (var room in entry.Rooms)
                {
                    // Skip rooms that can never spawn or have no prefab to spawn
                    if (room.Probability <= 0 || room.Prefab == null)
                        continue;

                    weightedRooms.Add(new ProbabilityEntry<RoomEntry>
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
                return new RoomEntry();
            }

            return Probability<RoomEntry>.ChooseRandomFromWeights(weightedRooms).Object;
        }

        private Room GenerateRoom(Path path, GameObject prefab, Vector3Int placementPosition)
        {
            Quaternion rotation = Quaternion.identity;      // TODO: set rotation
            Room generatedRoom = Object.Instantiate(prefab, ConvertToWorldCoords(placementPosition), rotation, _roomContainer).GetComponent<Room>();

            // Scan all room cells and disable overlapping blueprint availability
            foreach (RoomCell cell in generatedRoom.RoomCells)
            {
                Vector3Int positionInWorld = cell.Position + placementPosition;

                if (_context.BlueprintDictionary.TryGetValue(positionInWorld, out var blueprint))
                {
                    blueprint.Available = false;
                    generatedRoom.CopyBlueprintEntranceFlags(blueprint, cell);
                }
                else
                    Debug.LogError("No blueprint exists at this room's cell.");
            }

            generatedRoom.Initialize();

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
