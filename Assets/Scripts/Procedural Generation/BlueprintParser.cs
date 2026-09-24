/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/01/2026
 * Last Modified:   09/23/2026 (Ryan)
 * Notes:           Data-only definition of a parsible _roomShape's footprint
*/
using System.Collections.Generic;
using UnityEngine;
using static RyansLibrary.Utilities.Math;

namespace RyansLibrary.Labyrinth
{
    public class ShapeCandidate
    {
        private ShapeData _shape;
        public ShapeData Shape => _shape;
        private Vector3Int _anchor;
        public Vector3Int Anchor => _anchor;
        private RoomRotation _rotation;
        public RoomRotation Rotation => _rotation;
        private readonly HashSet<Vector3Int> _coveredCells;
        public HashSet<Vector3Int> CoveredCells => _coveredCells;
        public int PassedCells => _coveredCells.Count;

        private bool _isFilled;
        public bool IsFilled => _isFilled;

        public ShapeCandidate(ShapeData shape, Vector3Int cell, RoomRotation rotation = RoomRotation.Deg0)
        {
            _shape = shape;
            _anchor = cell;
            _coveredCells = new();
            _rotation = rotation;
        }

        public bool CheckFilled()
        {
            if (_coveredCells.Count >= _shape.CellCount)
            {
                return true;
            }
            return false;
        }

        public void MarkFilled()
        {
            _isFilled = true;
        }

        public void CellCovered(Vector3Int shapeCell)
        {
            _coveredCells.Add(shapeCell);
        }
    }

    // Lexgen
    public class BlueprintParser
    {
        // Parser Directions
        // Modify this for 2D parser if nessessary
        private static readonly Vector3Int[] k_directions =
        {
            Vector3Int.left,
            Vector3Int.right,
            Vector3Int.forward,
            Vector3Int.back,
            Vector3Int.up,      // OMITTED FOR NOW
            Vector3Int.down,    // OMMITED FOR NOW
        };

        private readonly Dictionary<Vector3Int, Blueprint> _blueprintDictionary;
        private List<ShapeCandidate> _acceptedShapes;
        private Blueprint _baseBlueprint;

        public BlueprintParser(Dictionary<Vector3Int, Blueprint> blueprintDictionary)
        {
            _blueprintDictionary = blueprintDictionary;
        }

        /// <summary>
        /// Recursion-based parsing algorithm that eliminates shapes one by one until a final candidate list is found that
        /// fits a set of adjacent blueprints. Basically fitting shapes in holes like that toddler game.
        /// Top Level Function: Primes parser for new token check. Initilizes all needed data structures, and creates
        /// candidate entries from shapes.
        /// </summary>
        /// <param name="baseBlueprint">Starting point for token parsing.</param>
        /// <param name="possibleShapes">Possible tokens; should be all possible shapes in zone.</param>
        /// <returns>All possible candidates found that can fit a range of available blueprints.</returns>
        public List<ShapeCandidate> CheckValidShapes(Blueprint baseBlueprint, List<ShapeData> possibleShapes)
        {
            if (possibleShapes == null || possibleShapes.Count <= 0)
            {
                Debug.LogError("Parsing Failed - No possible shapes to parse.");
                return null;
            }

            if (!baseBlueprint.Available)
            {
                Debug.LogError("Parsing Failed - Base blueprint is not available to parse");
                return null;
            }

            HashSet<Blueprint> emptyVisitedSet = new();
            List<ShapeCandidate> candidates = new();
            _acceptedShapes = new();
            _baseBlueprint = baseBlueprint;

            // Create full list of candidates for each shape
            foreach (var shape in possibleShapes)
            {
                // Find all 'Blueprint' marked cells in shape
                var validCells = FindValidCells(baseBlueprint, shape);

                if (validCells.Count <= 0)      // Shape does not have any blueprint cells
                    continue;

                // Turn cells into candidates
                foreach (var cell in validCells)        // Add all 'Blueprint' cells
                {
                    for (int i = 0; i < 4; i++)         // Add all rotation factors
                    {
                        RoomRotation r = (RoomRotation)i;
                        ShapeCandidate newCandidate = new ShapeCandidate(shape, cell, r);
                        candidates.Add(newCandidate);
                    }
                }
            }

            if (candidates.Count <= 0)
            {
                Debug.LogError($"No viable origins found in any shape that can parse blueprint {baseBlueprint}");
                return null;
            }

            // Recursive Descent Based Parsing
            ParseBlueprints(baseBlueprint, candidates, emptyVisitedSet);

            return _acceptedShapes;
        }

        /// <summary>
        /// Recursion-based parsing algorithm that eliminates shapes one by one until a final candidate list is found that
        /// fits a set of adjacent blueprints. Basically fitting shapes in holes like that toddler game.
        /// </summary>
        /// <param name="currentBlueprint">Current blueprint being parsed</param>
        /// <param name="candidates">Candidate shapes are the tokens.</param>
        public void ParseBlueprints(Blueprint currentBlueprint, List<ShapeCandidate> candidates, HashSet<Blueprint> visited)
        {
            // We can only parse blueprints that are still available; not claimed
            if (!currentBlueprint.Available)
            {
                Debug.LogError("Tried to parse unavailable blueprint.");
                return;
            }

            // Base case; no viable shapes to check for next blueprint
            if (candidates.Count <= 0)
                return;

            // Shapes that pass this iteration have atleast one vaiable anchor
            List<ShapeCandidate> nextRoundCandidates = new List<ShapeCandidate>(candidates);

            // Make sure we don't visit a blueprint twice in a recursive branch
            HashSet<Blueprint> visitedBlueprintDictionary = new HashSet<Blueprint>(visited);

            // Local position from base blueprint
            Vector3Int localPosition = currentBlueprint.Position - _baseBlueprint.Position;

            // Check all candidates
            foreach (var candidate in nextRoundCandidates.ToArray())
            {
                // Shape cell that lands on this blueprint; undo the candidate's rotation, then offset from its anchor
                Vector3Int localPosFromAnchor = candidate.Anchor + candidate.Rotation.ToInverseMatrix() * localPosition;

                // If candidate passes
                if (CheckConfigs(localPosFromAnchor, candidate.Shape, currentBlueprint, candidate.Rotation))
                {
                    // Add passed cell to candidate
                    candidate.CellCovered(localPosFromAnchor);

                    // If all cells of shape are satisfied; first time satisfaction
                    if (candidate.CheckFilled() && !candidate.IsFilled)
                    {
                        candidate.MarkFilled();     // Prevents duplicate candidates from being pushed into the accepted list
                        _acceptedShapes.Add(candidate);
                    }

                    // Removed if already filled; prevents algorithm from checking multiple valid paths for one candidate
                    if (candidate.IsFilled)
                        nextRoundCandidates.Remove(candidate);

                }
                else  // Candidate did not pass
                {
                    nextRoundCandidates.Remove(candidate);
                }
            }
            visitedBlueprintDictionary.Add(currentBlueprint);

            // Parse new blueprint
            foreach (var direction in k_directions)
            {
                // Blueprint needs to exist and be available to walk into
                if (!ParserPeek(currentBlueprint, direction, out Blueprint found))
                    continue;

                // Already checked blueprint in this direction so don't parse
                if (visitedBlueprintDictionary.Contains(found))
                    continue;

                // Parse the neighbour with the shapes that are still viable
                ParseBlueprints(found, nextRoundCandidates, visitedBlueprintDictionary);
            }
        }

        /// <summary>
        /// Peek anywhere in the blueprint dictionary and return the blueprint at a position if
        /// found.
        /// </summary>
        /// <param name="blueprint">Base blueprint to peek from.</param>
        /// <param name="position">Where to peek in the dictionary</param>
        /// <param name="found">Blueprint that was found with peek; otherwise null</param>
        /// <returns></returns>
        private bool ParserPeek(Blueprint blueprint, Vector3Int position, out Blueprint found)
        {
            if (_blueprintDictionary.TryGetValue(blueprint.Position + position, out found))
            {
                if (found.Available)        // Blueprint needs to be available to parse
                    return true;
            }
            return false;
        }

        /// <summary>
        /// TODO: May want to add later to save on performance.
        /// Strips every candidate sharing a shape with the given candidate out of the list, retiring
        /// that shape from the parse. The passed candidate is removed too, since it matches its own shape.
        /// </summary>
        private void RemoveShapeFromCandidateList(ShapeCandidate candidate, List<ShapeCandidate> candidates)
        {
            if (candidates == null || candidate == null)
                return;

            candidates.RemoveAll(c => c.Shape == candidate.Shape);
        }

        public List<Vector3Int> FindValidCells(Blueprint blueprint, ShapeData shape)
        {
            List<Vector3Int> validCells = new();

            foreach (var cell in shape.Cells)
            {
                // Skip cells that are not marked as blueprint cells, since they cannot be origins
                if (cell.Value == CellState.Blueprint)
                {
                    validCells.Add(cell.Key);
                }
            }

            return validCells;
        }

        #region Check Configs
        public bool CheckConfigs(Vector3Int localPosition, ShapeData shapeData, Blueprint blueprint, RoomRotation rotation = RoomRotation.Deg0)
        {
            // Shape does not contain a cell at position, so it's an illegal check
            if (!shapeData.Cells.ContainsKey(localPosition))
                return false;

            CellState[] ShapeDataConfigs = new CellState[6];
            ShapeDataConfigs[0] = CheckSide(shapeData, localPosition, Vector3Int.right);
            ShapeDataConfigs[1] = CheckSide(shapeData, localPosition, Vector3Int.left);
            ShapeDataConfigs[2] = CheckSide(shapeData, localPosition, Vector3Int.forward);
            ShapeDataConfigs[3] = CheckSide(shapeData, localPosition, Vector3Int.back);
            ShapeDataConfigs[4] = CheckSide(shapeData, localPosition, Vector3Int.up);
            ShapeDataConfigs[5] = CheckSide(shapeData, localPosition, Vector3Int.down);

            // Each shape side faces a turned world direction once the shape is rotated
            Matrix3x3Int rotationMatrix = rotation.ToMatrix();

            CellState[] BlueprintConfigs = new CellState[6];
            BlueprintConfigs[0] = CheckSide(blueprint, rotationMatrix * Vector3Int.right);
            BlueprintConfigs[1] = CheckSide(blueprint, rotationMatrix * Vector3Int.left);
            BlueprintConfigs[2] = CheckSide(blueprint, rotationMatrix * Vector3Int.forward);
            BlueprintConfigs[3] = CheckSide(blueprint, rotationMatrix * Vector3Int.back);
            BlueprintConfigs[4] = CheckSide(blueprint, rotationMatrix * Vector3Int.up);
            BlueprintConfigs[5] = CheckSide(blueprint, rotationMatrix * Vector3Int.down);

            for (int i = 0; i < 6; i++)
            {
                if (ShapeDataConfigs[i] == CellState.DontCare)
                {
                    continue;
                }
                else if (ShapeDataConfigs[i] != BlueprintConfigs[i])
                {
                    return false; // Configurations do not match
                }
            }
            return true; // All configurations match
        }

        private CellState CheckSide(ShapeData data, Vector3Int origin, Vector3Int offset)
        {
            if (data.Cells.TryGetValue(origin + offset, out var cellState))
            {
                // Handle the case where the side cell is found
                return cellState;
            }
            else
            {
                // Handle the case where the side cell is not found
                return CellState.DontCare;
            }
        }

        private CellState CheckSide(Blueprint blueprint, Vector3Int offset)
        {
            if (_blueprintDictionary.TryGetValue(blueprint.Position + offset, out var bp))
            {
                // Handle the case where the side cell is found
                return CellState.Blueprint;
            }
            else
            {
                // Handle the case where the side cell is not found
                return CellState.NoBlueprint;
            }
        }
        #endregion
    }
}
