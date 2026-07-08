# MG2 -> Return To Nour Transition Plan

## Goal

After MG2 success, pressing **Next** should:
1. Make the robot return to the gate using the ideal path (learning payoff).
2. Fade out.
3. Load the main scene `Nour`.
4. Place both player and robot in front of the storage gate in `Nour`.

This flow should be deterministic, readable in code, and safe against missing references.

---

## Current Baseline

- `MiniGame2ResultScreenUI.OnNextClicked()` is currently placeholder-only.
- `MiniGame2Manager` already computes ideal paths and has route references (`gridManager`, `tileClickMover`, `startCoord`).
- `TileClickMover` currently aborts movement when MG2 phase is `Completed`.
- `SceneTransitionFader` already supports fade + scene loading.
- `Nour` scene contains storage door objects (including `Warehouse_Door.B`) and existing MG1 -> MG2 wiring through `DoorInteractable`.

---

## Desired Behavior

### On success

- `Next` is enabled only if MG2 passed.
- When clicked:
  - result UI stops accepting input,
  - robot auto-walks from current tile to gate/start tile using ideal path,
  - optional short hold (0.5-1.0s) when robot reaches gate,
  - screen fades out,
  - scene `Nour` loads,
  - player and robot appear in front of storage gate.

### On fail

- `Next` stays disabled.
- `Retry` remains available.

---

## Technical Design

## 1) Result UI flow (`MiniGame2ResultScreenUI.cs`)

### Changes

- Cache last evaluation result in UI (`lastResult`, `hasResult`).
- `ConfigureNextButtonState()` should combine:
  - inspector toggle (`disableNextButton`),
  - runtime outcome (`lastResult.isSuccess`).
- In `OnMiniGameCompleted(result)`:
  - save `lastResult`,
  - call `ConfigureNextButtonState()` after filling result data.
- In `OnNextClicked()`:
  - guard: return unless `hasResult && lastResult.isSuccess`,
  - disable Retry/Next interactability,
  - call manager method to run post-MG2 sequence (return + transition).

### Why

UI owns progression eligibility; manager owns gameplay sequence execution.

---

## 2) Return demo + exit sequence (`MiniGame2Manager.cs`)

### New serialized settings

- `string returnToMainSceneName = "Nour"`
- `float returnStepTimeoutSeconds = 10f`
- `float holdAfterReturnSeconds = 0.75f`
- `float fadeDurationSeconds = 1f`
- `bool enableReturnDemoLogs = true`

### New public API

- `StartReturnToGateAndExitSequence(...)` (or coroutine-returning equivalent)

### Runtime sequence

1. Validate required references (`gridManager`, `tileClickMover`).
2. Resolve current grid from `tileClickMover.CurrentGridPos`.
3. Resolve return destination as MG2 gate/start coordinate (`startCoord`).
4. Compute path using `gridManager.FindIdealPath(current, startCoord)`.
5. Move robot along path tile-by-tile.
6. Wait for completion (or timeout/fallback).
7. Set one-shot session flag for spawn placement in `Nour`.
8. Fade + load via `SceneTransitionFader.TransitionToScene("Nour", -1, fadeDurationSeconds)`.

### Fallback

If return path is unavailable, log warning and continue fade/load to avoid soft lock.

---

## 3) Allow post-completion demo movement (`TileClickMover.cs`)

### Problem

`ShouldAbortMovementExecution()` currently aborts when phase is `Completed`.

### Change

- Add manager query flag such as `IsPostResultReturnDemoRunning`.
- Abort only when completed and no return demo is active.

### Why

Robot must still move during post-result demonstration without reopening gameplay.

---

## 4) Cross-scene spawn intent (`GameSessionFlowFlags.cs`)

### Add one-shot payload

- bool: `hasPendingMg2ReturnSpawn`
- payload fields:
  - anchor name/id (default `Warehouse_Door.B`),
  - player/robot spawn offsets.
- methods:
  - `RequestMg2ReturnSpawn(...)`
  - `TryConsumeMg2ReturnSpawn(out data)`

### Why

Keeps MG2 scene logic decoupled from `Nour` placement logic.

---

## 5) Spawn placer in Nour (new script)

Suggested file:
`Assets/Scripts/MiniGames/MiniGame2/MG2ReturnToNourSpawner.cs`

### Responsibility

On scene load/start:
1. Check `GameSessionFlowFlags.TryConsumeMg2ReturnSpawn(...)`.
2. If no flag, do nothing.
3. Find anchor transform by configured name (`Warehouse_Door.B`) with fallback list.
4. Find player and robot roots.
5. Place both in front of gate with offsets.
6. Orient both toward gate.
7. Use CharacterController-safe teleport pattern (disable -> move/rotate -> enable).
8. Ensure control state is consistent via `ControlManager` when needed.

### Serialized settings

- anchor names/fallbacks,
- player offset,
- robot offset,
- look-target mode,
- optional debug logs.

---

## Anchor Strategy

Primary anchor in `Nour`:
- `Warehouse_Door.B`

Fallback search order:
1. `Warehouse_Door.F`
2. object name containing `Warehouse_Door`
3. explicit serialized `Transform` override

---

## Edge Cases

- Next clicked multiple times:
  - lock buttons immediately,
  - ignore repeated calls while sequence is running.
- Missing path in MG2:
  - skip demo, continue fade/load.
- Missing anchor in Nour:
  - log warning; keep default scene placement.
- Missing player/robot roots:
  - log warning and skip only unavailable actor placement.
- Timeout during return walk:
  - continue transition to avoid soft lock.

---

## Acceptance Criteria

1. On MG2 fail:
   - `Next` is not interactable,
   - `Retry` works.
2. On MG2 pass:
   - clicking `Next` triggers robot return path to gate/start tile,
   - no score/stat recomputation happens.
3. After return:
   - fade out occurs,
   - scene `Nour` loads.
4. In `Nour`:
   - player and robot appear in front of storage gate anchor.
5. No progression soft lock if references/path are missing.

---

## Implementation Order

1. Update `MiniGame2ResultScreenUI` success-gated Next and click guard.
2. Add manager sequence API in `MiniGame2Manager`.
3. Update `TileClickMover` abort behavior for post-result demo.
4. Extend `GameSessionFlowFlags` with MG2 return spawn request/consume.
5. Add `MG2ReturnToNourSpawner` script.
6. Wire scene object(s) and test in-editor.

---

## Test Checklist

### Functional

- Pass MG2 quickly -> Next -> robot returns -> fade -> Nour spawn correct.
- Pass MG2 with inefficient route -> same behavior.
- Fail MG2 -> Next disabled.
- Retry loop works after fail.

### Robustness

- Break path availability temporarily -> no soft lock (fallback still transitions).
- Rename anchor temporarily -> warning + fallback/default behavior.
- Mash-click Next -> only one transition runs.

### Visual

- Return motion is readable as a learned behavior.
- Fade timing feels smooth.
- Spawn orientation at gate looks intentional.

---

## Out of Scope

- New cinematic camera shots for return demo.
- Dialogue/content changes after returning to Nour.
- MG3 transition logic.

---

## Future Polish

- Add subtitle during return: "Robot applied optimized route.".
- Add camera framing for the return demonstration.
- Replace name-based anchor search with explicit scene references once pipeline stabilizes.
