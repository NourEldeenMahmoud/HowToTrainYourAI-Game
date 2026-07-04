# MiniGame 3 Session Handoff - 2026-07-04

**Scene:** `Assets/Scenes/Aya/MiniGame 3.unity`  
**Primary script folder:** `Assets/Scripts/MiniGames/MiniGame3/`  
**Purpose:** Record the current state, fixes, design decisions, and follow-up notes from the MG3 debugging/iteration session.

---

## High-Level Current State

MiniGame 3 is now playable through the recent device, pathing, camera, and movement improvements made during this session.

Current intended behavior:

- Task devices push correctly, including Task 3 devices.
- Task 3 devices preserve their authored visual offset while staying grid-authoritative.
- Future task devices are visible and block movement, but cannot be pushed until their task starts.
- The camera is intentionally kept as an angled follow camera from behind, not pure top-down.
- A selected-destination marker appears when a valid move destination is chosen.
- The destination marker disappears when the robot reaches the destination.
- Clicking a new destination while the robot is moving redirects the robot after it finishes its current tile step.

---

## Important Design Decisions

### Grid System Was Not Changed

The grid system itself was intentionally left alone.

Reason:

- Changing `MG3GridManager` grid math risked breaking other MG3 behavior.
- Device and movement issues were fixed at the device/movement layer instead.

### Device Root Versus Visual Child

For pushable devices:

- The root object with `MG3PushableDevice` is treated as the logical grid object.
- Visual centering should be fixed by moving the visual child/model, not the root.
- Colliders may be adjusted if needed, but root position should not be moved casually.

Safe prefab/scene structure:

```text
Device Root
  MG3PushableDevice
  Collider / gameplay logic
  Visual Model Child
```

If a device looks off-center:

- Move the child visual transform, for example `RootNode` or the imported `.fbx` child.
- Avoid moving the selected root object if it has `MG3PushableDevice`.
- If the collider no longer matches the visual, adjust `Box Collider > Center` and `Size`.

### Future Task Devices Should Block

Chosen behavior:

- Devices from future tasks remain visible.
- They block robot pathfinding before their task starts.
- They are not pushable before their task starts.
- They are not permanently locked or visually colored early.

This was implemented by registering future devices as `LockedPushable` in the runtime grid occupancy map only.

### Redirect Movement Uses Finish-Current-Step

Chosen redirect behavior:

- If the robot is moving and the player clicks another valid tile, the marker updates immediately.
- The robot finishes the tile step it is currently travelling to.
- Then it recalculates and follows a new path from that tile.

Reason:

- This avoids mid-cell snapping.
- This keeps grid occupancy stable.
- It is safer than instantly cancelling movement while the robot is visually between cells.

---

## Fix 1: Task 3 Device Push/Grid Desync

### Problem

Task 3 devices were visible in one place but registered logically in another place.

Observed diagnostic values before the fix:

```text
planarDelta = 0.717 to 1.439
```

Symptoms:

- Task 3 devices did not push reliably.
- Push logic could not consistently find the device at the visual cell.

### Cause

`MG3PushableDevice.ResetToTaskStart()` was using saved world position in a way that could desync the transform from the grid coordinate.

### Fix

`MG3PushableDevice.ResetToTaskStart()` now uses the grid coordinate as authoritative when the device is configured not to use authored world position.

Relevant file:

```text
Assets/Scripts/MiniGames/MiniGame3/MG3PushableDevice.cs
```

Important methods/fields:

- `ResetToTaskStart()`
- `SetCurrentCoordinate(...)`
- `GetWorldPositionForCoordinate(...)`
- `startingGridWorldOffset`

### Verification

After the fix:

```text
planarDelta = 0
```

The user confirmed Task 3 devices started pushing properly.

---

## Fix 2: Preserve Authored Visual Offset For Task 3 Devices

### Problem

After the push/grid fix, some Task 3 devices shifted visually and looked off-center on their tiles.

### Cause

Some imported device prefabs have roots/pivots that are not visually centered. Pure grid-center root placement removed their authored visual offset.

### Fix

Added offset-preservation logic to `MG3PushableDevice`:

```csharp
private Vector3 startingGridWorldOffset;
```

New behavior:

- Grid coordinate remains authoritative.
- Visual/root authored offset is preserved where configured.
- Push end positions use the same offset-aware world coordinate.

Relevant files:

```text
Assets/Scripts/MiniGames/MiniGame3/MG3PushableDevice.cs
Assets/Scripts/MiniGames/MiniGame3/MG3PushController.cs
```

Important method:

```csharp
public Vector3 GetWorldPositionForCoordinate(Vector2Int coord)
```

`MG3PushController` now uses this method for push destination placement instead of raw grid center placement.

### Diagnostic

Temporary Task 3 alignment diagnostics were added in:

```text
Assets/Scripts/MiniGames/MiniGame3/MiniGame3Manager.cs
```

Diagnostic output includes:

```text
actual
expectedVisual
gridCenter
visualDelta
```

Expected good result:

```text
visualDelta = 0
```

Note:

- `gridCenter` may differ from `expectedVisual` intentionally if authored offset is being preserved.

---

## Fix 3: Future Task Devices Block Movement

### Problem

Before this fix:

- During Task 1, the robot could move through Task 2 and Task 3 devices.
- During Task 2, the robot could move through Task 3 devices.

### Cause

`MiniGame3Manager.ResetTaskState()` clears all runtime occupancy:

```csharp
gridManager.ClearAllRuntimeOccupancy();
```

Then it re-registered only:

- current task devices
- previously locked/completed devices
- robot

Future task devices were visible but absent from the runtime occupancy map, so pathfinding treated their cells as walkable.

### Fix

Added future-device blocker registration in:

```text
Assets/Scripts/MiniGames/MiniGame3/MiniGame3Manager.cs
```

New helper:

```csharp
private void RegisterFutureTaskDevicesAsBlockers()
```

Behavior:

```text
Task 1:
  Task 1 devices = Pushable
  Task 2/3 devices = LockedPushable blockers, not actually locked

Task 2:
  Task 1 solved devices = locked blockers
  Task 2 devices = Pushable
  Task 3 devices = LockedPushable blockers, not actually locked
```

### Revert Note

This change is isolated.

To revert future-device blocking:

- Remove the call to `RegisterFutureTaskDevicesAsBlockers()` from `ResetTaskState()`.
- Remove the helper method if no longer used.

---

## Camera Work

### Current Camera Setup

The scene uses Cinemachine.

Main objects:

```text
Main Camera
  Camera
  CinemachineBrain
  MG3CameraFovLimiter

MG3_Root/MG3_Cameras/CM vcam1
  CinemachineCamera
  CinemachineFollow
  CinemachineRotationComposer
```

The live virtual camera is:

```text
CM vcam1
```

It follows and looks at the robot.

### Top-Down Experiment

A pure top-down/orthographic direction was considered but rejected because it made the robot look like only a head.

The user preferred an angled camera from behind.

### Recommended Camera Tuning Range

For an angled behind-the-robot camera:

```text
CM vcam1 > Cinemachine Follow > Follow Offset
  X: 0
  Y: 12-16
  Z: -8 to -12
```

Perspective lens recommended:

```text
Orthographic: false
Field Of View: 45-60
```

Good starting preset:

```text
Follow Offset: X 0, Y 14, Z -9
Field Of View: 50
Target Offset: X 0, Y 1.2, Z 0
```

Avoid if the goal is to see the robot body:

```text
Follow Offset: X 0, Y 20, Z 0
```

### FOV Limiter Note

`Main Camera` has:

```text
MG3CameraFovLimiter
```

The scene instance had values:

```text
minFov: 10
maxFov: 20
enforceEveryFrame: true
```

If perspective FOV changes do not appear to work, this limiter may be clamping the camera. For wider perspective view, tune it toward:

```text
minFov: 35
maxFov: 60
```

---

## Device Visual Centering Notes

### Safe Way To Center A Device

If a pushable device looks visually off-center:

1. Select the object with `MG3PushableDevice`.
2. Do not move this root object's `Transform > Position` unless intentionally moving the device to another grid cell.
3. Expand the hierarchy.
4. Move the visual child, for example:

```text
large_sci-fi_vat
  b73c6e...fbx
    RootNode
      SM_vat_large_low
      vatlarge_buffer_low
      vatlarge_buffer_low1
      vatlarge_buffer_low2
```

Recommended child to move:

```text
RootNode
```

If `RootNode` does not move all visible parts, move the imported `.fbx` child.

Use local `X/Z` movement for centering. Be careful with `Y` to avoid floating/sinking.

### Collider Note

If visual centering causes the green collider to no longer wrap the visual correctly:

- Select the device root.
- Adjust `Box Collider > Center` and `Size`.
- Do not move the root transform to fix collider/visual offset.

---

## Selected Destination Marker

### Goal

Add a marker on the selected movement destination, matching the look of MiniGame 2's valid-move markers.

### MG2 Reference

MiniGame 2 uses:

```text
Assets/Scripts/MiniGames/MiniGame2/UI/MG2MovableTileHighlighter.cs
```

That script creates:

- cyan `LineRenderer` ring
- center `Quad` dot
- pulse animation
- slow spin
- transparent/emissive material
- surface snapping above the tile

### MG3 Implementation

New script:

```text
Assets/Scripts/MiniGames/MiniGame3/MG3SelectedMoveMarker.cs
```

Purpose:

- Shows one selected destination marker.
- Uses the same procedural visual style as MG2.
- Does not depend on MG2 classes.

Behavior:

- Shows when `MG3RobotGridMover` accepts a valid destination.
- Updates when a redirect destination is accepted.
- Hides when destination is rejected.
- Hides when the robot reaches the destination.

### Marker Events

`MG3RobotGridMover` now exposes:

```csharp
public event Action<Vector2Int> DestinationAccepted;
```

`MG3SelectedMoveMarker` listens to:

```text
DestinationAccepted
DestinationRejected
DestinationReached
```

### Inspector Tweaking

`MG3SelectedMoveMarker` was added directly to:

```text
MG3_Manager
```

So it can be tuned in the Inspector like MiniGame 2.

Tweakable fields:

```text
Marker Scale Relative To Tile
Ring Thickness Relative To Tile
Center Dot Scale Relative
Ring Color
Dot Color
Use Transparent Material
Marker Layer
Y Offset
Min Height Above Grid
Surface Snap Ray Height
Surface Snap Mask
Animate Pulse
Pulse Speed
Pulse Scale Amplitude
Idle Spin Degrees Per Second
```

Scene saved after adding the component:

```text
Assets/Scenes/Aya/MiniGame 3.unity
```

### Bootstrap

`MG3SelectedMoveMarker.cs` includes a bootstrap class:

```csharp
MG3SelectedMoveMarkerBootstrap
```

It auto-adds the component at runtime if none exists. Since the component is now in the scene, the bootstrap should detect it and avoid creating a duplicate.

---

## Redirect Movement

### Goal

Allow the player to choose a different destination while the robot is already moving.

### Implemented Behavior

Option implemented:

```text
Finish-current-step redirect
```

Flow:

1. Player clicks destination A.
2. Robot starts moving toward A.
3. Player clicks destination B before the robot arrives.
4. Destination B is validated from the tile the robot is currently moving toward.
5. Marker updates to B immediately.
6. Robot finishes its current tile step.
7. Robot recalculates path from that tile to B.
8. Robot follows the new path.

### Why Not Immediate Cancel/Snap

Immediate cancel was discussed but not implemented.

Risk of immediate cancel:

- The robot may be visually between two cells.
- Grid occupancy may already say the robot occupies the next cell.
- `CurrentGridCoord` may still represent the previous cell until arrival.
- Snapping can look abrupt and can create path/occupancy bugs if done incorrectly.

The current finish-step implementation avoids those risks.

### Relevant Fields/Methods

File:

```text
Assets/Scripts/MiniGames/MiniGame3/MG3RobotGridMover.cs
```

New serialized toggle:

```csharp
[SerializeField] private bool allowRedirectWhileMoving = true;
```

New internal state:

```csharp
private bool hasPendingRedirect;
private Vector2Int pendingRedirectDestination;
private Vector2Int activeStepDestination;
```

New helper methods:

```csharp
private bool TryQueueRedirect(Vector2Int destination)
private bool TryApplyPendingRedirect(ref List<Vector2Int> path)
private void ClearPendingRedirect()
```

### Tuning/Revert Note

To disable redirect behavior without code revert:

- Uncheck `Allow Redirect While Moving` on the `MG3RobotGridMover` component.

To revert the code change:

- Remove `allowRedirectWhileMoving`.
- Remove pending redirect fields.
- Remove `TryQueueRedirect`, `TryApplyPendingRedirect`, and `ClearPendingRedirect`.
- Restore `TryRequestMove()` so moving state returns `false`.
- Restore `ProcessClick()` so it ignores clicks while moving.

---

## Files Changed During This Work

Main gameplay/script changes:

```text
Assets/Scripts/MiniGames/MiniGame3/MG3PushableDevice.cs
Assets/Scripts/MiniGames/MiniGame3/MG3PushController.cs
Assets/Scripts/MiniGames/MiniGame3/MG3RobotGridMover.cs
Assets/Scripts/MiniGames/MiniGame3/MiniGame3Manager.cs
Assets/Scripts/MiniGames/MiniGame3/MG3SelectedMoveMarker.cs
Assets/Scripts/MiniGames/MiniGame3/MG3SelectedMoveMarker.cs.meta
```

Scene changes:

```text
Assets/Scenes/Aya/MiniGame 3.unity
```

Known workspace changes also observed from Unity/editor activity:

```text
Assets/Mini Game 3/Prefabs/UI prefabs/Task MiniGame 3 Canvas.prefab
Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Anton SDF.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Bangers SDF.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset
Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Oswald Bold SDF.asset
```

These existing Unity asset changes were not intentionally reverted.

---

## Validation Performed

Unity script validation passed for the changed gameplay scripts:

```text
MG3PushableDevice.cs
MG3PushController.cs
MiniGame3Manager.cs
MG3RobotGridMover.cs
MG3SelectedMoveMarker.cs
```

Unity compilation was requested after changes and completed successfully.

Console state after the latest marker/redirect changes:

```text
0 errors
0 warnings
```

Earlier unrelated warnings observed:

- obsolete `FindFirstObjectByType<T>()` warnings in other scripts
- analyzer warning: `String concatenation in Update() can cause garbage collection issues`

Earlier unrelated runtime issue observed:

```text
Animator is not playing an AnimatorController
```

This was not addressed in this session because it did not appear related to MG3 device positioning, marker, future blockers, or redirect movement.

---

## Current Follow-Up Testing Checklist

Use this checklist next time before continuing new features.

1. Start MiniGame 3.
2. In Task 1, verify Task 2 and Task 3 visible devices block robot movement.
3. In Task 1, verify Task 1 devices remain pushable.
4. Click a valid movement destination and verify the cyan marker appears.
5. Verify marker disappears when robot reaches the destination.
6. While robot is moving, click a different valid tile.
7. Verify marker updates immediately.
8. Verify robot finishes the current tile step, then redirects to the new destination.
9. Skip or complete Task 1 and start Task 2.
10. Verify Task 3 devices block movement during Task 2.
11. Verify Task 2 devices are pushable during Task 2.
12. Start Task 3 and verify Task 3 devices are pushable and visually centered.
13. Check console for new errors/warnings after the full run.

---

## Current Known Risks / Watch Items

### Temporary Task 3 Alignment Diagnostic

`MiniGame3Manager.cs` still contains Task 3 alignment diagnostics.

It is useful while verifying Task 3 visual/grid alignment.

When stable, consider removing or gating it behind a debug flag to reduce log noise.

### Redirect Edge Cases

Redirect is intentionally conservative, but test these cases:

- redirect to a blocked future-device tile
- redirect to the current/next tile
- redirect immediately before arrival
- redirect repeatedly during one move
- redirect while a task reset or push starts

### Future Device Blockers

Future devices now block current task movement. This is intended, but it may make a task impossible if future devices overlap required routes.

If this happens, possible alternatives:

- move the future devices
- hide/disable future task groups until their task starts
- revert `RegisterFutureTaskDevicesAsBlockers()`

### Marker Height/Visibility

If marker appears too high, too low, or hidden by the floor:

- tune `Y Offset`
- tune `Min Height Above Grid`
- tune `Surface Snap Mask`

The marker is on `MG3_Manager` as `MG3SelectedMoveMarker`.

---

## Useful Component Locations In Scene

```text
MG3_Manager
  MiniGame3Manager
  MG3DebugHudPlaceholder
  MG3SelectedMoveMarker

MG3_Root/MG3_Cameras/CM vcam1
  CinemachineCamera
  CinemachineFollow
  CinemachineRotationComposer

Main Camera
  Camera
  CinemachineBrain
  MG3CameraFovLimiter

MG3_Grid
  MG3_Manager
  MG3_GridManager
  MG3_Pathfinder / related grid objects

MG3_Tasks
  Task 1 Devices
  Task 2 Devices
  Task 3 Devices
```

---

## Quick Summary For Future Work

If resuming later, remember:

- Do not change grid math unless absolutely necessary.
- Treat `MG3GridManager` occupancy as authoritative for movement and pushing.
- Device visual centering should happen inside visual children, not root transforms.
- Future devices currently block as runtime `LockedPushable` occupants only.
- `MG3SelectedMoveMarker` is tweakable on `MG3_Manager`.
- Redirect is finish-current-step, not instant snap.
- Camera should remain angled from behind unless the design changes again.
