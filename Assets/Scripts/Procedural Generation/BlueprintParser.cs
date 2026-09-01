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
    public class BlueprintParser
    {
        private struct ShapeContext
        {
            public ShapeData Shape;
            public List<Vector3Int> Origins;
        }

        Dictionary<Vector3Int, Blueprint> _blueprintDictionary;

        public BlueprintParser(Dictionary<Vector3Int, Blueprint> blueprintDictionary)
        {
            _blueprintDictionary = blueprintDictionary;
        }

        /// <summary>
        /// Checks all cells in each shape in the list against the current blueprint.
        /// If a cell matches the configs of the blueprint, then the cell is valid as an origin
        /// and shape passes test.
        /// </summary>
        /// <param name="shapeList">List of viable shapes</param>
        /// <param name="blueprint">The current blueprint being parsed</param>
        private void CheckAllOrigins(List<ShapeData> shapeList, Blueprint blueprint)
        {
            List<ShapeContext> passedShapes = new List<ShapeContext>();

            foreach (var shape in shapeList)
            {
                if (CheckForValidOrigins(shape, blueprint, out List<Vector3Int> validOrigins))
                {
                    passedShapes.Add(new ShapeContext { Shape = shape, Origins = validOrigins });
                }
            }
        }

        public bool CheckForValidOrigins(ShapeData shape, Blueprint blueprint, out List<Vector3Int> validOrigins)
        {
            validOrigins = new();

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
                    validOrigins.Add(cell.Key);
            }

            // If at least one origin was found then shape also passes
            if (validOrigins.Count > 0)
            {
                return true;
            }

            return false;
        }

        #region Check Configs
        public bool CheckConfigs(Vector3Int posRelativeToOrigin, ShapeData shapeData, Blueprint blueprint)
        {
            // Shape does not contain a cell at position, so it's an illegal check
            if (!shapeData.Cells.ContainsKey(posRelativeToOrigin))
            {
                Debug.LogError($"Attempted illegal check on {shapeData} at relative position {posRelativeToOrigin}");
                return false;
            }

            CellState[] ShapeDataConfigs = new CellState[6];
            ShapeDataConfigs[0] = CheckSide(shapeData, posRelativeToOrigin, Vector3Int.right);
            ShapeDataConfigs[1] = CheckSide(shapeData, posRelativeToOrigin, Vector3Int.left);
            ShapeDataConfigs[2] = CheckSide(shapeData, posRelativeToOrigin, Vector3Int.forward);
            ShapeDataConfigs[3] = CheckSide(shapeData, posRelativeToOrigin, Vector3Int.back);
            ShapeDataConfigs[4] = CheckSide(shapeData, posRelativeToOrigin, Vector3Int.up);
            ShapeDataConfigs[5] = CheckSide(shapeData, posRelativeToOrigin, Vector3Int.down);

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
