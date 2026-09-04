/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/01/2026
 * Last Modified:   09/01/2026 (Ryan)
 * Notes:           Blueprint Parser Unit Tests
*/
using NUnit.Framework;
using RyansLibrary.Labyrinth;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace RyansLibrary
{
    public class BlueprintParserTests
    {
        private Dictionary<Vector3Int, Blueprint> _blueprintDictionary;
        BlueprintParser _parser;
        ShapeData _shape;

        [SetUp]
        public void SetUp()
        {
            _blueprintDictionary = new Dictionary<Vector3Int, Blueprint>();
            _parser = new BlueprintParser(_blueprintDictionary);

            _shape = ScriptableObject.CreateInstance<ShapeData>();
            _shape.Cells = new AYellowpaper.SerializedCollections.SerializedDictionary<Vector3Int, CellState>();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any resources used in the test
            _shape = null;
        }

        #region Test Origins
        [Test]
        public void TestOriginPass()
        {
            // Arrange  (Set test data and conditions)
            Vector3Int bpPosition = RandomVector();
            List<Vector3Int> validOrigins = new List<Vector3Int>();
            Blueprint b1 = new Blueprint(bpPosition);
            _blueprintDictionary.Add(b1.Position, b1);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);

            // Act      (Execute the code you're testing)
            validOrigins = _parser.CheckForValidOrigins(b1, _shape);

            // Assert   (Make sure results are what you expect)
            Assert.AreEqual(1, validOrigins.Count);  // Only one valid origin
            Assert.AreEqual(Vector3Int.zero, validOrigins[0]);  // The only valid origin is at (0,0,0)

            // Cleanup  (Optional: Clean up any resources used in the test)
        }

        /// <summary>
        /// Shape has no valid origin due to the fact that the shape is (2, 1, 1) and only 
        /// one blueprint at origin exists. Therefore should return false and no valid origins.
        /// </summary>
        [Test]
        public void TestOriginFail()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            List<Vector3Int> validOrigins = new List<Vector3Int>();
            Blueprint b1 = new Blueprint(bpPosition);
            _blueprintDictionary.Add(b1.Position, b1);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.Blueprint);

            // Act
            validOrigins = _parser.CheckForValidOrigins(b1, _shape);

            // Assert
            Assert.IsNull(validOrigins);  // No valid origins

        }

        /// <summary>
        /// Shape should have only one valid origin due to the fact that the shape is (2, 1, 1)
        /// and blueprints are in valid configuration. Therefore should return true with an origin at (0,0,0)
        /// </summary>
        [Test]
        public void TestOriginPassOn2x1x1()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            List<Vector3Int> validOrigins = new List<Vector3Int>();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.Blueprint);

            // Act
            validOrigins = _parser.CheckForValidOrigins(b1, _shape);

            // Assert
            Assert.AreEqual(1, validOrigins.Count);  // One valid origin
            Assert.AreEqual(Vector3Int.zero, validOrigins[0]);  // The valid origin is at (0,0,0)

        }

        /// <summary>
        /// Shape should have only one valid origin due to the fact that the shape is (2, 1, 1)
        /// and blueprints are in valid configuration. Therefore should return true with an origin at (0,0,0)
        /// </summary>
        [Test]
        public void TestOriginPassOn2x1x1StateDifference()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            List<Vector3Int> validOrigins = new List<Vector3Int>();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.NoBlueprint);

            // Act
            // Even with NoBlueprint set on one of the shape cells the first cell should still pass the check
            // when b2 is the blueprint being checked.
            validOrigins = _parser.CheckForValidOrigins(b2, _shape);

            // Assert
            Assert.AreEqual(1, validOrigins.Count);  // One valid origin
            Assert.AreEqual(Vector3Int.zero, validOrigins[0]);  // The valid origin is at (0,0,0)

        }

        /// <summary>
        /// Shape should have only one valid origin due to the fact that the shape is (2, 1, 1)
        /// and blueprints are in valid configuration. Therefore should return true with an origin at (0,0,0)
        /// </summary>
        [Test]
        public void TestOriginFailOn2x1x1StateDifference()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            List<Vector3Int> validOrigins = new List<Vector3Int>();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.NoBlueprint);

            // Act
            // This shape requires there to not be a blueprint at (1, 0, 0) but b2 is present at that location,
            validOrigins = _parser.CheckForValidOrigins(b1, _shape);

            // Assert
            Assert.IsNull(validOrigins);  // No valid origins
        }

        /// <summary>
        /// Shape has no valid origins due to the fact that the shape is (2, 1, 1) and the two blueprints that do
        /// exist are not in a valid configuration to match the shape. Therefore should return false and no valid origins.
        /// </summary>
        [Test]
        public void TestOriginFailOn2x1x1BlueprintDifference()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            List<Vector3Int> validOrigins = new List<Vector3Int>();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right * 2);     // This blueprint leaves a space between the first
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.Blueprint);        // No Blueprint exists at this cell's location

            // Act
            validOrigins = _parser.CheckForValidOrigins(b1, _shape);

            // Assert
            Assert.IsNull(validOrigins);  // No valid origins

        }

        /// <summary>
        /// Shape should have two valid origins due to the fact that the shape is (2, 1, 2)
        /// and blueprints are in valid configurations for origins at (0,0,0) and (1,0,0).
        /// </summary>
        [Test]
        public void TestOriginPassOn2x1x2OneValidOrigin()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            List<Vector3Int> validOrigins = new List<Vector3Int>();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            Blueprint b3 = new Blueprint(bpPosition + Vector3Int.forward);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _blueprintDictionary.Add(b3.Position, b3);

            // 2x1x2 shape
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.forward, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right + Vector3Int.forward, CellState.Blueprint);

            // Act
            validOrigins = _parser.CheckForValidOrigins(b1, _shape);

            // Assert
            Assert.AreEqual(1, validOrigins.Count);  // Only 2 valid origins
            Assert.AreEqual(Vector3Int.zero, validOrigins[0]);  // A valid origin is at (0,0,0)
        }

        /// <summary>
        /// Shape should have two valid origins due to the fact that the shape is (2, 1, 2)
        /// and blueprints are in valid configurations for origins at (0,0,0) and (1,0,0).
        /// </summary>
        [Test]
        public void TestOriginPassOn2x1x2TwoValidOrigins()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            List<Vector3Int> validOrigins = new List<Vector3Int>();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            Blueprint b3 = new Blueprint(bpPosition + Vector3Int.forward);
            Blueprint b5 = new Blueprint(bpPosition + Vector3Int.left);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _blueprintDictionary.Add(b3.Position, b3);
            _blueprintDictionary.Add(b5.Position, b5);

            // 2x1x2 shape
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.forward, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right + Vector3Int.forward, CellState.Blueprint);

            // Act
            validOrigins = _parser.CheckForValidOrigins(b1, _shape);

            // Assert
            Assert.AreEqual(2, validOrigins.Count);  // Only 2 valid origins
            Assert.AreEqual(Vector3Int.zero, validOrigins[0]);  // A valid origin is at (0,0,0)
            Assert.AreEqual(Vector3Int.right, validOrigins[1]);  // A valid origin is at (1,0,0)

        }

        /// <summary>
        /// Shape should not have any origins due to the fact that the shape is (2, 1, 2)
        /// and blueprints are missing on the top right corner and direct left.
        /// </summary>
        [Test]
        public void TestOriginFailOn2x1x2()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            List<Vector3Int> validOrigins = new List<Vector3Int>();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            // Blueprint b3 = new Blueprint(bpPosition + Vector3Int.forward);
            // Blueprint b5 = new Blueprint(bpPosition + Vector3Int.left);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            // _blueprintDictionary.Add(b3.Position, b3);
            // _blueprintDictionary.Add(b5.Position, b5);

            // 2x1x2 shape
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.forward, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right + Vector3Int.forward, CellState.Blueprint);

            // Act
            validOrigins = _parser.CheckForValidOrigins(b1, _shape);

            // Assert
            Assert.IsNull(validOrigins);  // Only 2 valid origins
        }

        /// <summary>
        /// Shape should have only one valid origins due to the fact that the shape is a Plus
        /// and blueprints are only in a valid configuration for origins at (1,0,0).
        /// </summary>
        [Test]
        public void TestOriginPassOnPlusShape()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            List<Vector3Int> validOrigins = new List<Vector3Int>();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            Blueprint b3 = new Blueprint(bpPosition + Vector3Int.left);
            Blueprint b4 = new Blueprint(bpPosition + Vector3Int.forward);
            Blueprint b5 = new Blueprint(bpPosition + Vector3Int.down);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _blueprintDictionary.Add(b3.Position, b3);
            _blueprintDictionary.Add(b4.Position, b4);
            _blueprintDictionary.Add(b5.Position, b5);

            // Plus shape
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.left, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.forward, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.down, CellState.Blueprint);

            // Act
            // Starting at right blueprint only 
            validOrigins = _parser.CheckForValidOrigins(b2, _shape);

            // Assert
            Assert.AreEqual(1, validOrigins.Count);  // Only 2 valid origins
            Assert.AreEqual(Vector3Int.right, validOrigins[0]);  // Only valid origin is at (1,0,0)
        }

        /// <summary>
        /// Shape should have two valid origins due to the fact that the shape is a Plus
        /// and blueprints are in an H configuration. Therefore should return true with origins at (1,0,0) and (-1,0,0)
        /// </summary>
        [Test]
        public void TestOriginPassOnPlusShape2()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            List<Vector3Int> validOrigins = new List<Vector3Int>();

            // Blueprints in an H configuration
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            Blueprint b3 = new Blueprint(bpPosition + Vector3Int.left);
            Blueprint b4 = new Blueprint(bpPosition + Vector3Int.right + Vector3Int.forward);
            Blueprint b5 = new Blueprint(bpPosition + Vector3Int.right + Vector3Int.down);
            Blueprint b6 = new Blueprint(bpPosition + Vector3Int.left + Vector3Int.forward);
            Blueprint b7 = new Blueprint(bpPosition + Vector3Int.left + Vector3Int.down);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _blueprintDictionary.Add(b3.Position, b3);
            _blueprintDictionary.Add(b4.Position, b4);
            _blueprintDictionary.Add(b5.Position, b5);
            _blueprintDictionary.Add(b6.Position, b6);
            _blueprintDictionary.Add(b7.Position, b7);

            // Plus shape
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.left, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.forward, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.down, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right + Vector3Int.forward, CellState.NoBlueprint);
            _shape.Cells.Add(Vector3Int.right + Vector3Int.back, CellState.NoBlueprint);
            _shape.Cells.Add(Vector3Int.left + Vector3Int.forward, CellState.NoBlueprint);
            _shape.Cells.Add(Vector3Int.left + Vector3Int.back, CellState.NoBlueprint);

            // Act
            // Starting at right blueprint only 
            validOrigins = _parser.CheckForValidOrigins(b1, _shape);

            // Assert
            Assert.AreEqual(2, validOrigins.Count);  // Only 2 valid origins
            Assert.AreEqual(Vector3Int.right, validOrigins[0]);  // Valid origin is at (1,0,0)
            Assert.AreEqual(Vector3Int.left, validOrigins[1]);  // Valid origin is at (-1,0,0)
        }
        #endregion

        #region Test Config Check
        /// <summary>
        /// Takes one blueprint and a shape with one cell marked with the state 'Blueprint'.
        /// The test will pass since a blueprint is present at the origin and the shape is 
        /// verified to have a cell at the origin marked as 'Blueprint'.
        /// </summary>
        [Test]
        public void TestConfigPass()
        {
            Vector3Int bpPosition = RandomVector();

            // Arrange  (Set test data and conditions)
            Blueprint b1 = new Blueprint(bpPosition);
            _blueprintDictionary.Add(b1.Position, b1);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);

            // Act      (Execute the code you're testing)
            bool result = _parser.CheckConfigs(Vector3Int.zero, _shape, b1);

            // Assert   (Make sure results are what you expect)
            Assert.IsTrue(result);

            // Cleanup  (Optional: Clean up any resources used in the test)
        }

        [Test]
        public void TestConfigFailWithNoBlueprint()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);             // Has a right blueprint
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.NoBlueprint);  // Requires there to not be a blueprint to the right

            // Act
            bool result = _parser.CheckConfigs(Vector3Int.zero, _shape, b1);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TestConfigPassWithNoBlueprint()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            Blueprint b1 = new Blueprint(bpPosition);
            _blueprintDictionary.Add(b1.Position, b1);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.NoBlueprint);  // Requires there to not be a blueprint to the right

            // Act
            bool result = _parser.CheckConfigs(Vector3Int.zero, _shape, b1);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Any space in shape that does not have a cell is marked as 'DontCare' and will disregard the 
        /// presence of a blueprint in that position.
        /// </summary>
        [Test]
        public void TestConfigPassDontCare()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);     // disreguarded by check
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);

            // Act
            bool result = _parser.CheckConfigs(Vector3Int.zero, _shape, b1);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Any space in shape that does not have a cell is marked as 'DontCare' and will disregard the 
        /// presence of a blueprint in that position.
        /// </summary>
        [Test]
        public void TestConfigPassEachSide()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            Blueprint b1 = new Blueprint(bpPosition);      // origin blueprint
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            Blueprint b3 = new Blueprint(bpPosition + Vector3Int.left);
            Blueprint b4 = new Blueprint(bpPosition + Vector3Int.forward);
            Blueprint b5 = new Blueprint(bpPosition + Vector3Int.back);
            Blueprint b6 = new Blueprint(bpPosition + Vector3Int.up);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _blueprintDictionary.Add(b3.Position, b3);
            _blueprintDictionary.Add(b4.Position, b4);
            _blueprintDictionary.Add(b5.Position, b5);
            _blueprintDictionary.Add(b6.Position, b6);

            // origin, right, left = Blueprint, forward, back, up = DontCare
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);     // origin cell
            _shape.Cells.Add(Vector3Int.right, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.left, CellState.Blueprint);

            // Act
            bool result = _parser.CheckConfigs(Vector3Int.zero, _shape, b1);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Any space in shape that does not have a cell is marked as 'DontCare' and will disregard the 
        /// presence of a blueprint in that position.
        /// </summary>
        [Test]
        public void TestConfigFailEachSide()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            Blueprint b1 = new Blueprint(bpPosition);      // origin blueprint
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            Blueprint b3 = new Blueprint(bpPosition + Vector3Int.left);
            Blueprint b4 = new Blueprint(bpPosition + Vector3Int.forward);
            Blueprint b5 = new Blueprint(bpPosition + Vector3Int.back);
            Blueprint b6 = new Blueprint(bpPosition + Vector3Int.up);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _blueprintDictionary.Add(b3.Position, b3);
            _blueprintDictionary.Add(b4.Position, b4);
            _blueprintDictionary.Add(b5.Position, b5);
            _blueprintDictionary.Add(b6.Position, b6);

            // origin, right, left = Blueprint, forward, back, up = DontCare
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);     // origin cell
            _shape.Cells.Add(Vector3Int.right, CellState.NoBlueprint);
            _shape.Cells.Add(Vector3Int.left, CellState.NoBlueprint);

            // Act
            bool result = _parser.CheckConfigs(Vector3Int.zero, _shape, b1);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Any space in shape that does not have a cell is marked as 'DontCare' and will disregard the 
        /// presence of a blueprint in that position.
        /// </summary>
        [Test]
        public void TestConfigPassOriginShift()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.Blueprint);

            // Act
            bool result = _parser.CheckConfigs(Vector3Int.right, _shape, b2);

            // Assert
            Assert.IsTrue(result);
        }

        /// <summary>
        /// Any space in shape that does not have a cell is marked as 'DontCare' and will disregard the 
        /// presence of a blueprint in that position.
        /// </summary>
        [Test]
        public void TestConfigFailOriginShift()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shape.Cells.Add(Vector3Int.right, CellState.Blueprint);
            // Should fail because the shape requires a blueprint at (1,1,0) but there is none
            _shape.Cells.Add(Vector3Int.right + Vector3Int.up, CellState.Blueprint);

            // Act
            bool result = _parser.CheckConfigs(Vector3Int.right, _shape, b2);

            // Assert
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Any space in shape that does not have a cell is marked as 'DontCare' and will disregard the 
        /// presence of a blueprint in that position.
        /// </summary>
        [Test]
        public void TestConfigFailNoCellAtPos()
        {
            // Arrange
            Vector3Int bpPosition = RandomVector();
            Blueprint b1 = new Blueprint(bpPosition);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.right);
            _blueprintDictionary.Add(b1.Position, b1);
            _blueprintDictionary.Add(b2.Position, b2);
            _shape.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            // Missing a cell at (1,0,0) which is required for the shape to match the blueprint

            // Act
            bool result = _parser.CheckConfigs(Vector3Int.right, _shape, b2);

            // Assert
            LogAssert.Expect(LogType.Error, $"Attempted illegal check on {_shape} at relative position {Vector3Int.right}");
        }
        #endregion

        #region Utility
        private Vector3Int RandomVector(int range = 1000)
        {
            // Arrange
            int randomX = Random.Range(-range, range);
            int randomY = Random.Range(-range, range);
            int randomZ = Random.Range(-range, range);
            return new Vector3Int(randomX, randomY, randomZ);
        }
        #endregion
    }
}
