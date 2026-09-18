/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/14/2026
 * Last Modified:   09/17/2026 (Ryan)
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
    /// <typeparam name="L">Bucket Key</typeparam>
    /// <typeparam name="B">Bucket Collection Object</typeparam>
    public class BucketCollection<L, B>
    {
        private Dictionary<L, List<B>> _bucketDict;

        public int BucketCount => _bucketDict.Count;

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

        public int GetCountInBucket(L bucketKey)
        {
            if (TryGetBucket(bucketKey, out var bucket))
            {
                return bucket.Count;
            }
            else
            {
                Debug.LogWarning($"Bucket with key {bucketKey} does not exist.");
                return 0;
            }
        }

        public bool IsBucketEmpty(L bucketKey)
        {
            if (TryGetBucket(bucketKey, out var bucket))
            {
                return bucket.Count <= 0;
            }
            else
            {
                Debug.LogWarning($"Bucket with key {bucketKey} does not exist.");
                return false;
            }
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

        public bool ParsePathAndGenerateRooms(Path path)
        {
            if (path == null || !path.IsInitialized)
            {
                Debug.LogError($"Path {path.Name} was null or not initialized.");
                return false;
            }

            if (path.BlueprintList == null || path.BlueprintCount <= 0)
            {
                Debug.LogError($"Path {path.Name} blueprint list was null or has no blueprints to parse");
            }

            foreach (Blueprint currrentBlueprint in path.BlueprintList)
            {
                if (!currrentBlueprint.Available)
                    continue;

                // Parse blueprints and return all candidates
                List<ShapeCandidate> candidates = _parser.CheckValidShapes(currrentBlueprint, path.RoomShapes.Select(e => e.RoomShape).ToList());

                if (candidates == null || candidates.Count <= 0)
                {
                    Debug.Log($"Parsing failed on blueprint {currrentBlueprint.BlueprintID} in path {path.name}. " +
                        $"No valid candidates found.");
                    return false;
                }

                // Sort candidates into buckets based on ShapeData
                BucketCollection<ShapeData, ShapeCandidate> buckets = BucketAllCandidates(candidates);

                // TODO: Dead code; Claude said to remove I guess.
                if (buckets == null || buckets.BucketCount < 0)
                {
                    Debug.Log($"Parsing failed on blueprint {currrentBlueprint.BlueprintID} in path {path.name}. " +
                        $"No valid candidate buckets made.");
                    return false;
                }

                // Choose a random candidate from a weighted selection
                ShapeCandidate candidate = PickWeightedCandidate(path.RoomShapes, buckets);

                if (candidate == null)
                {
                    Debug.Log($"Parsing failed on blueprint {currrentBlueprint.BlueprintID} in path {path.name}. " +
                        $"No candidates choosen from bucketed list.");
                    return false;
                }

                // Choose a random room from a ShapeData
                RoomEntry pathEntry = SelectRandomRoomFromShape(path.RoomShapes, candidate.Shape);

                if (pathEntry.Prefab == null)
                {
                    Debug.Log($"Parsing failed on blueprint {currrentBlueprint.BlueprintID} in path {path.name}. " +
                        $"No room choosen from shape {candidate.Shape}");
                    return false;
                }

                // Spawn room and make all overlapping blueprints unavailable
                Vector3Int placementPosition = currrentBlueprint.Position - candidate.Cell;
                Room room = GenerateRoom(path, pathEntry.Prefab, placementPosition);

                if (room == null)
                {
                    Debug.Log($"Parsing failed on blueprint {currrentBlueprint.BlueprintID} in path {path.name}. " +
                        $"No room generated.");
                    return false;
                }
            }

            return true;
        }

        private BucketCollection<ShapeData, ShapeCandidate> BucketAllCandidates(List<ShapeCandidate> candidates)
        {
            if (candidates == null)
            {
                Debug.LogError("Candidate list was null or empty.");
                return null;
            }

            // Bucket all candidates by shape
            BucketCollection<ShapeData, ShapeCandidate> buckets = new();
            foreach (var candidate in candidates)
            {
                buckets.AddItemToBucket(candidate.Shape, candidate);
            }

            return buckets;
        }

        /// <summary>
        /// Picks a shape weighted by its ShapeEntry._weight, then a random candidate of that shape.
        /// </summary>
        private ShapeCandidate PickWeightedCandidate(List<ShapeEntry> entries, BucketCollection<ShapeData, ShapeCandidate> buckets)
        {
            if (buckets == null)
            {
                Debug.LogError("Bucket collection was null.");
                return null;
            }
            if (entries == null || entries.Count <= 0)
            {
                Debug.LogError("Room shape entries was null or empty.");
            }

            // Shapes that passed parse go on to next process; only shapes that actually produced candidates are eligible
            List<ShapeEntry> eligibleShapes = new();
            foreach (ShapeEntry entry in entries)
            {
                if (buckets.TryGetBucket(entry.RoomShape, out _))    // Bucket exists
                {
                    if (!buckets.IsBucketEmpty(entry.RoomShape))     // Bucket has at least one candidate
                        eligibleShapes.Add(entry);
                    else
                    {
                        Debug.LogError("Bucket has no items.");
                        return null;
                    }
                }
            }

            // No shapes eligable due to bucket being empty or no buckets existing for all shapes
            if (eligibleShapes.Count <= 0)
            {
                Debug.LogError("No shapes are eligible for picking. Were buckets empty? Did all shapes have an existing bucket?");
                return null;
            }

            // Choose a random shape with weights
            if (!WeightedRandom.TryPick(eligibleShapes, out ShapeEntry chosenShape))
            {
                Debug.LogError("No shape choosen for generation. Were all their probabilities 0?");
                return null;
            }

            // Choose a random candidate w/o weights
            if (buckets.TryGetBucket(chosenShape.RoomShape, out List<ShapeCandidate> chosenBucket))
            {
                ShapeCandidate choosenCandidate = chosenBucket[Random.Range(0, chosenBucket.Count)];

                if (choosenCandidate == null)
                {
                    Debug.LogError("Choosen candidate was null.");
                    return null;
                }

                return choosenCandidate;
            }
            else
            {
                Debug.LogError("Bucket does not exist for choosen shape.");
                return null;
            }
        }

        /// <summary>
        /// Picks one pathEntry for the given shape, weighted by each RoomEntry's _weight.
        /// </summary>
        /// <param name="entries">Room-shape entries from a path</param>
        /// <param name="shape">Shape to </param>
        /// <returns></returns>
        private RoomEntry SelectRandomRoomFromShape(List<ShapeEntry> entries, ShapeData shape)
        {
            if (entries == null || entries.Count <= 0 || shape == null)
            {
                Debug.LogError("Shape entry list was null or empty.");
                return new RoomEntry();
            }
            if (shape == null)
            {
                Debug.LogError("ShapeData was null");
                return new RoomEntry();
            }

            // Only the rooms that match with the ShapeData can advance to next process
            List<RoomEntry> rooms = new();
            foreach (ShapeEntry entry in entries)
            {
                // Skip entry if shapes don't match; skip entry if it has no rooms
                if (entry.RoomShape != shape || entry.RoomShape == null)
                    continue;

                foreach (RoomEntry room in entry.Rooms)
                {
                    // Skip rooms with no prefab to spawn; TryPick already skips zero weights
                    if (room.Prefab != null)
                        rooms.Add(room);
                }
            }

            // Choose a random room with shapes
            if (!WeightedRandom.TryPick(rooms, out RoomEntry pickedRoom))
            {
                Debug.LogError($"No spawnable rooms found for shape {shape.name}.");
                return new RoomEntry();
            }

            return pickedRoom;
        }

        private Room GenerateRoom(Path path, GameObject prefab, Vector3Int placementPosition)
        {
            Quaternion rotation = Quaternion.identity;
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
                {
                    Debug.LogError($"No blueprint exists at this room's cell's postion in world {positionInWorld}");
                    return null;
                }
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
