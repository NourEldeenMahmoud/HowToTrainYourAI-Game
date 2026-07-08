# MiniGame 3 Recovery Plan

**Date:** 2026-07-03  
**Scene:** `Assets/Scenes/Aya/MiniGame 3.unity`  
**Scope:** Scene-data and camera recovery for MiniGame 3  
**Primary Goal:** Restore playable Task 1 -> Task 2 -> Task 3 progression without script changes unless validation proves scripts are still blocking gameplay.

---

## Current Goal

Fix the blocker scene-data and camera issues in `Assets/Scenes/Aya/MiniGame 3.unity`.

The immediate priority is to repair missing lab mesh references, broken serialized task references, stale target-slot coordinates, and the camera setup that makes the Game View appear black or heavily zoomed in.

---

## Working Constraints

- Fix scene data first, not scripts.
- Work initially only in `Assets/Scenes/Aya/MiniGame 3.unity`.
- Do not touch scripts unless scene-data repair is not enough.
- Preserve existing object references where possible.
- Avoid creating new scene objects unless an intended object is clearly missing.
- Validate all task references and coordinates after edits.
- Treat Task 2's intended count as unresolved until confirmed.

---

## Progress So Far

### Completed

- Inspected Unity MCP/custom tool scene state.
- Identified `MG3TaskDefinition`, device, and slot reference issues.
- Validated camera-related critical visual issues and dependencies.
- Produced this implementation and verification plan.

### Not Yet Done

- Scene-data edits have not been applied yet.
- Camera edits have not been applied yet.
- Lab mesh import/reference repair has not been applied yet.
- Play Mode verification has not been run yet.

### Open Blocker

Task 2 has 7 real devices and 7 real `G_A` target slots in the live scene, but the serialized arrays are length 8.

Recommended handling: resize Task 2 `devices` and `slots` arrays to 7 unless an intended 8th device and 8th slot are identified.

---

## Important Discovery

### Lab Objects Are Missing Mesh Data

The lab visibility issue is not limited to Game View or Play Mode.

Confirmed live scene state:

- `sci-fi_lab` exists and is active in the scene hierarchy.
- `sci-fi_lab` has 33 active child `MeshRenderer` components.
- All 33 child `MeshFilter.sharedMesh` references are null.
- Renderer bounds are zero-size, so Unity has no lab mesh geometry to draw.
- `Assets/Mini Game 3/Assets/sci-fi_lab.glb` exists, but Unity reports it as `DefaultAsset`, not as an imported model asset.

This means the lab walls/environment will not appear in Scene View or Game View until the GLB/model import or mesh references are restored.

Likely causes:

- Required GLB/glTF importer package is missing or disabled.
- The GLB asset failed to import as a model.
- Scene mesh references were lost after project/package changes.

Recommended fix direction:

1. Restore GLB model import support.
2. Reimport `Assets/Mini Game 3/Assets/sci-fi_lab.glb`.
3. Verify `sci-fi_lab` children have non-null `MeshFilter.sharedMesh` references.
4. Verify the lab is visible in Scene View before testing Game View camera fixes.

---

## Important Discovery: Task 2 Count Mismatch

Task 2 does not currently appear to have 8 real devices or 8 real target slots.

Observed live scene state:

- 7 Task 2 `gravity_generator` devices.
- 7 `G_A` target slots.
- `Task 2 Devices.devices` serialized as 8 null entries.
- `Task 2 Devices.slots` serialized as 8 entries, with only 4 valid references and 4 null references.

The safest recommended fix is to make Task 2 a 7-device/7-slot puzzle using the real objects already present in the scene.

---

## Phase 1: Back Up And Freeze State

Before editing scene data:

1. Save the current `MiniGame 3` scene.
2. Check `git status` and review the current diff.
3. Keep unrelated user changes untouched.
4. Make scene-data changes in one focused pass.
5. Verify the scene after each major repair group if possible.

---

## Phase 2: Restore Lab Mesh Visibility

Current problem:

- Lab objects are not visible even in Scene View.
- The `sci-fi_lab` hierarchy is active, but its child MeshFilters have null meshes.
- The GLB asset exists but is not currently imported as model geometry.

Repair steps:

1. Check whether the project has a GLB/glTF importer package installed and enabled.
2. If missing, install the required importer package used by the project.
3. Force reimport `Assets/Mini Game 3/Assets/sci-fi_lab.glb`.
4. Inspect `sci-fi_lab` children and confirm MeshFilters have assigned meshes.
5. Frame `sci-fi_lab` in Scene View and confirm walls/environment geometry is visible.

Expected post-fix state:

- `sci-fi_lab` remains active.
- Child MeshRenderers remain enabled.
- Child MeshFilters have non-null `sharedMesh` values.
- Renderer bounds are non-zero.
- Lab walls/environment are visible in Scene View before entering Play Mode.

---

## Phase 3: Repair Task References

### Task 1: Exact Placement

Current problem:

- Device array has 4 entries, but `device[3]` is null.
- Slot coordinates are stale or duplicated.

Set `Task 1 Devices.devices` to these 4 entries:

| Index | Device | Scene Path |
|---|---|---|
| 0 | `tablehall` | `MG3_Tasks/Task 1 Devices/tablehall` |
| 1 | `large_sci-fi_vat` | `MG3_Tasks/Task 1 Devices/large_sci-fi_vat` |
| 2 | `sci-fi_terminal` | `MG3_Tasks/Task 1 Devices/sci-fi_terminal` |
| 3 | `sci_fi_computer_table` | `MG3_Tasks/Task 1 Devices/sci_fi_computer_table` |

Keep the existing Task 1 slot objects, but sync their `MG3TargetSlot.coordinate` values to the nearest grid coordinates:

| Slot | Required Device ID | Correct Coordinate |
|---|---|---|
| `metal_floor_tile (284)` | `D1` | `(-10, 1)` |
| `metal_floor_tile (269)` | `D2` | `(-10, 5)` |
| `metal_floor_tile (188)` | `D3` | `(-4, 6)` |
| `metal_floor_tile (165)` | `D4` | `(-2, 3)` |

Expected Task 1 post-fix state:

- 4 devices.
- 4 slots.
- 0 null device references.
- 0 null slot references.
- Slot coordinates match nearest grid coordinates.

---

### Task 2: Group Placement

Current problem:

- Serialized `devices` array has 8 null entries.
- Serialized `slots` array has 8 entries: 4 valid, 4 null.
- Scene only has 7 real Task 2 devices and 7 real `G_A` slots.

Recommended fix:

- Resize `Task 2 Devices.devices` to 7.
- Resize `Task 2 Devices.slots` to 7.
- Assign the 7 existing devices and 7 existing `G_A` slots.

Set `Task 2 Devices.devices` to these 7 entries:

| Device | Correct Coordinate |
|---|---|
| `gravity_generator (4)` | `(-4, 3)` |
| `gravity_generator (8)` | `(-3, 3)` |
| `gravity_generator (9)` | `(-2, 3)` |
| `gravity_generator (10)` | `(-6, 5)` |
| `gravity_generator (5)` | `(-2, 7)` |
| `gravity_generator (6)` | `(-3, 7)` |
| `gravity_generator (7)` | `(-4, 7)` |

Set `Task 2 Devices.slots` to these 7 entries:

| Slot | Required Group ID | Correct Coordinate |
|---|---|---|
| `metal_floor_tile (170)` | `G_A` | `(-7, 3)` |
| `metal_floor_tile (198)` | `G_A` | `(-7, 5)` |
| `metal_floor_tile (201)` | `G_A` | `(-6, 4)` |
| `metal_floor_tile (203)` | `G_A` | `(-8, 4)` |
| `metal_floor_tile (236)` | `G_A` | `(-9, 3)` |
| `metal_floor_tile (245)` | `G_A` | `(-10, 4)` |
| `metal_floor_tile (248)` | `G_A` | `(-9, 5)` |

Expected Task 2 post-fix state:

- 7 devices.
- 7 slots.
- 0 null device references.
- 0 null slot references.
- All slots use required group ID `G_A`.
- Slot coordinates match nearest grid coordinates.

If an 8th Task 2 device is intended:

1. Identify or create the missing 8th `gravity_generator`.
2. Identify or create the missing 8th `G_A` slot.
3. Assign both to the serialized arrays.
4. Re-run validation against 8 devices and 8 slots.

Do not guess the 8th object without design confirmation.

---

### Task 3: Size Ordering

Current problem:

- Devices exist.
- All 4 slot references are null.
- Candidate size-order slots exist, but their coordinates are stale/default `(0, 0)`.

Set `Task 3 Devices.slots` to these 4 entries:

| Slot | Required Size Rank | Correct Coordinate |
|---|---:|---|
| `metal_floor_tile (153)` | 1 | `(-2, 0)` |
| `metal_floor_tile (145)` | 2 | `(-2, -1)` |
| `metal_floor_tile (107)` | 3 | `(-2, -2)` |
| `metal_floor_tile (114)` | 4 | `(-2, -3)` |

Keep existing Task 3 devices:

| Device | Size Rank |
|---|---:|
| `server_-_mainframe_model` | 1 |
| `sci-fi_table` | 2 |
| `scifi_-_krf` | 3 |
| `sample_80` | 4 |

Expected Task 3 post-fix state:

- 4 devices.
- 4 slots.
- 0 null device references.
- 0 null slot references.
- Required size ranks are 1 through 4.
- Slot coordinates match nearest grid coordinates.

---

## Phase 4: Fix Push Animation

Current problem:

- The robot's pushing animation does not play when the robot pushes an object.
- The push logic works (the device moves to the target cell), but the robot model stays in Idle/Walk pose during the push.

### Investigation Findings

The robot's AnimatorController is at `Assets/Animation Controllers/Robot/RobotController.controller`.

**What works:**
- `Animator` component assigned on `Robot Variant Variant` ✅
- `RobotController.controller` has states: **Idle**, **Walk**, **Push** ✅
- Controller has parameters: `IsWalking` (Bool), `IsSprinting` (Bool), `IsPushing` (Bool) ✅
- `AnyState → Push` transition configured with condition `IsPushing = true` ✅
- `Push → Idle` transition configured with condition `IsPushing = false` + exit time ✅
- Script `MG3RobotGridMover` properly calls `SetPushingAnimation(true/false)` which sets `IsPushing` bool ✅
- Walking animation works (Idle/Walk states reference valid animation clips) ✅

**What is broken:**

The **Push state** in the AnimatorController references a motion from `Assets/Mini Game 3/Animation/Push (1).fbx` with GUID `e8788e26af1cadd4491f0401b5b60e89`. This FBX file exists (33 MB, contains animation data), but **its import settings do not extract any animation clips**.

Comparison of the three FBX animation files:

| Animation | File | `clipAnimations` | Status |
|---|---|---|---|
| Idle | `Assets/Animations/Robot/Sad Idle With Skin.fbx` | `mixamo.com` (frames 0-167) | ✅ Valid |
| Walk | `Assets/Animations/Robot/Walking without sking.fbx` | `mixamo.com` (frames 0-62) | ✅ Valid |
| Push | `Assets/Mini Game 3/Animation/Push (1).fbx` | **empty** (`[]`) | ❌ Broken |

The Idle and Walk FBX files have a `clipAnimations` entry with internalID `-203655887218126122`. The Push FBX has no clips at all (`clipAnimations: []`), so the AnimatorController's reference `{fileID: -203655887218126122, guid: e8788e26af1cadd4491f0401b5b60e89, type: 3}` is dangling — it points to a clip that Unity never imported.

**Secondary note:** The script's `pushingTriggerParameter = "Push"` references a Trigger parameter named `Push` that does not exist in the controller. However, this is not the blocking issue — `ResolveAnimator()` detects the missing trigger and skips `PlayPushAnimation()` gracefully. The transition works via `IsPushing` bool instead.

### Repair Steps

1. Open `Assets/Mini Game 3/Animation/Push (1).fbx` in the Unity Inspector.
2. In the **Rig** tab, confirm Animation Type is set appropriately (same as Idle/Walk FBX files).
3. In the **Animation** tab, add a clip extraction entry:
   - Name: `mixamo.com` (to match the naming convention of Idle/Walk)
   - Start frame: `0`
   - End frame: (detect from the FBX file, should be set automatically)
4. Apply the import settings.
5. Verify that the AnimatorController's Push state now has a valid motion preview.
6. Test in Play Mode: push an object and confirm the robot plays a push animation.

### Expected Post-Fix State

- `Push (1).fbx` has a non-empty `clipAnimations` entry matching the Idle/Walk pattern.
- AnimatorController Push state has a valid animation clip reference.
- When the robot pushes, the Animator transitions to Push state and plays the animation.
- The push animation holds for at least `minPushAnimationHold = 0.35s` before returning to Idle.

---

## Phase 5: Repair Camera Criticals

Current confirmed camera state:

- Main Camera live FOV is `20`.
- `MG3CameraFovLimiter` clamps Main Camera to `minFov = 10`, `maxFov = 20`.
- `CM vcam1` wants FOV `60`, but the limiter overrides the output.
- `Robot Top-Down Camera` exists but is disabled.

Recommended first camera fix:

1. Set `MG3CameraFovLimiter.minFov = 40`.
2. Set `MG3CameraFovLimiter.maxFov = 80`.
3. Keep `MG3CameraFovLimiter.enforceEveryFrame = true` if a safe playable clamp is desired.
4. Enable `Robot Top-Down Camera`.
5. Set `Robot Top-Down Camera` Cinemachine priority above `CM vcam1`, for example `10`.
6. Keep `CM vcam1` available as a lower-priority fallback.

Alternative if top-down camera framing looks wrong:

1. Leave `Robot Top-Down Camera` disabled.
2. Fix only `MG3CameraFovLimiter` to `40..80`.
3. Use `CM vcam1` as the active gameplay camera.

Expected camera post-fix state:

- Main Camera effective FOV is no longer clamped to `20`.
- Live FOV is around `40..60` depending on the active virtual camera.
- CinemachineBrain has a live virtual camera.
- Game View shows the lab/gameplay area instead of a black or over-zoomed view.

---

## Phase 6: Validation Checklist

After scene-data changes, run a read-only validation pass that checks all of the following:

- Every `MG3TaskDefinition.devices` array has no null entries.
- Every `MG3TaskDefinition.slots` array has no null entries.
- Every slot coordinate equals `MG3GridManager.TryFindNearestTileCoord(slot.transform.position)`.
- Every task has unique slot coordinates.
- Every task has unique device start coordinates.
- Every ExactPlacement slot has a non-empty `RequiredDeviceId`.
- Every GroupPlacement slot has a non-empty `RequiredGroupId`.
- Every SizeOrdering slot has a meaningful `RequiredSizeRank`.
- `sci-fi_lab` child MeshFilters have non-null `sharedMesh` references.
- `sci-fi_lab` renderer bounds are non-zero.
- `Push (1).fbx` has at least one clip in `clipAnimations`.
- AnimatorController Push state has a valid motion reference.
- Main Camera effective FOV is no longer `20`.
- CinemachineBrain has a live virtual camera.

Expected validation result:

| Task | Devices | Slots | Null Devices | Null Slots |
|---|---:|---:|---:|---:|
| Task 1 | 4 | 4 | 0 | 0 |
| Task 2 | 7 | 7 | 0 | 0 |
| Task 3 | 4 | 4 | 0 | 0 |

Expected global result:

- All slot coordinates synced.
- Lab geometry visible in Scene View.
- No duplicate slot coordinates inside the same task.
- No duplicate device start coordinates inside the same task.
- Live camera FOV is around `40..60`, not `20`.

---

## Phase 7: Play Mode Verification

Manual playtest checklist:

1. Enter Play Mode.
2. Confirm the Game View is no longer black or zoomed in.
3. Confirm the lab environment is visible in Scene View and Game View.
4. Click Begin/Start.
5. Confirm Task 1 panel appears.
6. Confirm right-click movement works.
7. Confirm push with `E` works.
8. Solve or force-place Task 1 devices and confirm Task 2 starts.
9. Confirm Task 2 no longer completes instantly or gets stuck from null data.
10. Solve or force-place Task 2 devices and confirm Task 3 starts.
11. Confirm Task 3 validates size order.
12. Confirm the result screen appears after Task 3.

---

## Phase 8: If Validation Still Fails

Only investigate scripts after scene references and camera setup are fixed.

Potential script-level improvements if needed:

- Add `MG3TaskDefinition` editor validation that warns on null devices or slots.
- Add a context menu action: `Sync Slot Coordinates From Grid`.
- Add a context menu action: `Validate Task Definition`.
- Improve `MG3TaskValidator` logs to report exactly which slot failed and why.
- Prevent null slots from silently making a task impossible.

These are not first-pass fixes. The current blockers are scene-data problems.

---

## Relevant Files

| File | Purpose |
|---|---|---|
| `Assets/Scenes/Aya/MiniGame 3.unity` | Main scene containing broken task references, stale slot coordinates, and camera setup. |
| `Documentation/MiniGame3_Report.md` | Existing diagnostic report with broader issue inventory. |
| `Assets/Scripts/MiniGames/MiniGame3/MG3TaskDefinition.cs` | Runtime task-definition data type. Do not edit first. |
| `Assets/Scripts/MiniGames/MiniGame3/MG3TargetSlot.cs` | Target slot data type. Do not edit first. |
| `Assets/Scripts/MiniGames/MiniGame3/MG3PushableDevice.cs` | Device data type. Do not edit first. |
| `Assets/Scripts/MiniGames/MiniGame3/MG3TaskValidator.cs` | Task validation logic. Do not edit first. |
| `Assets/Scripts/MiniGames/MiniGame3/MG3CameraFovLimiter.cs` | FOV clamp behavior. Do not edit first unless scene values are insufficient. |
| `Assets/Mini Game 3/Animation/Push (1).fbx` | Push animation FBX file. Import settings missing clip extraction. |
| `Assets/Animation Controllers/Robot/RobotController.controller` | Robot AnimatorController. Push state references non-existent clip. |
| `Assets/Scripts/MiniGames/MiniGame3/MG3RobotGridMover.cs` | Robot movement script. Contains animation trigger/bool logic. |

---

## Summary

The main recovery path is to repair serialized scene data, not code:

1. Fix Task 1's missing 4th device reference and stale slot coordinates.
2. Restore `sci-fi_lab` mesh import/references so lab geometry is visible in Scene View.
3. Fix Task 2's device/slot arrays, preferably as a 7-device/7-slot puzzle using existing objects.
4. Fix Task 3's missing slot references and stale coordinates.
5. Fix `Push (1).fbx` import settings so the robot's push animation plays.
6. Fix the camera clamp and activate the intended top-down gameplay camera.
7. Validate lab meshes, push animation, null references, coordinates, requirements, and camera state.
8. Playtest the full Task 1 -> Task 2 -> Task 3 flow.
