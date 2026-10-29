## Open

### 1. WeightedRandom.TryPick int overflow — logged 2026-09-23
- **Where:** `SharedPackages/com.ryanslibrary/Runtime/Probability.cs`, `WeightedRandom.TryPick` (shared package, affects all projects using com.ryanslibrary)
- **Problem:** `totalWeight` is `int`; weights summing past int.MaxValue (e.g. `[int.MaxValue, 1]` or 3× 1,000,000,000) wrap negative → `TryPick` returns false, nothing picked.
- **Fix:** accumulate in `long`. `Random.Range` has no long overload, so roll via `(long)(Random.value * totalWeight)`, clamped to `totalWeight - 1`. Alt: cap weights (e.g. 1,000,000) via `[Min]`/`[Range]` instead.
- **Tests:** `RoomGeneratorTests.TestPickMaxWeightShapeWithOtherShapes`, `TestSelectRoomMaxWeightWithOtherRooms` fail until fixed.

### 2. No rollback when path parsing fails partway — logged 2026-09-23
- **Where:** `Assets/Scripts/Procedural Generation/RoomGenerator.cs`, `ParsePathAndGenerateRooms`
- **Problem:** if a blueprint fails mid-path, rooms already spawned for that path stay in the scene and their blueprints stay unavailable. Path is left half-generated.
- **Fix:** track rooms + claimed blueprints spawned during the call; on failure destroy those rooms, set blueprints back to available, remove rooms from `path.Rooms`. Alt: parse whole path first, then spawn only if every blueprint has a valid pick.

### 3. Room prefab cells not validated against ShapeData — logged 2026-09-23
- **Where:** `Assets/Scripts/Procedural Generation/RoomGenerator.cs`, `ParsePathAndGenerateRooms` / `GenerateRoom`
- **Problem:** the room prefab is picked by ShapeData, but its `Room.RoomCells` are never checked against `ShapeData.Cells` (Blueprint-marked cells). A mismatched prefab claims the wrong blueprints or fails placement with a misleading "no blueprint" error.
- **Fix:** validate `RoomCells` positions == ShapeData Blueprint cells, either when picking the room (`SelectRandomRoomFromShape`) or in an editor validation pass on Path assets.

## Resolved
(none)