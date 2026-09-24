/*
 * Created By:      Ryan Carpenter
 * Date Created:    09/23/2026
 * Last Modified:   09/23/2026 (Ryan)
 * Notes:           Room Generator Unit Tests
*/
using AYellowpaper.SerializedCollections;
using NUnit.Framework;
using RyansLibrary.Utilities;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace RyansLibrary.Labyrinth
{
    /// <summary>
    /// Tests are written against the behaviour RoomGenerator *should* have (fail safely: return false/null and log
    /// an error, leave no side effects). Tests that fail right now point at missing error handling.
    /// Room prefabs are built with unavailable RoomCells so Room's wall logic (which relies on Awake, and Awake does
    /// not run in EditMode) is skipped - these tests only cover RoomGenerator itself.
    /// </summary>
    public class RoomGeneratorTests
    {
        private const int k_gridUnitSize = 10;
        private const int k_randomIterations = 100;

        private MapGenerationContext _context;
        private RoomGenerator _generator;
        private Transform _container;
        private Path _path;

        private ShapeData _shape1x1x1;
        private ShapeData _shape2x1x1;
        private GameObject _prefab1x1x1;
        private GameObject _prefab2x1x1;

        private List<Object> _createdObjects;
        private List<string> _errorLogs;

        #region Test SetUp/TearDown
        [SetUp]
        public void SetUp()
        {
            _createdObjects = new();
            _errorLogs = new();
            Application.logMessageReceived += OnLogMessage;

            _context = new MapGenerationContext();
            _container = Track(new GameObject("Room Container")).transform;
            _generator = new RoomGenerator(_context, k_gridUnitSize, _container);

            _shape1x1x1 = MakeShape(Vector3Int.zero);
            _shape2x1x1 = MakeShape(Vector3Int.zero, Vector3Int.right);
            _prefab1x1x1 = MakeRoomPrefab("Room 1x1x1", Vector3Int.zero);
            _prefab2x1x1 = MakeRoomPrefab("Room 2x1x1", Vector3Int.zero, Vector3Int.right);

            _path = MakePath();
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLogMessage;
            LogAssert.ignoreFailingMessages = false;

            // Rooms spawned without a container end up at the scene root
            if (_path != null && _path.Rooms != null)
            {
                foreach (Room room in _path.Rooms)
                {
                    if (room != null)
                        Object.DestroyImmediate(room.gameObject);
                }
            }

            foreach (Object obj in _createdObjects)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }
        }
        #endregion

        #region General Tests
        [Test]
        public void TestNullContextFailsGracefully()
        {
            // Arrange
            RoomGenerator generator = null;
            _path.Add(new Blueprint(RandomVector()));
            SetRoomShapes(_path, MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1));
            CaptureErrorLogs();

            // Act/Assert
            Assert.DoesNotThrow(() => generator = new RoomGenerator(null, k_gridUnitSize, _container));
            Assert.IsFalse(generator.ParsePathAndGenerateRooms(_path));
            AssertErrorLogged();
        }

        [Test]
        public void TestNullRoomContainerFailsParse()
        {
            // Arrange
            RoomGenerator generator = new RoomGenerator(_context, k_gridUnitSize, null);
            AddBlueprint(RandomVector());
            SetRoomShapes(_path, MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1));
            CaptureErrorLogs();

            // Act
            bool result = generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsFalse(result);
            AssertErrorLogged();
            Assert.AreEqual(0, _path.RoomCount);
        }
        #endregion

        #region Parse Path Tests
        [Test]
        public void TestParseSingleBlueprint()
        {
            // Arrange
            Blueprint b1 = AddBlueprint(RandomVector());
            SetRoomShapes(_path, MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _path.RoomCount);
            Assert.IsFalse(b1.Available);
            Assert.AreEqual(ToWorld(b1.Position), _path.Rooms[0].transform.position);
        }

        [Test]
        public void TestParseTwoBlueprintsPlacesLongRoom()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint b1 = AddBlueprint(o);
            Blueprint b2 = AddBlueprint(o + Vector3Int.right);
            SetRoomShapes(_path, MakeShapeEntry(_shape2x1x1, 1, _prefab2x1x1));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _path.RoomCount);
            Assert.IsFalse(b1.Available);
            Assert.IsFalse(b2.Available);
            Assert.AreEqual(ToWorld(o), _path.Rooms[0].transform.position);
        }

        /// <summary>
        /// Path walks right to left, so the base blueprint is the 2x1x1's (1,0,0) cell. The room origin must be
        /// shifted back by the candidate cell (placement = blueprint - cell), landing on the left blueprint.
        /// </summary>
        [Test]
        public void TestParseReversedPathOffsetsRoomOrigin()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint right = AddBlueprint(o + Vector3Int.right);
            Blueprint left = AddBlueprint(o);
            SetRoomShapes(_path, MakeShapeEntry(_shape2x1x1, 1, _prefab2x1x1));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _path.RoomCount);
            Assert.IsFalse(right.Available);
            Assert.IsFalse(left.Available);
            Assert.AreEqual(ToWorld(o), _path.Rooms[0].transform.position);
        }

        [Test]
        public void TestParseNullPath()
        {
            // Arrange
            bool result = true;
            CaptureErrorLogs();

            // Act
            Assert.DoesNotThrow(() => result = _generator.ParsePathAndGenerateRooms(null));

            // Assert
            Assert.IsFalse(result);
            AssertErrorLogged();
        }

        [Test]
        public void TestParseUninitializedPath()
        {
            // Arrange
            Path path = Track(ScriptableObject.CreateInstance<Path>());
            path.Name = "Uninitialized Path";
            LogAssert.Expect(LogType.Error, new Regex("was null or not initialized"));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(path);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TestParseNullBlueprintList()
        {
            // Arrange
            SetPrivateField(_path, "_blueprintList", null);
            LogAssert.Expect(LogType.Error, new Regex("was null or not initialized"));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TestParseEmptyBlueprintList()
        {
            // Arrange
            SetRoomShapes(_path, MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1));
            LogAssert.Expect(LogType.Error, new Regex("has no blueprints to parse"));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TestParseUnavailableStartBlueprint()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint b1 = AddBlueprint(o, false);
            AddBlueprint(o + Vector3Int.right);
            AddBlueprint(o + Vector3Int.right * 2);
            SetRoomShapes(_path, MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(2, _path.RoomCount);
            AssertNoRoomAt(b1.Position);
            AssertAllBlueprintsClaimed();
        }

        [Test]
        public void TestParseRandomUnavailableBlueprints()
        {
            // Arrange
            Vector3Int o = RandomVector();
            AddBlueprint(o);
            Blueprint b2 = AddBlueprint(o + Vector3Int.right, false);
            AddBlueprint(o + Vector3Int.right * 2);
            Blueprint b4 = AddBlueprint(o + Vector3Int.right * 3, false);
            AddBlueprint(o + Vector3Int.right * 4);
            SetRoomShapes(_path, MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(3, _path.RoomCount);
            AssertNoRoomAt(b2.Position);
            AssertNoRoomAt(b4.Position);
            AssertAllBlueprintsClaimed();
        }

        [Test]
        public void TestParseUnavailableEndBlueprint()
        {
            // Arrange
            Vector3Int o = RandomVector();
            AddBlueprint(o);
            AddBlueprint(o + Vector3Int.right);
            Blueprint b3 = AddBlueprint(o + Vector3Int.right * 2, false);
            SetRoomShapes(_path, MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(2, _path.RoomCount);
            AssertNoRoomAt(b3.Position);
            AssertAllBlueprintsClaimed();
        }

        [Test]
        public void TestParseNullShapeEntries()
        {
            // Arrange
            bool result = true;
            AddBlueprint(RandomVector());
            SetRoomShapes(_path, (List<ShapeEntry>)null);
            CaptureErrorLogs();

            // Act
            Assert.DoesNotThrow(() => result = _generator.ParsePathAndGenerateRooms(_path));

            // Assert
            Assert.IsFalse(result);
            AssertErrorLogged();
        }

        [Test]
        public void TestParseEmptyShapeEntries()
        {
            // Arrange
            AddBlueprint(RandomVector());
            SetRoomShapes(_path);
            CaptureErrorLogs();

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsFalse(result);
            AssertErrorLogged();
            Assert.AreEqual(0, _path.RoomCount);
        }

        /// <summary>
        /// Line of three with only a 2x1x1 available. The first two blueprints become one room, the third can't
        /// be filled by anything so parsing must fail.
        /// </summary>
        [Test]
        public void TestParseNoOneByOneShapeEntry()
        {
            // Arrange
            Vector3Int o = RandomVector();
            AddBlueprint(o);
            AddBlueprint(o + Vector3Int.right);
            Blueprint b3 = AddBlueprint(o + Vector3Int.right * 2);
            SetRoomShapes(_path, MakeShapeEntry(_shape2x1x1, 1, _prefab2x1x1));
            LogAssert.Expect(LogType.Error, new Regex("No valid candidates found"));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, _path.RoomCount);
            Assert.IsTrue(b3.Available);
        }
        #endregion

        #region Shape Entry Tests
        [Test]
        public void TestParseShapeEntryNullRooms()
        {
            // Arrange
            AddBlueprint(RandomVector());
            SetRoomShapes(_path, MakeShapeEntryWithRooms(_shape1x1x1, 1, null));
            LogAssert.Expect(LogType.Error, "A ShapeEntry contains no rooms.");
            LogAssert.Expect(LogType.Error, new Regex("No room chosen from shape"));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, _path.RoomCount);
        }

        [Test]
        public void TestParseShapeEntryEmptyRooms()
        {
            // Arrange
            AddBlueprint(RandomVector());
            SetRoomShapes(_path, MakeShapeEntryWithRooms(_shape1x1x1, 1, new List<RoomEntry>()));
            LogAssert.Expect(LogType.Error, "A ShapeEntry contains no rooms.");
            LogAssert.Expect(LogType.Error, new Regex("No room chosen from shape"));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, _path.RoomCount);
        }

        [Test]
        public void TestParseShapeEntryNullShapeData()
        {
            // Arrange
            bool result = true;
            AddBlueprint(RandomVector());
            SetRoomShapes(_path, MakeShapeEntry(null, 1, _prefab1x1x1));
            CaptureErrorLogs();

            // Act
            Assert.DoesNotThrow(() => result = _generator.ParsePathAndGenerateRooms(_path));

            // Assert
            Assert.IsFalse(result);
            AssertErrorLogged();
        }

        [Test]
        public void TestParseShapeEntryZeroWeight()
        {
            // Arrange
            AddBlueprint(RandomVector());
            SetRoomShapes(_path, MakeShapeEntry(_shape1x1x1, 0, _prefab1x1x1));
            LogAssert.Expect(LogType.Error, new Regex("No shape chosen for generation"));
            LogAssert.Expect(LogType.Error, new Regex("No candidates chosen from bucketed list"));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TestParseShapeEntryNegativeWeight()
        {
            // Arrange
            AddBlueprint(RandomVector());
            SetRoomShapes(_path, MakeShapeEntry(_shape1x1x1, -5, _prefab1x1x1));
            LogAssert.Expect(LogType.Error, new Regex("No shape chosen for generation"));
            LogAssert.Expect(LogType.Error, new Regex("No candidates chosen from bucketed list"));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TestParseShapeEntryMaxWeight()
        {
            // Arrange
            AddBlueprint(RandomVector());
            SetRoomShapes(_path, MakeShapeEntry(_shape1x1x1, int.MaxValue, _prefab1x1x1));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _path.RoomCount);
        }
        #endregion

        #region Room Entry Tests
        [Test]
        public void TestParseRoomEntryNoPrefab()
        {
            // Arrange
            AddBlueprint(RandomVector());
            SetRoomShapes(_path, MakeShapeEntry(_shape1x1x1, 1, null));
            LogAssert.Expect(LogType.Error, new Regex("No spawnable rooms found"));
            LogAssert.Expect(LogType.Error, new Regex("No room chosen from shape"));

            // Act
            bool result = _generator.ParsePathAndGenerateRooms(_path);

            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, _path.RoomCount);
        }

        [Test]
        public void TestSelectRoomSkipsNullPrefab()
        {
            // Arrange
            List<ShapeEntry> entries = new()
            {
                MakeShapeEntryWithRooms(_shape1x1x1, 1, new List<RoomEntry>
                {
                    MakeRoomEntry(null, 100),
                    MakeRoomEntry(_prefab1x1x1, 1),
                })
            };

            // Act/Assert
            for (int i = 0; i < k_randomIterations; i++)
                Assert.AreEqual(_prefab1x1x1, _generator.SelectRandomRoomFromShape(entries, _shape1x1x1).Prefab);
        }

        [Test]
        public void TestSelectRoomZeroWeightNeverPicked()
        {
            // Arrange
            GameObject other = MakeRoomPrefab("Other Room 1x1x1", Vector3Int.zero);
            List<ShapeEntry> entries = new()
            {
                MakeShapeEntryWithRooms(_shape1x1x1, 1, new List<RoomEntry>
                {
                    MakeRoomEntry(other, 0),
                    MakeRoomEntry(_prefab1x1x1, 1),
                })
            };

            // Act/Assert
            for (int i = 0; i < k_randomIterations; i++)
                Assert.AreEqual(_prefab1x1x1, _generator.SelectRandomRoomFromShape(entries, _shape1x1x1).Prefab);
        }

        [Test]
        public void TestSelectRoomAllZeroWeight()
        {
            // Arrange
            List<ShapeEntry> entries = new() { MakeShapeEntryWithRooms(_shape1x1x1, 1, new List<RoomEntry> { MakeRoomEntry(_prefab1x1x1, 0) }) };
            LogAssert.Expect(LogType.Error, new Regex("No spawnable rooms found"));

            // Act
            RoomEntry room = _generator.SelectRandomRoomFromShape(entries, _shape1x1x1);

            // Assert
            Assert.IsNull(room.Prefab);
        }

        [Test]
        public void TestSelectRoomNegativeWeightNeverPicked()
        {
            // Arrange
            GameObject other = MakeRoomPrefab("Other Room 1x1x1", Vector3Int.zero);
            List<ShapeEntry> entries = new()
            {
                MakeShapeEntryWithRooms(_shape1x1x1, 1, new List<RoomEntry>
                {
                    MakeRoomEntry(other, -5),
                    MakeRoomEntry(_prefab1x1x1, 1),
                })
            };

            // Act/Assert
            for (int i = 0; i < k_randomIterations; i++)
                Assert.AreEqual(_prefab1x1x1, _generator.SelectRandomRoomFromShape(entries, _shape1x1x1).Prefab);
        }

        [Test]
        public void TestSelectRoomMaxWeight()
        {
            // Arrange
            List<ShapeEntry> entries = new() { MakeShapeEntryWithRooms(_shape1x1x1, 1, new List<RoomEntry> { MakeRoomEntry(_prefab1x1x1, int.MaxValue) }) };

            // Act
            RoomEntry room = _generator.SelectRandomRoomFromShape(entries, _shape1x1x1);

            // Assert
            Assert.AreEqual(_prefab1x1x1, room.Prefab);
        }

        /*  UNUSED: Tech debt item where weights used need to be 'long' instead of 'int'
        /// <summary>
        /// int.MaxValue + 1 overflows the total weight in WeightedRandom.TryPick, so nothing gets picked.
        /// </summary>
        [Test]
        public void TestSelectRoomMaxWeightWithOtherRooms()
        {
            // Arrange
            GameObject other = MakeRoomPrefab("Other Room 1x1x1", Vector3Int.zero);
            List<ShapeEntry> entries = new()
            {
                MakeShapeEntryWithRooms(_shape1x1x1, 1, new List<RoomEntry>
                {
                    MakeRoomEntry(_prefab1x1x1, int.MaxValue),
                    MakeRoomEntry(other, 1),
                })
            };

            // Act
            RoomEntry room = _generator.SelectRandomRoomFromShape(entries, _shape1x1x1);

            // Assert
            Assert.IsNotNull(room.Prefab);
        }
        */
        #endregion

        #region Bucket All Candidates Tests
        [Test]
        public void TestBucketNullCandidates()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Candidate list was null or empty.");

            // Act
            var buckets = _generator.BucketAllCandidates(null);

            // Assert
            Assert.IsNull(buckets);
        }

        /// <summary>
        /// The error message says "null or empty" but an empty list currently returns an empty collection.
        /// </summary>
        [Test]
        public void TestBucketEmptyCandidates()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Candidate list was null or empty.");

            // Act
            var buckets = _generator.BucketAllCandidates(new List<ShapeCandidate>());

            // Assert
            Assert.IsNull(buckets);
        }

        [Test]
        public void TestBucketCandidateNullShape()
        {
            // Arrange
            List<ShapeCandidate> candidates = new() { new ShapeCandidate(null, Vector3Int.zero) };
            LogAssert.Expect(LogType.Error, "Candidate has no shape.");

            // Act
            var buckets = _generator.BucketAllCandidates(candidates);

            // Assert
            Assert.IsNull(buckets);
        }

        [Test]
        public void TestBucketNullCandidateInList()
        {
            // Arrange
            BucketCollection<ShapeData, ShapeCandidate> buckets = null;
            List<ShapeCandidate> candidates = new() { new ShapeCandidate(_shape1x1x1, Vector3Int.zero), null };
            CaptureErrorLogs();

            // Act
            Assert.DoesNotThrow(() => buckets = _generator.BucketAllCandidates(candidates));

            // Assert
            Assert.IsNull(buckets);
            AssertErrorLogged();
        }

        [Test]
        public void TestBucketOneCandidate()
        {
            // Arrange
            List<ShapeCandidate> candidates = new() { new ShapeCandidate(_shape1x1x1, Vector3Int.zero) };

            // Act
            var buckets = _generator.BucketAllCandidates(candidates);

            // Assert
            Assert.AreEqual(1, buckets.BucketCount);
            Assert.AreEqual(1, buckets.GetCountInBucket(_shape1x1x1));
        }

        [Test]
        public void TestBucketAllSameShape()
        {
            // Arrange
            List<ShapeCandidate> candidates = new()
            {
                new ShapeCandidate(_shape2x1x1, Vector3Int.zero),
                new ShapeCandidate(_shape2x1x1, Vector3Int.right),
                new ShapeCandidate(_shape2x1x1, Vector3Int.zero),
            };

            // Act
            var buckets = _generator.BucketAllCandidates(candidates);

            // Assert
            Assert.AreEqual(1, buckets.BucketCount);
            Assert.AreEqual(3, buckets.GetCountInBucket(_shape2x1x1));
        }

        [Test]
        public void TestBucketAllDifferentShapes()
        {
            // Arrange
            ShapeData shape1x2x1 = MakeShape(Vector3Int.zero, Vector3Int.up);
            List<ShapeCandidate> candidates = new()
            {
                new ShapeCandidate(_shape1x1x1, Vector3Int.zero),
                new ShapeCandidate(_shape2x1x1, Vector3Int.zero),
                new ShapeCandidate(shape1x2x1, Vector3Int.zero),
            };

            // Act
            var buckets = _generator.BucketAllCandidates(candidates);

            // Assert
            Assert.AreEqual(3, buckets.BucketCount);
            Assert.AreEqual(1, buckets.GetCountInBucket(_shape1x1x1));
            Assert.AreEqual(1, buckets.GetCountInBucket(_shape2x1x1));
            Assert.AreEqual(1, buckets.GetCountInBucket(shape1x2x1));
        }
        #endregion

        #region Pick Weighted Candidate Tests
        [Test]
        public void TestPickSingleCandidate()
        {
            // Arrange
            ShapeCandidate candidate = new ShapeCandidate(_shape1x1x1, Vector3Int.zero);
            var buckets = _generator.BucketAllCandidates(new List<ShapeCandidate> { candidate });
            List<ShapeEntry> entries = new() { MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1) };

            // Act
            ShapeCandidate picked = _generator.PickWeightedCandidate(entries, buckets);

            // Assert
            Assert.AreSame(candidate, picked);
        }

        [Test]
        public void TestPickNullEntries()
        {
            // Arrange
            ShapeCandidate picked = null;
            var buckets = _generator.BucketAllCandidates(new List<ShapeCandidate> { new ShapeCandidate(_shape1x1x1, Vector3Int.zero) });
            CaptureErrorLogs();

            // Act
            Assert.DoesNotThrow(() => picked = _generator.PickWeightedCandidate(null, buckets));

            // Assert
            Assert.IsNull(picked);
            AssertErrorLogged();
        }

        [Test]
        public void TestPickEmptyEntries()
        {
            // Arrange
            var buckets = _generator.BucketAllCandidates(new List<ShapeCandidate> { new ShapeCandidate(_shape1x1x1, Vector3Int.zero) });
            CaptureErrorLogs();

            // Act
            ShapeCandidate picked = _generator.PickWeightedCandidate(new List<ShapeEntry>(), buckets);

            // Assert
            Assert.IsNull(picked);
            AssertErrorLogged();
        }

        [Test]
        public void TestPickNullBuckets()
        {
            // Arrange
            List<ShapeEntry> entries = new() { MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1) };
            LogAssert.Expect(LogType.Error, "Bucket collection was null or empty.");

            // Act
            ShapeCandidate picked = _generator.PickWeightedCandidate(entries, null);

            // Assert
            Assert.IsNull(picked);
        }

        [Test]
        public void TestPickEmptyBuckets()
        {
            // Arrange
            List<ShapeEntry> entries = new() { MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1) };
            LogAssert.Expect(LogType.Error, "Bucket collection was null or empty.");

            // Act
            ShapeCandidate picked = _generator.PickWeightedCandidate(entries, new BucketCollection<ShapeData, ShapeCandidate>());

            // Assert
            Assert.IsNull(picked);
        }

        [Test]
        public void TestPickBucketWithNoCandidates()
        {
            // Arrange
            var buckets = _generator.BucketAllCandidates(new List<ShapeCandidate> { new ShapeCandidate(_shape1x1x1, Vector3Int.zero) });
            buckets.TryGetBucket(_shape1x1x1, out List<ShapeCandidate> bucket);
            bucket.Clear();
            List<ShapeEntry> entries = new() { MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1) };
            LogAssert.Expect(LogType.Error, new Regex("Bucket has no items"));

            // Act
            ShapeCandidate picked = _generator.PickWeightedCandidate(entries, buckets);

            // Assert
            Assert.IsNull(picked);
        }

        [Test]
        public void TestPickNoEntryMatchesBucket()
        {
            // Arrange
            var buckets = _generator.BucketAllCandidates(new List<ShapeCandidate> { new ShapeCandidate(_shape1x1x1, Vector3Int.zero) });
            List<ShapeEntry> entries = new() { MakeShapeEntry(_shape2x1x1, 1, _prefab2x1x1) };
            LogAssert.Expect(LogType.Error, new Regex("No shapes are eligible for picking"));

            // Act
            ShapeCandidate picked = _generator.PickWeightedCandidate(entries, buckets);

            // Assert
            Assert.IsNull(picked);
        }

        [Test]
        public void TestPickZeroWeightShapeNeverPicked()
        {
            // Arrange
            var buckets = MakeBothShapeBuckets();
            List<ShapeEntry> entries = new()
            {
                MakeShapeEntry(_shape1x1x1, 0, _prefab1x1x1),
                MakeShapeEntry(_shape2x1x1, 1, _prefab2x1x1),
            };

            // Act/Assert
            for (int i = 0; i < k_randomIterations; i++)
                Assert.AreEqual(_shape2x1x1, _generator.PickWeightedCandidate(entries, buckets).Shape);
        }

        [Test]
        public void TestPickNegativeWeightShapeNeverPicked()
        {
            // Arrange
            var buckets = MakeBothShapeBuckets();
            List<ShapeEntry> entries = new()
            {
                MakeShapeEntry(_shape1x1x1, -5, _prefab1x1x1),
                MakeShapeEntry(_shape2x1x1, 1, _prefab2x1x1),
            };

            // Act/Assert
            for (int i = 0; i < k_randomIterations; i++)
                Assert.AreEqual(_shape2x1x1, _generator.PickWeightedCandidate(entries, buckets).Shape);
        }

        /*  UNUSED: Tech debt item where weights used need to be 'long' instead of 'int'
        /// <summary>
        /// int.MaxValue + 1 overflows the total weight in WeightedRandom.TryPick, so nothing gets picked.
        /// </summary>
        [Test]
        public void TestPickMaxWeightShapeWithOtherShapes()
        {
            // Arrange
            var buckets = MakeBothShapeBuckets();
            List<ShapeEntry> entries = new()
            {
                MakeShapeEntry(_shape1x1x1, int.MaxValue, _prefab1x1x1),
                MakeShapeEntry(_shape2x1x1, 1, _prefab2x1x1),
            };

            // Act
            ShapeCandidate picked = _generator.PickWeightedCandidate(entries, buckets);

            // Assert
            Assert.IsNotNull(picked);
        }
        */
        #endregion

        #region Select Random Room From Shape Tests
        [Test]
        public void TestSelectRoomFromShape()
        {
            // Arrange
            List<ShapeEntry> entries = new()
            {
                MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1),
                MakeShapeEntry(_shape2x1x1, 1, _prefab2x1x1),
            };

            // Act
            RoomEntry room = _generator.SelectRandomRoomFromShape(entries, _shape2x1x1);

            // Assert
            Assert.AreEqual(_prefab2x1x1, room.Prefab);
        }

        [Test]
        public void TestSelectRoomNullShapeList()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Shape entry list was null or empty.");

            // Act
            RoomEntry room = _generator.SelectRandomRoomFromShape(null, _shape1x1x1);

            // Assert
            Assert.IsNull(room.Prefab);
        }

        [Test]
        public void TestSelectRoomEmptyShapeList()
        {
            // Arrange
            LogAssert.Expect(LogType.Error, "Shape entry list was null or empty.");

            // Act
            RoomEntry room = _generator.SelectRandomRoomFromShape(new List<ShapeEntry>(), _shape1x1x1);

            // Assert
            Assert.IsNull(room.Prefab);
        }

        [Test]
        public void TestSelectRoomShapeEntryNoRooms()
        {
            // Arrange
            List<ShapeEntry> entries = new() { MakeShapeEntryWithRooms(_shape1x1x1, 1, null) };
            LogAssert.Expect(LogType.Error, "A ShapeEntry contains no rooms.");

            // Act
            RoomEntry room = _generator.SelectRandomRoomFromShape(entries, _shape1x1x1);

            // Assert
            Assert.IsNull(room.Prefab);
        }

        /// <summary>
        /// The "ShapeData was null" branch is unreachable; the first guard already catches a null shape and logs
        /// the list message instead.
        /// </summary>
        [Test]
        public void TestSelectRoomNullShape()
        {
            // Arrange
            List<ShapeEntry> entries = new() { MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1) };
            LogAssert.Expect(LogType.Error, "ShapeData was null");

            // Act
            RoomEntry room = _generator.SelectRandomRoomFromShape(entries, null);

            // Assert
            Assert.IsNull(room.Prefab);
        }

        [Test]
        public void TestSelectRoomShapeNotInList()
        {
            // Arrange
            List<ShapeEntry> entries = new() { MakeShapeEntry(_shape1x1x1, 1, _prefab1x1x1) };
            LogAssert.Expect(LogType.Error, new Regex("No spawnable rooms found for shape"));

            // Act
            RoomEntry room = _generator.SelectRandomRoomFromShape(entries, _shape2x1x1);

            // Assert
            Assert.IsNull(room.Prefab);
        }
        #endregion

        #region Generate Room Tests
        [Test]
        public void TestGenerateRoom()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint b1 = AddBlueprint(o);

            // Act
            Room room = _generator.GenerateRoom(_path, _prefab1x1x1, o);

            // Assert
            Assert.IsNotNull(room);
            Assert.AreEqual(_container, room.transform.parent);
            Assert.AreEqual(ToWorld(o), room.transform.position);
            Assert.IsFalse(b1.Available);
            Assert.Contains(room, _path.Rooms);
        }

        [Test]
        public void TestGenerateMultiCellRoomClaimsAllBlueprints()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint b1 = AddBlueprint(o);
            Blueprint b2 = AddBlueprint(o + Vector3Int.right);

            // Act
            Room room = _generator.GenerateRoom(_path, _prefab2x1x1, o);

            // Assert
            Assert.IsNotNull(room);
            Assert.IsFalse(b1.Available);
            Assert.IsFalse(b2.Available);
        }

        [Test]
        public void TestGenerateRoomNullPath()
        {
            // Arrange
            Room room = null;
            Vector3Int o = RandomVector();
            Blueprint b1 = AddBlueprint(o);
            CaptureErrorLogs();

            // Act
            Assert.DoesNotThrow(() => room = _generator.GenerateRoom(null, _prefab1x1x1, o));

            // Assert
            Assert.IsNull(room);
            AssertErrorLogged();
            Assert.IsTrue(b1.Available, "Blueprint was claimed even though generation failed.");
            Assert.AreEqual(0, _container.childCount, "A room was left in the scene even though generation failed.");
        }

        [Test]
        public void TestGenerateRoomUninitializedPath()
        {
            // Arrange
            Room room = null;
            Vector3Int o = RandomVector();
            Blueprint b1 = AddBlueprint(o);
            Path path = Track(ScriptableObject.CreateInstance<Path>());
            CaptureErrorLogs();

            // Act
            Assert.DoesNotThrow(() => room = _generator.GenerateRoom(path, _prefab1x1x1, o));

            // Assert
            Assert.IsNull(room);
            AssertErrorLogged();
            Assert.IsTrue(b1.Available, "Blueprint was claimed even though generation failed.");
            Assert.AreEqual(0, _container.childCount, "A room was left in the scene even though generation failed.");
        }

        [Test]
        public void TestGenerateRoomNullPrefab()
        {
            // Arrange
            Room room = null;
            Vector3Int o = RandomVector();
            AddBlueprint(o);
            CaptureErrorLogs();

            // Act
            Assert.DoesNotThrow(() => room = _generator.GenerateRoom(_path, null, o));

            // Assert
            Assert.IsNull(room);
            AssertErrorLogged();
        }

        [Test]
        public void TestGenerateRoomPrefabWithoutRoomComponent()
        {
            // Arrange
            Room room = null;
            Vector3Int o = RandomVector();
            AddBlueprint(o);
            GameObject prefab = Track(new GameObject("Not A Room"));
            CaptureErrorLogs();

            // Act
            Assert.DoesNotThrow(() => room = _generator.GenerateRoom(_path, prefab, o));

            // Assert
            Assert.IsNull(room);
            AssertErrorLogged();
            Assert.AreEqual(0, _container.childCount, "A room was left in the scene even though generation failed.");
        }

        [Test]
        public void TestGenerateRoomMissingBlueprintReturnsNull()
        {
            // Arrange
            Vector3Int o = RandomVector();
            AddBlueprint(o);        // No blueprint at o + right
            LogAssert.Expect(LogType.Error, new Regex("No blueprint exists at this room's cell's position"));

            // Act
            Room room = _generator.GenerateRoom(_path, _prefab2x1x1, o);

            // Assert
            Assert.IsNull(room);
        }

        [Test]
        public void TestGenerateRoomMissingBlueprintLeavesNoRoom()
        {
            // Arrange
            Vector3Int o = RandomVector();
            AddBlueprint(o);        // No blueprint at o + right
            LogAssert.Expect(LogType.Error, new Regex("No blueprint exists at this room's cell's position"));

            // Act
            _generator.GenerateRoom(_path, _prefab2x1x1, o);

            // Assert
            Assert.AreEqual(0, _container.childCount, "A room was left in the scene even though generation failed.");
        }

        [Test]
        public void TestGenerateRoomMissingBlueprintClaimsNothing()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint b1 = AddBlueprint(o);     // No blueprint at o + right
            LogAssert.Expect(LogType.Error, new Regex("No blueprint exists at this room's cell's position"));

            // Act
            _generator.GenerateRoom(_path, _prefab2x1x1, o);

            // Assert
            Assert.IsTrue(b1.Available, "Blueprint was claimed even though generation failed.");
        }

        [Test]
        public void TestGenerateRoomNoCells()
        {
            // Arrange
            Vector3Int o = RandomVector();
            AddBlueprint(o);
            GameObject prefab = MakeRoomPrefab("Room No Cells");
            CaptureErrorLogs();

            // Act
            Room room = _generator.GenerateRoom(_path, prefab, o);

            // Assert
            Assert.IsNull(room);
            AssertErrorLogged();
        }

        [Test]
        public void TestGenerateRoomNoContainer()
        {
            // Arrange
            RoomGenerator generator = new RoomGenerator(_context, k_gridUnitSize, null);
            Vector3Int o = RandomVector();
            AddBlueprint(o);
            CaptureErrorLogs();

            // Act
            Room room = generator.GenerateRoom(_path, _prefab1x1x1, o);
            if (room != null)
                _createdObjects.Add(room.gameObject);

            // Assert
            Assert.IsNull(room);
            AssertErrorLogged();
        }

        [Test]
        public void TestGenerateRoomOverlapsUnavailableBlueprint()
        {
            // Arrange
            Vector3Int o = RandomVector();
            AddBlueprint(o, false);     // Already claimed by another room
            CaptureErrorLogs();

            // Act
            Room room = _generator.GenerateRoom(_path, _prefab1x1x1, o);

            // Assert
            Assert.IsNull(room);
            AssertErrorLogged();
        }

        [Test]
        public void TestGenerateRoomPartiallyOverlapsUnavailableBlueprint()
        {
            // Arrange
            Vector3Int o = RandomVector();
            Blueprint b1 = AddBlueprint(o);
            AddBlueprint(o + Vector3Int.right, false);      // Already claimed by another room
            CaptureErrorLogs();

            // Act
            Room room = _generator.GenerateRoom(_path, _prefab2x1x1, o);

            // Assert
            Assert.IsNull(room);
            AssertErrorLogged();
            Assert.IsTrue(b1.Available, "Blueprint was claimed even though generation failed.");
        }
        #endregion

        #region Helpers
        private T Track<T>(T obj) where T : Object
        {
            _createdObjects.Add(obj);
            return obj;
        }

        private Blueprint AddBlueprint(Vector3Int position, bool available = true)
        {
            Blueprint blueprint = new Blueprint(position);
            blueprint.Available = available;
            _context.BlueprintDictionary.Add(blueprint.Position, blueprint);
            _path.Add(blueprint);
            return blueprint;
        }

        private Path MakePath()
        {
            Path path = Track(ScriptableObject.CreateInstance<Path>());
            path.Name = "Test Path";
            path.Initialize();
            SetRoomShapes(path);
            return path;
        }

        private ShapeData MakeShape(params Vector3Int[] cells)
        {
            ShapeData shape = Track(ScriptableObject.CreateInstance<ShapeData>());
            shape.Cells = new SerializedDictionary<Vector3Int, CellState>();
            foreach (Vector3Int cell in cells)
                shape.Cells.Add(cell, CellState.Blueprint);
            return shape;
        }

        /// <summary>
        /// Room cells are unavailable so Room skips its wall/entranceway logic.
        /// </summary>
        private GameObject MakeRoomPrefab(string name, params Vector3Int[] cellPositions)
        {
            GameObject prefab = Track(new GameObject(name));
            Room room = prefab.AddComponent<Room>();

            List<RoomCell> cells = new();
            foreach (Vector3Int position in cellPositions)
                cells.Add(new RoomCell { Position = position, IsAvailable = false, Walls = new List<RoomWall>() });

            SetPrivateField(room, "_roomCells", cells);
            return prefab;
        }

        private static RoomEntry MakeRoomEntry(GameObject prefab, int weight)
        {
            object entry = new RoomEntry();     // Boxed so reflection writes stick
            SetPrivateField(entry, "_prefab", prefab);
            SetPrivateField(entry, "_weight", weight);
            return (RoomEntry)entry;
        }

        private static ShapeEntry MakeShapeEntry(ShapeData shape, int weight, GameObject prefab)
        {
            return MakeShapeEntryWithRooms(shape, weight, new List<RoomEntry> { MakeRoomEntry(prefab, 1) });
        }

        private static ShapeEntry MakeShapeEntryWithRooms(ShapeData shape, int weight, List<RoomEntry> rooms)
        {
            object entry = new ShapeEntry();    // Boxed so reflection writes stick
            SetPrivateField(entry, "_roomShape", shape);
            SetPrivateField(entry, "_weight", weight);
            SetPrivateField(entry, "_rooms", rooms);
            return (ShapeEntry)entry;
        }

        private BucketCollection<ShapeData, ShapeCandidate> MakeBothShapeBuckets()
        {
            return _generator.BucketAllCandidates(new List<ShapeCandidate>
            {
                new ShapeCandidate(_shape1x1x1, Vector3Int.zero),
                new ShapeCandidate(_shape2x1x1, Vector3Int.zero),
            });
        }

        private static void SetRoomShapes(Path path, params ShapeEntry[] entries)
        {
            SetRoomShapes(path, new List<ShapeEntry>(entries));
        }

        private static void SetRoomShapes(Path path, List<ShapeEntry> entries)
        {
            PropertyInfo property = typeof(Path).GetProperty(nameof(Path.RoomShapes));
            property.GetSetMethod(true).Invoke(path, new object[] { entries });
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field {fieldName} not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private Vector3 ToWorld(Vector3Int position)
        {
            return (Vector3)(position * k_gridUnitSize);
        }

        private void AssertNoRoomAt(Vector3Int position)
        {
            foreach (Room room in _path.Rooms)
                Assert.AreNotEqual(ToWorld(position), room.transform.position, $"Room placed on unavailable blueprint {position}.");
        }

        private void AssertAllBlueprintsClaimed()
        {
            foreach (Blueprint blueprint in _path.BlueprintList)
                Assert.IsFalse(blueprint.Available, $"Blueprint {blueprint.Position} was never claimed by a room.");
        }

        /// <summary>
        /// For cases where the exact error message is up to the implementation; only requires that some error was logged.
        /// </summary>
        private void CaptureErrorLogs()
        {
            LogAssert.ignoreFailingMessages = true;
        }

        private void AssertErrorLogged()
        {
            Assert.IsNotEmpty(_errorLogs, "Expected an error to be logged.");
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                _errorLogs.Add(condition);
        }

        private Vector3Int RandomVector(int range = 1000)
        {
            int randomX = Random.Range(-range, range);
            int randomY = Random.Range(-range, range);
            int randomZ = Random.Range(-range, range);
            return new Vector3Int(randomX, randomY, randomZ);
        }
        #endregion
    }
}
