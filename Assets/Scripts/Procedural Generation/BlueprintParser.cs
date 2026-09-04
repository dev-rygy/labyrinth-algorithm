/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/01/2026
 * Last Modified:   09/01/2026 (Ryan)
 * Notes:           Data-only definition of a parsible RoomShape's footprint
*/
using System.Collections.Generic;
using UnityEngine;

namespace RyansLibrary.Labyrinth
{
    public class ShapeCandidate
    {
        private ShapeData _shape;
        public ShapeData Shape => _shape;
        private Vector3Int _cell;
        public Vector3Int Cell => _cell;
        private int _passedCells;
        public int PassedCells => _passedCells;

        public ShapeCandidate(ShapeData shape, Vector3Int cell)
        {
            _shape = shape;
            _cell = cell;
            _passedCells = 0;
        }

        public bool CheckFilled()
        {
            if (_passedCells >= _shape.CellCount)
            {
                return true;
            }
            return false;
        }

        public void CellPassed()
        {
            _passedCells++;
        }
    }

    // Lexgen
    public class BlueprintParser
    {
        private Dictionary<Vector3Int, Blueprint> _blueprintDictionary;
        private Dictionary<Vector3Int, Blueprint> _checkedBlueprintDictionary;
        private Stack<ShapeCandidate> _acceptedShapes;
        private Blueprint _baseBlueprint;

        public BlueprintParser(Dictionary<Vector3Int, Blueprint> blueprintDictionary)
        {
            _blueprintDictionary = blueprintDictionary;
        }

        public Stack<ShapeCandidate> CheckValidShapes(Blueprint baseBlueprint, List<ShapeData> possibleShapes)
        {
            if (possibleShapes.Count <= 0)
            {
                Debug.LogError("No shapes to parse.");
                return null;
            }

            _checkedBlueprintDictionary = new();
            _acceptedShapes = new();
            List<ShapeCandidate> candidates = new();
            _baseBlueprint = baseBlueprint;

            // Check all shapes for valid origins
            foreach (var shape in possibleShapes)
            {
                var validCells = CheckForValidCells(baseBlueprint, shape);

                if (validCells.Count <= 0)      // Shape does not have any blueprint cells
                    continue;

                // Turn cells into candidates
                foreach (var cell in validCells)
                {
                    ShapeCandidate newCandidate = new ShapeCandidate(shape, cell);
                    candidates.Add(newCandidate);
                }
            }

            if (candidates.Count <= 0)
            {
                Debug.LogError($"No viable origins found in any shape that can parse blueprint {baseBlueprint}");
                return null;
            }

            // Recursive Descent Based Parsing
            ParseBlueprints(baseBlueprint, candidates);

            return _acceptedShapes;
        }

        public void ParseBlueprints(Blueprint currentBlueprint, List<ShapeCandidate> candidates)
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

            // Shapes that pass this iteration have atleast one vaiable origin; Copy candidate list and 
            // then we just remove candidates one by one.
            List<ShapeCandidate> nextRoundCandidates = new List<ShapeCandidate>(candidates);

            // Local position from base blueprint
            Vector3Int localPosition = currentBlueprint.Position - _baseBlueprint.Position;

            foreach (var candidate in candidates)
            {
                Vector3Int localPosFromCell = candidate.Cell - localPosition;

                // If candidate passes 
                if (CheckConfigs(localPosFromCell, candidate.Shape, currentBlueprint))
                {
                    candidate.CellPassed();

                    // If all cells of shape are satisfied
                    if (candidate.CheckFilled())
                    {
                        // Remove any candidates with same shape from the next round
                        RemoveShapeFromCandidateList(candidate, nextRoundCandidates);
                        _acceptedShapes.Push(candidate);
                    }
                }
                else  // Candidate did not pass
                {
                    nextRoundCandidates.Remove(candidate);
                }
            }
            _checkedBlueprintDictionary.Add(currentBlueprint.Position, currentBlueprint);

            Blueprint found;

            // Try peeking left
            if (ParserPeek(currentBlueprint, Vector3Int.left, out found))
            {
                if (!_checkedBlueprintDictionary.ContainsKey(found.Position))
                {
                    // Parse left blueprint with remaining viable shapes that are not already filled
                    ParseBlueprints(found, nextRoundCandidates);
                }
            }

            // Try peeking right
            if (ParserPeek(currentBlueprint, Vector3Int.right, out found))
            {
                if (!_checkedBlueprintDictionary.ContainsKey(found.Position))
                {
                    // Parse right blueprint with remaining viable shapes that are not already filled
                    ParseBlueprints(found, nextRoundCandidates);
                }
            }

            // Try peeking forward
            if (ParserPeek(currentBlueprint, Vector3Int.forward, out found))
            {
                if (!_checkedBlueprintDictionary.ContainsKey(found.Position))
                {
                    // Parse forward blueprint with remaining viable shapes that are not already filled
                    ParseBlueprints(found, nextRoundCandidates);
                }
            }

            // Try peeking back
            if (ParserPeek(currentBlueprint, Vector3Int.back, out found))
            {
                if (!_checkedBlueprintDictionary.ContainsKey(found.Position))
                {
                    // Parse back blueprint with remaining viable shapes that are not already filled
                    ParseBlueprints(found, nextRoundCandidates);
                }
            }

            // Try peeking up
            if (ParserPeek(currentBlueprint, Vector3Int.up, out found))
            {
                if (!_checkedBlueprintDictionary.ContainsKey(found.Position))
                {
                    // Parse up blueprint with remaining viable shapes that are not already filled
                    ParseBlueprints(found, nextRoundCandidates);
                }
            }

            // Try peeking down
            if (ParserPeek(currentBlueprint, Vector3Int.down, out found))
            {
                if (!_checkedBlueprintDictionary.ContainsKey(found.Position))
                {
                    // Parse down blueprint with remaining viable shapes that are not already filled
                    ParseBlueprints(found, nextRoundCandidates);
                }
            }
        }

        /// <summary>
        /// Strips every candidate sharing a shape with the given candidate out of the list, retiring
        /// that shape from the parse. The passed candidate is removed too, since it matches its own shape.
        /// </summary>
        private void RemoveShapeFromCandidateList(ShapeCandidate candidate, List<ShapeCandidate> candidates)
        {
            if (candidates == null || candidate == null)
                return;

            candidates.RemoveAll(c => c.Shape == candidate.Shape);
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

        public List<Vector3Int> CheckForValidOrigins(Blueprint blueprint, ShapeData shape)
        {
            List<Vector3Int> validCells = new();

            foreach (var cell in shape.Cells)
            {
                // Skip cells that are not marked as blueprint cells, since they cannot be origins
                if (cell.Value != CellState.Blueprint)
                {
                    continue;
                }

                // Check if the cell is valid as an origin point
                // If one cell passes as an origin then add shape to list
                if (CheckConfigs(cell.Key, shape, blueprint))
                    validCells.Add(cell.Key);
            }

            // If at least one origin was found then shape also passes
            if (validCells.Count > 0)
            {
                return validCells;
            }

            return null;
        }

        public List<Vector3Int> CheckForValidCells(Blueprint blueprint, ShapeData shape)
        {
            List<Vector3Int> validCells = new();

            foreach (var cell in shape.Cells)
            {
                // Skip cells that are not marked as blueprint cells, since they cannot be origins
                if (cell.Value == CellState.Blueprint)
                {
                    // DEPRICATED: We don't need to do this here
                    // Check if the cell is valid as an origin point
                    // If one cell passes as an origin then add shape to list
                    // if (CheckConfigs(cell.Key, shape, blueprint))
                    //     validCells.Add(cell.Key);

                    validCells.Add(cell.Key);
                }
            }

            return validCells;
        }

        #region Check Configs
        public bool CheckConfigs(Vector3Int localPosition, ShapeData shapeData, Blueprint blueprint)
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

            CellState[] BlueprintConfigs = new CellState[6];
            BlueprintConfigs[0] = CheckSide(blueprint, Vector3Int.right);
            BlueprintConfigs[1] = CheckSide(blueprint, Vector3Int.left);
            BlueprintConfigs[2] = CheckSide(blueprint, Vector3Int.forward);
            BlueprintConfigs[3] = CheckSide(blueprint, Vector3Int.back);
            BlueprintConfigs[4] = CheckSide(blueprint, Vector3Int.up);
            BlueprintConfigs[5] = CheckSide(blueprint, Vector3Int.down);

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
