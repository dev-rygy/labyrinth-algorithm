/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/01/2026
 * Last Modified:   09/08/2026 (Ryan)
 * Notes:           Blueprint Parser Unit Tests
*/
using AYellowpaper.SerializedCollections;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace RyansLibrary.Labyrinth
{
    public class BlueprintParserTests
    {
        private Dictionary<Vector3Int, Blueprint> _blueprintDictionary;
        BlueprintParser _parser;
        ShapeData _shape;
        List<ShapeData> _shapes;

        #region Test SetUp/TearDown
        [SetUp]
        public void SetUp()
        {
            _blueprintDictionary = new();
            _parser = new BlueprintParser(_blueprintDictionary);
            _shapes = new();

            _shape = ScriptableObject.CreateInstance<ShapeData>();
            _shape.Cells = new SerializedDictionary<Vector3Int, CellState>();

            ShapeData shape1x1x1 = ScriptableObject.CreateInstance<ShapeData>();
            shape1x1x1.Cells = new SerializedDictionary<Vector3Int, CellState>();
            shape1x1x1.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            _shapes.Add(shape1x1x1);

            ShapeData shape2x1x1 = ScriptableObject.CreateInstance<ShapeData>();
            shape2x1x1.Cells = new SerializedDictionary<Vector3Int, CellState>();
            shape2x1x1.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            shape2x1x1.Cells.Add(new Vector3Int(1, 0, 0), CellState.Blueprint);
            _shapes.Add(shape2x1x1);

            ShapeData shape1x2x1 = ScriptableObject.CreateInstance<ShapeData>();
            shape1x2x1.Cells = new SerializedDictionary<Vector3Int, CellState>();
            shape1x2x1.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            shape1x2x1.Cells.Add(new Vector3Int(0, 1, 0), CellState.Blueprint);
            _shapes.Add(shape1x2x1);

            ShapeData shape2x1x2 = ScriptableObject.CreateInstance<ShapeData>();
            shape2x1x2.Cells = new SerializedDictionary<Vector3Int, CellState>();
            shape2x1x2.Cells.Add(Vector3Int.zero, CellState.Blueprint);
            shape2x1x2.Cells.Add(new Vector3Int(1, 0, 0), CellState.Blueprint);
            shape2x1x2.Cells.Add(new Vector3Int(1, 0, 1), CellState.Blueprint);
            shape2x1x2.Cells.Add(new Vector3Int(0, 0, 1), CellState.Blueprint);
            _shapes.Add(shape2x1x2);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up any resources used in the test
            _shape = null;
            _shapes = null;
        }
        #endregion

        #region Baseline Parser Tests
        [Test]
        public void TestParserOnebyOneBlueprint()
        {
            // Arrage
            Vector3Int bpPosition = RandomVector();
            List<ShapeCandidate> validShapes;
            Blueprint b1 = new Blueprint(bpPosition);
            _blueprintDictionary.Add(b1.Position, b1);

            // Act
            validShapes = _parser.CheckValidShapes(b1, _shapes);

            // Assert
            Assert.AreEqual(1, validShapes.Count);
            Assert.AreEqual(validShapes[0].Shape, _shapes[0]);      // Candidate of 1x1x1
        }

        [Test]
        public void TestParserNoPossibleShapes()
        {
            // Arrage
            Vector3Int bpPosition = RandomVector();
            List<ShapeCandidate> validShapes;
            Blueprint b1 = new Blueprint(bpPosition);
            _blueprintDictionary.Add(b1.Position, b1);

            _shapes = new();

            // Act
            validShapes = _parser.CheckValidShapes(b1, _shapes);

            // Assert
            LogAssert.Expect(LogType.Error, "Parsing Failed - No possible shapes to parse.");
        }

        [Test]
        public void TestParserPlusBlueprints()
        {
            // Arrage
            Vector3Int bpPosition = RandomVector();
            List<ShapeCandidate> validShapes;
            Blueprint b1 = new Blueprint(bpPosition);
            _blueprintDictionary.Add(b1.Position, b1);
            Blueprint b2 = new Blueprint(bpPosition + Vector3Int.left);
            _blueprintDictionary.Add(b2.Position, b2);
            Blueprint b3 = new Blueprint(bpPosition + Vector3Int.right);
            _blueprintDictionary.Add(b3.Position, b3);
            Blueprint b4 = new Blueprint(bpPosition + Vector3Int.forward);
            _blueprintDictionary.Add(b4.Position, b4);
            Blueprint b5 = new Blueprint(bpPosition + Vector3Int.back);
            _blueprintDictionary.Add(b5.Position, b5);

            // Act
            validShapes = _parser.CheckValidShapes(b1, _shapes);

            // Assert
            Assert.AreEqual(3, validShapes.Count);
            // Candidate c2 accepted last
            ShapeCandidate c2 = validShapes[2];
            Assert.AreEqual(c2.Shape, _shapes[1]);          // c2 = Candidate of 2x1x1
            Assert.AreEqual(c2.Anchor, Vector3Int.zero);      // c2 = Base cell of (0, 0, 0)
            // Candidate c3 accepted second
            ShapeCandidate c3 = validShapes[1];
            Assert.AreEqual(c3.Shape, _shapes[1]);          // c3 = Candidate of 2x1x1
            Assert.AreEqual(c3.Anchor, Vector3Int.right);     // c3 = Base cell of (1, 0, 0)
            // Candidate c1 accepted first
            ShapeCandidate c1 = validShapes[0];
            Assert.AreEqual(c1.Shape, _shapes[0]);          // c1 = Candidate of 1x1x1
            Assert.AreEqual(c1.Anchor, Vector3Int.zero);      // c1 = Base cell of (0, 0, 0)
        }
        #endregion

        #region Parser Shape Fitting Tests
        /// <summary>
        /// Blob is a straight 1x3 corridor along X with the base blueprint in the middle.
        /// The 1x1x1 fills instantly at the middle cell. The 2x1x1 fills twice: once on the
        /// left branch (anchor (1,0,0), covering left+middle) and once on the right branch
        /// (anchor (0,0,0), covering middle+right). The 1x2x1 and the 2x1x2 are both killed on
        /// the very first config check because they demand a blueprint above / in front of the
        /// middle cell and the corridor is flat and one deep.
        /// </summary>
        [Test]
        public void TestParserLineOfThreeAlongX()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint middle = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.left);
            AddBlueprint(o + Vector3Int.right);

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(middle, _shapes);

            // Assert
            Assert.AreEqual(3, validShapes.Count);

            ShapeCandidate c2 = validShapes[2];
            Assert.AreEqual(_shapes[1], c2.Shape);              // c2 = Candidate of 2x1x1 covering middle + right
            Assert.AreEqual(Vector3Int.zero, c2.Anchor);

            ShapeCandidate c3 = validShapes[1];
            Assert.AreEqual(_shapes[1], c3.Shape);              // c3 = Candidate of 2x1x1 covering left + middle
            Assert.AreEqual(Vector3Int.right, c3.Anchor);

            ShapeCandidate c1 = validShapes[0];
            Assert.AreEqual(_shapes[0], c1.Shape);              // c1 = Candidate of 1x1x1 on the middle cell
            Assert.AreEqual(Vector3Int.zero, c1.Anchor);
        }

        /// <summary>
        /// Blob is exactly the 2x1x2 footprint. This is the only test in the suite that drives a
        /// four cell shape all the way to CheckFilled, so it exercises the full depth first walk:
        /// (0,0,0) -> right -> forward -> left, accumulating one PassedCells per hop. Confirms that
        /// the branch local candidate clones carry their PassedCells count down the recursion.
        /// </summary>
        [Test]
        public void TestParserSquareBlobFillsBigShape()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.right);
            AddBlueprint(o + Vector3Int.forward);
            AddBlueprint(o + Vector3Int.right + Vector3Int.forward);

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(start, _shapes);

            // Assert
            Assert.AreEqual(3, validShapes.Count);

            ShapeCandidate c6 = validShapes[2];
            Assert.AreEqual(_shapes[3], c6.Shape);             // c6 = Candidate of 2x1x2, the perfect fit
            Assert.AreEqual(Vector3Int.zero, c6.Anchor);

            ShapeCandidate mid = validShapes[1];
            Assert.AreEqual(_shapes[1], mid.Shape);             // c2 = Candidate of 2x1x1 across the near edge
            Assert.AreEqual(Vector3Int.zero, mid.Anchor);

            ShapeCandidate bottom = validShapes[0];
            Assert.AreEqual(_shapes[0], bottom.Shape);          // c1 = Candidate of 1x1x1
            Assert.AreEqual(Vector3Int.zero, bottom.Anchor);
        }

        /// <summary>
        /// Two blueprints stacked on Y. This is the only parser level test that walks the up/down
        /// peek, so it guards against the vertical axis being dropped from ParseBlueprints' peek
        /// chain. Only the 1x1x1 and the 1x2x1 can fit a column.
        /// </summary>
        [Test]
        public void TestParserVerticalPairFillsTallShape()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint bottomBp = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.up);

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(bottomBp, _shapes);

            // Assert
            Assert.AreEqual(2, validShapes.Count);

            ShapeCandidate c8 = validShapes[1];
            Assert.AreEqual(_shapes[2], c8.Shape);             // c8 = Candidate of 1x2x1 spanning both floors
            Assert.AreEqual(Vector3Int.zero, c8.Anchor);

            ShapeCandidate c1 = validShapes[0];
            Assert.AreEqual(_shapes[0], c1.Shape);          // c1 = Candidate of 1x1x1
            Assert.AreEqual(Vector3Int.zero, c1.Anchor);
        }

        /// <summary>
        /// Two blueprints stacked on Y. This is the only parser level test that walks the up/down
        /// peek, so it guards against the vertical axis being dropped from ParseBlueprints' peek
        /// chain. Only the 1x1x1 and the 1x2x1 can fit a column.
        /// </summary>
        [Test]
        public void TestParserVerticalPairFillsTallShapeWithBaseAtTop()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint topBp = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.down);

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(topBp, _shapes);

            // Assert
            Assert.AreEqual(2, validShapes.Count);

            ShapeCandidate c9 = validShapes[1];
            Assert.AreEqual(_shapes[2], c9.Shape);             // c8 = Candidate of 1x2x1 spanning both floors
            Assert.AreEqual(Vector3Int.up, c9.Anchor);

            ShapeCandidate c1 = validShapes[0];
            Assert.AreEqual(_shapes[0], c1.Shape);          // c1 = Candidate of 1x1x1
            Assert.AreEqual(Vector3Int.zero, c1.Anchor);
        }

        /// <summary>
        /// The 2x1x2 footprint sits inside a larger blob (a square with a tail hanging off its
        /// right edge). The tail cell is DontCare as far as the shape is concerned, so the square
        /// still fills. Also proves branch isolation: the walk visits the tail BEFORE it finishes
        /// the square, the tail kills the 2x1x2 candidate in that branch's clone list, and the
        /// parent's copy survives to complete the square on the forward branch.
        /// </summary>
        [Test]
        public void TestParserMatchesSubRegionOfLargerBlob()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.right);
            AddBlueprint(o + Vector3Int.forward);
            AddBlueprint(o + Vector3Int.right + Vector3Int.forward);
            AddBlueprint(o + Vector3Int.right * 2);             // tail, not part of any shape

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(start, _shapes);

            // Assert
            Assert.AreEqual(3, validShapes.Count);
            Assert.AreEqual(_shapes[3], validShapes[2].Shape);      // c6 = Candidate of 2x1x2 still fills
            Assert.AreEqual(_shapes[1], validShapes[1].Shape);      // c2 = Candidate of 2x1x1
            Assert.AreEqual(_shapes[0], validShapes[0].Shape);      // c1 = Candidate of 1x1x1
        }

        /// <summary>
        /// The blob is an L (three cells) and the only shape offered is the 2x1x2. Nothing can ever
        /// reach CheckFilled, so the parser must return an EMPTY stack rather than null and must not
        /// log an error - candidates did exist, they just all died during the walk.
        /// </summary>
        [Test]
        public void TestParserShapeLargerThanBlobYieldsNoCandidates()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.right);
            AddBlueprint(o + Vector3Int.forward);

            List<ShapeData> only2x1x2Shape = new List<ShapeData> { _shapes[3] };

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(start, only2x1x2Shape);

            // Assert
            Assert.IsNotNull(validShapes);
            Assert.AreEqual(0, validShapes.Count);
        }

        /// <summary>
        /// Same blob and same shape set, parsed at ten different random world origins. The parser
        /// works entirely in blueprint-local space (currentBlueprint.Position - _baseBlueprint.Position),
        /// so the result must be identical no matter where the blob lives on the grid. Catches any
        /// accidental reliance on absolute coordinates or on values near zero.
        /// </summary>
        [Test]
        public void TestParserIsIndependentOfWorldPosition()
        {
            for (int i = 0; i < 10; i++)
            {
                // Arrange - a fresh dictionary and parser per iteration
                Dictionary<Vector3Int, Blueprint> dictionary = new();
                BlueprintParser parser = new BlueprintParser(dictionary);

                Vector3Int o = RandomVector(100000);
                Blueprint center = new Blueprint(o);
                dictionary.Add(center.Position, center);
                foreach (var dir in new[] { Vector3Int.left, Vector3Int.right, Vector3Int.forward, Vector3Int.back })
                {
                    Blueprint bp = new Blueprint(o + dir);
                    dictionary.Add(bp.Position, bp);
                }

                // Act
                List<ShapeCandidate> validShapes = parser.CheckValidShapes(center, _shapes);

                // Assert
                Assert.AreEqual(3, validShapes.Count, $"Plus blob at {o} produced a different result.");
                Assert.AreEqual(_shapes[1], validShapes[2].Shape);
                Assert.AreEqual(_shapes[1], validShapes[1].Shape);
                Assert.AreEqual(_shapes[0], validShapes[0].Shape);
            }
        }
        #endregion

        #region Parser Traversal / Availability Tests
        /// <summary>
        /// A second blueprint exists five cells away with no connecting blueprints. The walk is
        /// driven purely by adjacency peeks, so the far blueprint must be unreachable and must not
        /// contribute to any candidate. Only the 1x1x1 can fill on an isolated cell.
        /// </summary>
        [Test]
        public void TestParserIgnoresDisconnectedBlueprints()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.right * 5);             // separate blob, not adjacent

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(start, _shapes);

            // Assert
            Assert.AreEqual(1, validShapes.Count);
            Assert.AreEqual(_shapes[0], validShapes[0].Shape);
        }

        /// <summary>
        /// The neighbour to the right exists in the dictionary but is already claimed
        /// (Available == false). ParserPeek refuses to walk into it, so the 2x1x1 gets its first
        /// PassedCells at the base cell and then starves - only the 1x1x1 survives.
        ///
        /// NOTE the asymmetry this test pins down: CheckSide(Blueprint, offset) looks the neighbour
        /// up in _blueprintDictionary WITHOUT consulting Available, so an unavailable blueprint still
        /// counts as CellState.Blueprint for config matching while being invisible to traversal. If
        /// claimed cells should read as NoBlueprint instead, this expectation changes.
        /// </summary>
        [Test]
        public void TestParserStopsAtUnavailableNeighbor()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.right, available: false);

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(start, _shapes);

            // Assert
            Assert.AreEqual(1, validShapes.Count);
            Assert.AreEqual(_shapes[0], validShapes[0].Shape);
        }

        /// <summary>
        /// Handing the parser an already claimed base blueprint. Candidate creation happens before
        /// the availability guard, so we still get past CheckForValidCells; ParseBlueprints then
        /// bails with an error. The contract to lock in: an EMPTY stack comes back, not null, so
        /// callers that do validShapes.Count don't null-ref.
        /// </summary>
        [Test]
        public void TestParserRejectsUnavailableStartBlueprint()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o, false);

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(start, _shapes);

            // Assert
            LogAssert.Expect(LogType.Error, "Parsing Failed - Base blueprint is not available to parse");
            Assert.IsNull(validShapes);
        }
        #endregion

        #region Parser Degenerate Input Tests
        /// <summary>
        /// Every shape offered consists solely of NoBlueprint cells, so CheckForValidCells returns
        /// nothing for all of them and the candidate list comes back empty. Expect the
        /// "no viable origins" error and a null return - distinct from the empty-stack return above.
        /// </summary>
        [Test]
        public void TestParserNoViableOriginsReturnsNull()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o);

            ShapeData negativeSpaceOnly = MakeShapeStates(
                (Vector3Int.zero, CellState.NoBlueprint),
                (Vector3Int.right, CellState.NoBlueprint));

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(start, new List<ShapeData> { negativeSpaceOnly });

            // Assert
            LogAssert.Expect(LogType.Error, new Regex("No viable origins found"));
            Assert.IsNull(validShapes);
        }

        /// <summary>
        /// A ShapeData with an empty Cells dictionary. This is worth guarding because CellCount is 0,
        /// so if such a shape ever produced a candidate it would satisfy CheckFilled
        /// (0 >= 0) on the very first blueprint and be accepted as a match for anything.
        /// CheckForValidCells saves us by never yielding a cell for it - assert that stays true.
        /// </summary>
        [Test]
        public void TestParserEmptyShapeProducesNoCandidates()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o);

            ShapeData emptyShape = MakeShape();     // zero cells

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(start, new List<ShapeData> { emptyShape });

            // Assert
            LogAssert.Expect(LogType.Error, new Regex("No viable origins found"));
            Assert.IsNull(validShapes);
        }

        /// <summary>
        /// Passing a null shape list. Documents that CheckValidShapes has no null guard - it
        /// dereferences possibleShapes.Count before anything else. If a guard is added later this
        /// test should be flipped to assert the null/error return instead.
        /// </summary>
        [Test]
        public void TestParserThrowsOnNullShapeList()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o);

            // Act / Assert
            _parser.CheckValidShapes(start, null);

            // Assert
            LogAssert.Expect(LogType.Error, "Parsing Failed - No possible shapes to parse.");
        }
        #endregion

        #region Parser Robustness / Known Limitation Tests
        /// <summary>
        /// ROBUSTNESS - shape retirement. A 1x4 corridor with only the 2x1x1 shape available. The
        /// shape fills across the first two cells, and RemoveShapeFromCandidateList then strips that
        /// shape out of the branch's candidate list. The list is now empty, so the recursion into
        /// cell 3 hits the base case immediately and cells 3 and 4 are never even visited.
        ///
        /// Result: exactly ONE candidate, despite the corridor having two clean 2x1x1 placements.
        /// This is fine if the caller re-runs the parser after claiming a room, but it means a single
        /// pass never enumerates all placements of a shape along one branch.
        /// </summary>
        [Test]
        public void TestParserRetiresShapeAfterFirstFillOnSameBranch()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.right);
            AddBlueprint(o + Vector3Int.right * 2);
            AddBlueprint(o + Vector3Int.right * 3);

            List<ShapeData> onlyLong = new List<ShapeData> { _shapes[1] };      // 2x1x1

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(start, onlyLong);

            // Assert
            Assert.AreEqual(1, validShapes.Count);
            ShapeCandidate only = validShapes[0];
            Assert.AreEqual(_shapes[1], only.Shape);
            Assert.AreEqual(Vector3Int.zero, only.Anchor);        // covers cells 1 and 2 only
        }

        /// <summary>
        /// ROBUSTNESS - the same limitation on the vertical axis. A three tall column with only the
        /// 1x2x1 available yields one candidate (floors 0-1); the placement across floors 1-2 is
        /// never discovered because the shape is retired and the walk stops.
        /// </summary>
        [Test]
        public void TestParserRetiresShapeAfterFirstFillOnColumn()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.up);
            AddBlueprint(o + Vector3Int.up * 2);

            List<ShapeData> onlyTall = new List<ShapeData> { _shapes[2] };      // 1x2x1

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(start, onlyTall);

            // Assert
            Assert.AreEqual(1, validShapes.Count);
            Assert.AreEqual(Vector3Int.zero, validShapes[0].Anchor);
        }

        /// <summary>
        /// ROBUSTNESS - overlapping results. Retirement is per branch, not global, so the SAME shape
        /// can be accepted once per sibling branch. On a 1x3 corridor parsed from the middle, the
        /// left branch accepts 2x1x1 anchored at (1,0,0) and the right branch accepts 2x1x1 anchored
        /// at (0,0,0) - two placements that OVERLAP on the middle cell.
        ///
        /// The parser returns both; whoever consumes the stack has to resolve the overlap. Worth
        /// asserting explicitly so nobody assumes the returned candidates are mutually compatible.
        /// </summary>
        [Test]
        public void TestParserReturnsOverlappingPlacementsAcrossBranches()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint middle = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.left);
            AddBlueprint(o + Vector3Int.right);

            List<ShapeData> onlyLong = new List<ShapeData> { _shapes[1] };      // 2x1x1

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(middle, onlyLong);

            // Assert
            Assert.AreEqual(2, validShapes.Count);

            ShapeCandidate fromRightBranch = validShapes[1];
            Assert.AreEqual(_shapes[1], fromRightBranch.Shape);
            Assert.AreEqual(Vector3Int.zero, fromRightBranch.Anchor);     // covers middle + right

            ShapeCandidate fromLeftBranch = validShapes[0];
            Assert.AreEqual(_shapes[1], fromLeftBranch.Shape);
            Assert.AreEqual(Vector3Int.right, fromLeftBranch.Anchor);     // covers left + middle
        }

        /// <summary>
        /// ROBUSTNESS - start position dependence, the passing half of the pair.
        /// Blob is a T: a straight run of three along X plus one cell in front of the junction.
        /// Shape is a 1x3 bar. Parsing from the END of the run, the walk is a single unbroken path
        /// (end -> junction -> far end), so PassedCells reaches 3 and the bar fills.
        /// </summary>
        [Test]
        public void TestParserFillsBarWhenStartingAtItsEnd()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint end = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.right);                                 // junction
            AddBlueprint(o + Vector3Int.right * 2);
            AddBlueprint(o + Vector3Int.right + Vector3Int.forward);            // T stem

            ShapeData bar = MakeShape(Vector3Int.zero, new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0));

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(end, new List<ShapeData> { bar });

            // Assert
            Assert.AreEqual(1, validShapes.Count);
            ShapeCandidate only = validShapes[0];
            Assert.AreEqual(bar, only.Shape);
            Assert.AreEqual(Vector3Int.zero, only.Anchor);
        }

        /// <summary>
        /// ROBUSTNESS - start position dependence, the FAILING half of the pair. Identical blob and
        /// identical shape as the test above; the only difference is that parsing starts at the
        /// MIDDLE of the bar.
        ///
        /// PassedCells only accumulates along a single root-to-leaf path. From the middle, the left
        /// and right cells are siblings, so the surviving candidate reaches 2 of its 3 cells on the
        /// left branch and (independently) 2 of 3 on the right branch, and never reaches 3 on either.
        /// The bar is a perfect fit for three of the four blueprints, yet ZERO candidates come back.
        ///
        /// This is the sharpest robustness finding in the suite: whether a shape is found depends on
        /// which blueprint you hand the parser. A fix would need to merge PassedCells across sibling
        /// branches (or track covered cells as a set rather than a counter).
        /// </summary>
        [Test]
        public void TestParserMissesBarWhenStartingAtItsMiddle()
        {
            // Arrange
            Vector3Int o = RandomVector();
            AddBlueprint(o);
            Blueprint junction = AddBlueprint(o + Vector3Int.right);
            AddBlueprint(o + Vector3Int.right * 2);
            AddBlueprint(o + Vector3Int.right + Vector3Int.forward);            // T stem

            ShapeData bar = MakeShape(Vector3Int.zero, new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0));

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(junction, new List<ShapeData> { bar });

            // Assert
            Assert.IsNotNull(validShapes);
            Assert.AreEqual(1, validShapes.Count);      // the bar fits perfectly, but is never found
        }

        /// <summary>
        /// ROBUSTNESS - the same start position dependence on a shape that is not a straight line,
        /// passing half. The blob IS the L-tromino, nothing more:
        ///
        ///     armB [1,0,1]
        ///            |
        ///     armA-corner
        ///   [0,0,0] [1,0,0]
        ///
        /// The footprint's adjacency graph is armA - corner - armB, a three node PATH whose middle
        /// vertex is the L's corner. Parsed from an arm, anchor (0,0,0) walks armA -> corner -> armB
        /// as one unbroken DFS path, PassedCells reaches 3, and the L fills.
        ///
        /// The other two anchors die on the very first config check: anchor (1,0,0) needs a blueprint
        /// to armA's left and anchor (1,0,1) needs one behind it, and the blob has neither.
        /// </summary>
        [Test]
        public void TestParserFillsLTrominoFromArmAnchor()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint armA = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.right);                                 // corner
            AddBlueprint(o + Vector3Int.right + Vector3Int.forward);            // armB

            ShapeData lTromino = MakeShape(
                Vector3Int.zero,
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, 0, 1)
                );

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(armA, new List<ShapeData> { lTromino });

            // Assert
            Assert.AreEqual(1, validShapes.Count);
            ShapeCandidate only = validShapes[0];
            Assert.AreEqual(lTromino, only.Shape);
            Assert.AreEqual(Vector3Int.zero, only.Anchor);        // anchored on the arm we started from
        }

        /// <summary>
        /// ROBUSTNESS - the failing half. Identical blob and identical shape as the test above; the
        /// only difference is that parsing starts at the L's CORNER instead of an arm.
        ///
        /// Anchor (1,0,0) is the placement that lays the L exactly over the blob, and it is the only
        /// one that survives the first config check. But the corner is the middle vertex of the
        /// footprint's path graph, so from there the two arms are SIBLING branches, not ancestor and
        /// descendant. PassedCells accumulates along a single root-to-leaf path only: the candidate
        /// reaches 2 of 3 down the left branch and, from a separate clone, 2 of 3 down the forward
        /// branch. Neither ever reaches 3.
        ///
        /// A three node path graph has no Hamiltonian path rooted at its middle vertex, so NO DFS
        /// ordering could rescue this - it is not a peek-order bug. Generalised: an L-shaped room can
        /// never be placed such that the blueprint you started parsing from is its corner. The same
        /// argument makes a plus-shaped footprint unfillable from every anchor, since its centre is a
        /// degree-4 cut vertex and it has no Hamiltonian path at all.
        /// </summary>
        [Test]
        public void TestParserMissesLTrominoFromCornerAnchor()
        {
            // Arrange
            Vector3Int o = RandomVector();
            AddBlueprint(o);                                                    // armA
            Blueprint corner = AddBlueprint(o + Vector3Int.right);
            AddBlueprint(o + Vector3Int.right + Vector3Int.forward);            // armB

            ShapeData lTromino = MakeShape(
                Vector3Int.zero,
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, 0, 1));

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(corner, new List<ShapeData> { lTromino });

            // Assert
            Assert.IsNotNull(validShapes);
            Assert.AreEqual(1, validShapes.Count);      // the L is the blob exactly, and is still never found
        }

        /// <summary>
        /// ROBUSTNESS - one shape, several valid placements, all of them overlapping the base.
        /// A 1x5 corridor parsed from its middle cell, matched against a 1x3 bar:
        ///
        ///     c0 - c1 - [c2] - c3 - c4        [c2] is the base blueprint
        ///
        /// Three placements cover c2 and every one of them is a legitimate fit, so three candidates
        /// should come back - anchor (2,0,0) over c0c1c2, anchor (1,0,0) over c1c2c3, and anchor
        /// (0,0,0) over c2c3c4. They overlap each other; resolving that is the caller's job.
        ///
        /// This is the test that guards shape retirement. All three candidates share one ShapeData
        /// asset, so when the (1,0,0) placement completes at c3, RemoveShapeFromCandidateList runs
        /// RemoveAll(c =&gt; c.Shape == candidate.Shape) and deletes the (0,0,0) placement along with
        /// it - while that one is sitting at 2 of 3 cells, one step from completing. Emptying the
        /// list then trips the 'candidates.Count &lt;= 0' guard, so c4 is never visited either.
        ///
        /// Retirement only ever looked safe because the deep clones meant it was deleting copies.
        /// Sharing the candidates makes it delete live progress instead.
        /// </summary>
        [Test]
        public void TestParserFindsEveryBarPlacementCoveringTheBase()
        {
            // Arrange
            Vector3Int o = RandomVector();
            AddBlueprint(o);                                            // b0
            AddBlueprint(o + Vector3Int.right);                         // b1
            Blueprint middle = AddBlueprint(o + Vector3Int.right * 2);  // b2, the base blueprint
            AddBlueprint(o + Vector3Int.right * 3);                     // b3
            AddBlueprint(o + Vector3Int.right * 4);                     // b4

            // Bar shape 3x1x1
            ShapeData bar = MakeShape(Vector3Int.zero, new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0));

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(middle, new List<ShapeData> { bar });

            // Assert
            Assert.AreEqual(3, validShapes.Count);

            // Membership rather than list order - what matters is which placements were found
            List<Vector3Int> anchors = new List<Vector3Int>();
            foreach (var candidate in validShapes)
            {
                Assert.AreEqual(bar, candidate.Shape);
                anchors.Add(candidate.Anchor);
            }

            Assert.Contains(new Vector3Int(2, 0, 0), anchors);      // covers c0 c1 c2
            Assert.Contains(new Vector3Int(1, 0, 0), anchors);      // covers c1 c2 c3
            Assert.Contains(Vector3Int.zero, anchors);              // covers c2 c3 c4
        }

        /// <summary>
        /// ROBUSTNESS - a candidate must survive the walk stepping off its own footprint.
        /// Blob is a flat 3 wide by 2 deep pocket, parsed from the bottom middle cell:
        ///
        ///     D(0,0,1)   E(1,0,1)   F(2,0,1)
        ///     A(0,0,0)  [B(1,0,0)]  C(2,0,0)        [B] is the base blueprint
        ///
        /// The 2x1x2 square fits over B two different ways - the left half {A,B,D,E} at anchor
        /// (1,0,0), and the right half {B,C,E,F} at anchor (0,0,0). Both are valid cell for cell,
        /// so two candidates should come back.
        ///
        /// Only one does. k_directions peeks left first, so the walk runs B -> A -> D -> E -> F.
        /// At A the right-half candidate maps to shape cell (-1,0,0), which the shape has no cell
        /// for, so CheckConfigs returns false and the candidate is dropped from that entire branch.
        /// The branch then goes on to consume E and F - two of that candidate's own four cells -
        /// and marks them in the visited dictionary, which is global. When the walk returns to B
        /// and heads right, the candidate picks up C for 2 of 4 and then finds E and F already
        /// visited. It starves one cell short of a placement that is perfectly legal.
        ///
        /// The cause is that CheckConfigs conflates two different answers: "the shape has no cell
        /// here" and "the shape has a cell here and the layout contradicts it". The first is not a
        /// failure - the blueprint simply belongs to some other room - and should skip the
        /// candidate rather than eliminate it. Only the second should drop it from the branch.
        ///
        /// Note this bites ordinary contiguous shapes, not just footprints with gaps: it triggers
        /// whenever the walk reaches part of a footprint by a route that leaves that footprint.
        /// </summary>
        [Test]
        public void TestParserSurvivesWalkLeavingItsFootprint()
        {
            // Arrange
            Vector3Int o = RandomVector();
            AddBlueprint(o);                                                    // A
            Blueprint middle = AddBlueprint(o + Vector3Int.right);               // B, the base blueprint
            AddBlueprint(o + Vector3Int.right * 2);                             // C
            AddBlueprint(o + Vector3Int.forward);                               // D
            AddBlueprint(o + Vector3Int.right + Vector3Int.forward);            // E
            AddBlueprint(o + Vector3Int.right * 2 + Vector3Int.forward);        // F

            ShapeData square = _shapes[3];      // 2x1x2

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(middle, new List<ShapeData> { square });

            // Assert
            Assert.AreEqual(2, validShapes.Count);

            // Membership rather than list order - what matters is which placements were found
            List<Vector3Int> anchors = new List<Vector3Int>();
            foreach (var candidate in validShapes)
            {
                Assert.AreEqual(square, candidate.Shape);
                anchors.Add(candidate.Anchor);
            }

            Assert.Contains(new Vector3Int(1, 0, 0), anchors);      // left half  A B D E
            Assert.Contains(Vector3Int.zero, anchors);              // right half B C E F
        }

        /* TODO: Handle case with disjointed shape
        /// <summary>
        /// ROBUSTNESS - CellCount counts negative space. The shape is "one room with explicitly
        /// nothing to its right": one Blueprint cell plus one NoBlueprint cell, so CellCount == 2.
        /// The blob is a single isolated blueprint, which matches the shape's intent exactly.
        ///
        /// CheckConfigs passes at the anchor, but PassedCells can only ever reach 1 because the walk
        /// visits blueprints and there is only one blueprint in the footprint. CheckFilled compares
        /// against CellCount (2) and never fires, so nothing is returned.
        ///
        /// Any shape that declares negative space is therefore unfillable. CheckFilled probably wants
        /// to compare against the count of Blueprint-state cells, not the total cell count.
        /// </summary>
        [Test]
        public void TestParserShapeWithNegativeSpaceNeverFills()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o);

            ShapeData isolatedSingle = MakeShapeStates(
                (Vector3Int.zero, CellState.Blueprint),
                (Vector3Int.right, CellState.NoBlueprint));

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(start, new List<ShapeData> { isolatedSingle });

            // Assert
            Assert.IsNotNull(validShapes);
            Assert.AreEqual(0, validShapes.Count);      // shape describes the blob exactly, still no match
        }
        */

        /// <summary>
        /// ROBUSTNESS - disjoint shapes. A shape whose two Blueprint cells are not adjacent
        /// ((0,0,0) and (2,0,0)) laid over a 1x3 corridor that does contain blueprints at both.
        /// The walk must step through the middle blueprint, which maps to a shape coordinate that
        /// has no cell, so CheckConfigs returns false and the candidate is eliminated.
        ///
        /// Conclusion to lock in: every blueprint the walk touches inside a candidate's footprint
        /// span must be described by that shape. Disconnected footprints cannot be expressed.
        /// </summary>
        [Test]
        public void TestParserCannotFillDisjointShape()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint start = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.right);
            AddBlueprint(o + Vector3Int.right * 2);

            ShapeData disjoint = MakeShape(Vector3Int.zero, new Vector3Int(2, 0, 0));

            // Act
            List<ShapeCandidate> validShapes = _parser.CheckValidShapes(start, new List<ShapeData> { disjoint });

            // Assert
            Assert.IsNotNull(validShapes);
            Assert.AreEqual(0, validShapes.Count);
        }
        #endregion

        #region Config Check Tests
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
            Assert.IsFalse(result);
        }
        #endregion

        #region Cell / Config Contract Tests
        /// <summary>
        /// CheckConfigs validates the six NEIGHBOURS of the anchor but never checks the anchor cell's
        /// own state - it only requires the cell to exist. Here the anchor is marked NoBlueprint yet a
        /// blueprint sits on it, and the check still returns true.
        ///
        /// Nothing exploits this today because CheckForValidCells only ever hands back Blueprint-state
        /// cells as anchors. It becomes a live bug the moment anything calls CheckConfigs with an
        /// arbitrary offset - which ParseBlueprints does, via candidate.Anchor + localPosition.
        /// </summary>
        [Test]
        public void TestConfigIgnoresAnchorCellState()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint b1 = AddBlueprint(o);

            ShapeData shape = MakeShapeStates((Vector3Int.zero, CellState.NoBlueprint));

            // Act
            bool result = _parser.CheckConfigs(Vector3Int.zero, shape, b1);

            // Assert
            Assert.IsTrue(result);      // a blueprint sits on a cell that demands no blueprint
        }

        /// <summary>
        /// Vertical coverage for the config check. The existing config tests only ever assert on the
        /// horizontal faces plus 'up' as DontCare; this drives both up and down as hard requirements
        /// to make sure indices 4 and 5 of the config arrays are compared and not silently skipped.
        /// </summary>
        [Test]
        public void TestConfigChecksBothVerticalFaces()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint b1 = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.up);

            ShapeData needsUpAndDown = MakeShapeStates(
                (Vector3Int.zero, CellState.Blueprint),
                (Vector3Int.up, CellState.Blueprint),
                (Vector3Int.down, CellState.Blueprint));    // nothing below, so this must fail

            ShapeData needsUpOnly = MakeShapeStates(
                (Vector3Int.zero, CellState.Blueprint),
                (Vector3Int.up, CellState.Blueprint),
                (Vector3Int.down, CellState.NoBlueprint));

            // Act / Assert
            Assert.IsFalse(_parser.CheckConfigs(Vector3Int.zero, needsUpAndDown, b1));
            Assert.IsTrue(_parser.CheckConfigs(Vector3Int.zero, needsUpOnly, b1));
        }
        #endregion

        #region Utility
        /// <summary>
        /// Creates a blueprint at the given grid position and registers it in the test dictionary.
        /// </summary>
        private Blueprint AddBlueprint(Vector3Int position, bool available = true)
        {
            Blueprint blueprint = new Blueprint(position);
            blueprint.Available = available;
            _blueprintDictionary.Add(blueprint.Position, blueprint);
            return blueprint;
        }

        /// <summary>
        /// Builds a ShapeData whose listed cells are all CellState.Blueprint.
        /// </summary>
        private ShapeData MakeShape(params Vector3Int[] blueprintCells)
        {
            ShapeData shape = ScriptableObject.CreateInstance<ShapeData>();
            shape.Cells = new SerializedDictionary<Vector3Int, CellState>();
            foreach (var cell in blueprintCells)
                shape.Cells.Add(cell, CellState.Blueprint);
            return shape;
        }

        /// <summary>
        /// Builds a ShapeData with an explicit CellState per cell, for shapes that declare negative
        /// space. Any position not listed is implicitly DontCare.
        /// </summary>
        private ShapeData MakeShapeStates(params (Vector3Int cell, CellState state)[] cells)
        {
            ShapeData shape = ScriptableObject.CreateInstance<ShapeData>();
            shape.Cells = new SerializedDictionary<Vector3Int, CellState>();
            foreach (var entry in cells)
                shape.Cells.Add(entry.cell, entry.state);
            return shape;
        }

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
