/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/14/2026
 * Last Modified:   09/23/2026 (Ryan)
 * Notes:           Room Generator
*/
using RyansLibrary.Utilities;
using System.Collections.Generic;
using UnityEngine;
using static RyansLibrary.Utilities.Math;
using Random = UnityEngine.Random;      // Using Unity Engine's Random not System.Collection's Random

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
            // Null args are reported when generation is attempted (see IsGeneratorValid), not here
            _context = context;
            _parser = _context != null ? new BlueprintParser(_context.BlueprintDictionary) : null;

            _gridUnitSize = gridUnitSize;
            _roomContainer = roomContainer;
        }

        public bool ParsePathAndGenerateRooms(Path path)
        {
            if (!IsGeneratorValid())
                return false;

            if (path == null || !path.IsInitialized)
            {
                Debug.LogError($"Path {(path == null ? "null" : path.Name)} was null or not initialized.");
                return false;
            }

            if (path.BlueprintList == null || path.BlueprintCount <= 0)
            {
                Debug.LogError($"Path {path.Name} blueprint list was null or has no blueprints to parse");
                return false;
            }

            if (!TryGetRoomShapes(path, out List<ShapeData> shapes))
                return false;

            foreach (Blueprint currentBlueprint in path.BlueprintList)
            {
                if (!currentBlueprint.Available)
                    continue;

                // Parse blueprints and return all candidates
                List<ShapeCandidate> candidates = _parser.CheckValidShapes(currentBlueprint, shapes);

                if (candidates == null || candidates.Count <= 0)
                {
                    Debug.LogError($"Parsing failed on blueprint {currentBlueprint.BlueprintID} in path {path.Name}. " +
                        $"No valid candidates found.");
                    return false;
                }

                // Sort candidates into buckets based on ShapeData
                BucketCollection<ShapeData, ShapeCandidate> buckets = BucketAllCandidates(candidates);

                if (buckets == null)
                {
                    Debug.LogError($"Parsing failed on blueprint {currentBlueprint.BlueprintID} in path {path.Name}. " +
                        $"No valid candidate buckets made.");
                    return false;
                }

                // Choose a random candidate from a weighted selection
                ShapeCandidate candidate = PickWeightedCandidate(path.RoomShapes, buckets);

                if (candidate == null)
                {
                    Debug.LogError($"Parsing failed on blueprint {currentBlueprint.BlueprintID} in path {path.Name}. " +
                        $"No candidates chosen from bucketed list.");
                    return false;
                }

                // Choose a random room from a ShapeData
                RoomEntry pathEntry = SelectRandomRoomFromShape(path.RoomShapes, candidate.Shape);

                if (pathEntry.Prefab == null)
                {
                    Debug.LogError($"Parsing failed on blueprint {currentBlueprint.BlueprintID} in path {path.Name}. " +
                        $"No room chosen from shape {candidate.Shape}");
                    return false;
                }

                // Spawn room and make all overlapping blueprints unavailable; the anchor offset turns with the room
                Vector3Int placementPosition = currentBlueprint.Position - candidate.Rotation.ToMatrix() * candidate.Anchor;
                Room room = GenerateRoom(path, pathEntry.Prefab, placementPosition, candidate.Rotation);

                if (room == null)
                {
                    Debug.LogError($"Parsing failed on blueprint {currentBlueprint.BlueprintID} in path {path.Name}. " +
                        $"No room generated.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Pulls the ShapeData out of every ShapeEntry in the path. Fails if there are no entries or any entry is
        /// missing its ShapeData, since the parser can't handle a null shape.
        /// </summary>
        internal bool TryGetRoomShapes(Path path, out List<ShapeData> shapes)
        {
            shapes = null;

            if (path.RoomShapes == null || path.RoomShapes.Count <= 0)
            {
                Debug.LogError($"No shapes exist for path {path.Name}");
                return false;
            }

            shapes = new List<ShapeData>(path.RoomShapes.Count);
            foreach (ShapeEntry entry in path.RoomShapes)
            {
                if (entry.RoomShape == null || entry.RoomShape.Cells == null)
                {
                    Debug.LogError($"Path {path.Name} has a ShapeEntry with no ShapeData or no cells.");
                    shapes = null;
                    return false;
                }

                shapes.Add(entry.RoomShape);
            }

            return true;
        }

        internal BucketCollection<ShapeData, ShapeCandidate> BucketAllCandidates(List<ShapeCandidate> candidates)
        {
            if (candidates == null || candidates.Count <= 0)
            {
                Debug.LogError("Candidate list was null or empty.");
                return null;
            }

            // Bucket all candidates by shape
            BucketCollection<ShapeData, ShapeCandidate> buckets = new();
            foreach (var candidate in candidates)
            {
                if (candidate == null)
                {
                    Debug.LogError("Candidate was null");
                    return null;
                }

                if (candidate.Shape == null)
                {
                    Debug.LogError("Candidate has no shape.");
                    return null;
                }

                // Create new bucket and add candidate
                buckets.AddItemToBucket(candidate.Shape, candidate);
            }

            return buckets;
        }

        /// <summary>
        /// Picks a shape weighted by its ShapeEntry._weight, then a random candidate of that shape.
        /// </summary>
        internal ShapeCandidate PickWeightedCandidate(List<ShapeEntry> entries, BucketCollection<ShapeData, ShapeCandidate> buckets)
        {
            if (buckets == null || buckets.IsEmpty())
            {
                Debug.LogError("Bucket collection was null or empty.");
                return null;
            }
            if (entries == null || entries.Count <= 0)
            {
                Debug.LogError("Room shape entries was null or empty.");
                return null;
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
                        Debug.LogError("Bucket has no items or does not exist.");
                        return null;
                    }
                }
            }

            // No shapes eligible due to bucket being empty or no buckets existing for all shapes
            if (eligibleShapes.Count <= 0)
            {
                Debug.LogError("No shapes are eligible for picking. Were buckets empty? Did all shapes have an existing bucket?");
                return null;
            }

            // Choose a random shape with weights
            if (!WeightedRandom.TryPick(eligibleShapes, out ShapeEntry chosenShape))
            {
                Debug.LogError("No shape chosen for generation. Were all their probabilities 0?");
                return null;
            }

            // Choose a random candidate w/o weights
            if (buckets.TryGetBucket(chosenShape.RoomShape, out List<ShapeCandidate> chosenBucket))
            {
                ShapeCandidate chosenCandidate = chosenBucket[Random.Range(0, chosenBucket.Count)];

                if (chosenCandidate == null)
                {
                    Debug.LogError("Chosen candidate was null.");
                    return null;
                }

                return chosenCandidate;
            }
            else
            {
                Debug.LogError("Bucket does not exist for chosen shape.");
                return null;
            }
        }

        /// <summary>
        /// Picks one pathEntry for the given shape, weighted by each RoomEntry's _weight.
        /// </summary>
        /// <param name="entries">Room-shape entries from a path</param>
        /// <param name="shape">Shape to </param>
        /// <returns></returns>
        internal RoomEntry SelectRandomRoomFromShape(List<ShapeEntry> entries, ShapeData shape)
        {
            if (entries == null || entries.Count <= 0)
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
                // Skip entry if it has no shape or shapes don't match
                if (entry.RoomShape == null || entry.RoomShape != shape)
                    continue;

                if (entry.Rooms == null || entry.Rooms.Count <= 0)
                {
                    Debug.LogError("A ShapeEntry contains no rooms.");
                    return new RoomEntry();
                }

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

        /// <summary>
        /// Spawns a room and claims the blueprints under its cells. Every check runs before anything is spawned or
        /// claimed, so a failed placement leaves the scene and blueprint grid untouched.
        /// </summary>
        /// <param name="rotation">Turns the room, and the cells it claims, about its origin cell</param>
        public Room GenerateRoom(Path path, GameObject prefab, Vector3Int placementPosition, RoomRotation rotation = RoomRotation.Deg0)
        {
            if (!IsGeneratorValid())
                return null;

            if (path == null || !path.IsInitialized)
            {
                Debug.LogError("Room generation failed - path was null or not initialized.");
                return null;
            }

            if (prefab == null)
            {
                Debug.LogError("Room generation failed - prefab was null.");
                return null;
            }

            if (!prefab.TryGetComponent(out Room prefabRoom))
            {
                Debug.LogError($"Room generation failed - prefab {prefab.name} has no Room component.");
                return null;
            }

            if (prefabRoom.RoomCells == null || prefabRoom.RoomCells.Count <= 0)
            {
                Debug.LogError($"Room generation failed - prefab {prefab.name} has no room cells.");
                return null;
            }

            // Find the blueprint under every (rotated) room cell; placement is illegal if one is missing or already claimed
            Matrix3x3Int rotationMatrix = rotation.ToMatrix();
            List<Blueprint> cellBlueprints = new(prefabRoom.RoomCells.Count);
            foreach (RoomCell cell in prefabRoom.RoomCells)
            {
                Vector3Int positionInWorld = placementPosition + rotationMatrix * cell.Position;

                if (!_context.BlueprintDictionary.TryGetValue(positionInWorld, out var blueprint))
                {
                    Debug.LogError($"No blueprint exists at this room's cell's position in world {positionInWorld}");
                    return null;
                }

                if (!blueprint.Available)
                {
                    Debug.LogError($"Room generation failed - blueprint at {positionInWorld} is already claimed by another room.");
                    return null;
                }

                cellBlueprints.Add(blueprint);
            }

            // Same turn as rotationMatrix so the room's geometry sits on the blueprints it claims
            Quaternion worldRotation = Quaternion.Euler(0f, 90f * (int)rotation, 0f);
            Room generatedRoom = Object.Instantiate(prefabRoom, ConvertToWorldCoords(placementPosition), worldRotation, _roomContainer);

            // Disable overlapping blueprint availability; clone's cells are in the same order as the prefab's
            for (int i = 0; i < cellBlueprints.Count; i++)
            {
                cellBlueprints[i].Available = false;
                generatedRoom.CopyBlueprintEntranceFlags(cellBlueprints[i], generatedRoom.RoomCells[i], rotation);
            }

            generatedRoom.Initialize();

            path.Add(generatedRoom);
            return generatedRoom;
        }

        /// <summary>
        /// Spawn a room given a position and direction; room type not passed as room is expected to
        /// already know it's type if unique (FOR NOW BUT MAYBE NOT LATER)
        /// For random room placement algorithm to use.
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="placementPosition"></param>
        /// <param name="rDir"></param>
        /// <returns></returns>
        public Room GenerateRoom(Path path, GameObject prefab, Vector3Int placementPosition)
        {
            Quaternion rotation = Quaternion.identity;      // TODO: set rotation
            Room generatedRoom = Object.Instantiate(prefab, ConvertToWorldCoords(placementPosition), rotation, _roomContainer).GetComponent<Room>();

            path.Add(generatedRoom);
            return generatedRoom;
        }

        /// <summary>
        /// Checks the constructor args before any generation is attempted.
        /// </summary>
        private bool IsGeneratorValid()
        {
            bool isValid = true;

            if (_context == null)
            {
                Debug.LogError("RoomGenerator has no MapGenerationContext.");
                isValid = false;
            }
            if (_roomContainer == null)
            {
                Debug.LogError("RoomGenerator has no room container to place rooms in.");
                isValid = false;
            }
            if (_gridUnitSize <= 0)
            {
                Debug.LogError($"RoomGenerator grid unit size must be positive; was {_gridUnitSize}.");
                isValid = false;
            }

            return isValid;
        }

        #region Utility
        // Vector based conversion from room -> world coords
        private Vector3 ConvertToWorldCoords(Vector3Int roomCoords)
        {
            return roomCoords * _gridUnitSize;
        }
        #endregion
    }
}
