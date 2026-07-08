---
dg-publish: true
---

# Project Progress Reference - How To Train Your AI

This file is the current implementation map for the Unity project. It should be updated whenever major behavior, scene flow, mini-game rules, or script responsibilities change.

| Item | Value |
|------|-------|
| Last Documentation Update | 25 April 2026 |
| Engine | Unity 6 / Unity 6000.x project |
| Main Active Area | Level 1 / MG1 to MG2 flow |
| Current Focus | MG1 calibration, post-MG1 robot behavior, MG2 transition |

---

## 1. Current Project Summary

The project is a story-driven robot training game. The player inherits a house and discovers a robot left by the grandfather. The robot starts unreliable. The player completes mini-games to train specific robot systems.

Current Level 1 vertical slice:

```text
Intro / message interaction
Robot control unlock attempt
Mini-Game 1 movement calibration
Result screen and robot stat update
Post-MG1 robot fault probabilities
Grandfather/corrupted message flow
Storage objective
Mini-Game 2 transition direction
```

---

## 2. Important Scenes

Known scenes in use:

```text
Assets/Scenes/Nour/Nour.unity
Assets/Scenes/Omar/Mini Game 1.unity
Assets/Scenes/Oraby/Second MiniGame.unity
```

`Nour/Nour.unity` is the main development scene seen in recent testing.

`Second MiniGame.unity` is referenced by `MG1ToMG2FlowCoordinator` as the MG2 scene path.

---

## 3. Input System

Current input setup:

```text
Global.inputactions
PlayerMovment.inputactions
RobotMovment.inputactions
```

Main actions:

```text
Tab -> switch player/robot control
WASD -> movement
Shift -> sprint
E -> interact
Mouse -> camera look
```

`ControlManager` owns player/robot control switching and input locking.

---

## 4. Core Runtime Scripts

### `Managers/ControlManager.cs`

Responsibilities:

```text
Switch player/robot control
Enable/disable PlayerInput components
Switch Cinemachine camera priorities
Manage pod UI and player UI visibility
Manage input lock state
Expose RobotCamera for RobotStabilityApplier
Handle message UI input blocking
Update time text
Blend pod post-processing volume
```

Important current note:

```text
ControlManager no longer owns post-MG1 camera fault behavior.
```

It only exposes:

```csharp
public CinemachineCamera RobotCamera => robotCamera;
```

### `Robot/RobotMovment.cs`

Class:

```text
RobotMovement
```

Responsibilities:

```text
Read robot movement input
Read robot sprint input
Move CharacterController
Rotate robot visual
Apply MG1 temporary yaw drift
Apply post-MG1 yaw drift from RobotStabilityApplier
Support sprint cancel/block fault
Expose MoveInput for drift challenge scoring
```

Important API:

```text
MoveInput
IsSprinting
IsSprintBlocked
CancelSprintFromFault(float blockDurationSeconds)
```

### `MiniGames/MiniGame1/Integration/RobotStabilityApplier.cs`

Responsibilities:

```text
Centralized post-MG1 fault event system
Roll drift fault events
Roll camera fault events
Roll speed/sprint fault events
Apply temporary movement yaw drift
Apply temporary camera pitch offset
Call RobotMovement.CancelSprintFromFault()
Auto-resolve RobotStatsSO through MiniGame1Manager
Auto-resolve robot camera through ControlManager if needed
Avoid running faults during MG1 or MG2
Clear camera offset safely when faults are unavailable
```

Inspector settings:

```text
Robot Stats
Robot Movement
Robot Camera
Min Fault Check Interval Seconds
Max Fault Check Interval Seconds
Drift Fault Yaw Deg
Drift Fault Duration Seconds
Sprint Block Duration Seconds
Camera Fault Pitch Offset Deg
Camera Fault Duration Seconds
```

---

## 5. Interaction Scripts

### `SimpleInteractable.cs`

Basic interactable that logs interaction and invokes behavior.

### `MessageInteractable.cs`

Opens message UI and can block player controls while message is active.

### `PlayerMovment/PlayerInteractor.cs`

Raycast-based interaction from player camera. Press `E` to interact.

---

## 6. Mini-Game 1 Current State

MG1 is implemented and consists of:

```text
Free Move
Drift Left
Drift Right
Camera Alignment
Speed Consistency
Result Screen
```

Main script:

```text
MiniGames/MiniGame1/MiniGame1Manager.cs
```

Main challenge scripts:

```text
Challenges/DriftChallenge.cs
Challenges/CameraAlignmentChallenge.cs
Challenges/SpeedConsistencyChallenge.cs
```

Main UI scripts:

```text
UI/MiniGame1RobotPovUI.cs
UI/MiniGame1ResultScreenUI.cs
```

Main scoring scripts:

```text
Scoring/MiniGame1ScoringEngine.cs
Scoring/MiniGame1Scoring.cs
Scoring/MiniGame1Evaluator.cs
```

Main stat update script:

```text
Update/MiniGame1RobotStatUpdater.cs
```

---

## 7. MG1 Drift Challenge Update

Current drift challenge is track-free.

Old behavior:

```text
Compare movement direction against TrackProgress current segment direction
```

Current behavior:

```text
Evaluate counter-steering against injected drift using RobotMovement.MoveInput
```

Core formula:

```text
inputAngleDeg = atan2(moveInput.x, moveInput.y)
stabilizedError = abs(DeltaAngle(0, inputAngleDeg + driftAngleDeg))
```

Fallback exists if input cannot be sampled:

```text
Compare actual movement direction against expected no-drift direction
```

`MG1_Track` is not required for drift scoring anymore.

---

## 8. MG1 Camera Challenge

`CameraAlignmentChallenge` injects pitch offset and evaluates how fast/accurately the player returns camera pitch to target.

Important values:

```text
injectedPitchOffsetDeg
targetPitchDeg
challengeDurationSeconds
alignmentThresholdDeg
cameraWorstErrorDeg in learning profile
```

Score:

```text
50% response time
50% average pitch alignment error
```

---

## 9. MG1 Speed Challenge

`SpeedConsistencyChallenge` uses `MiniGame1FaultState.speedWobbleAmplitude` only during MG1.

Important values:

```text
targetSpeed
sampleWindowSeconds
sampleWarmupSeconds
sampleIntervalSeconds
injectedSpeedWobble
```

Score is based on speed standard deviation.

This is separate from the post-MG1 speed fault event, which cancels/blocks sprint.

---

## 10. MG1 Result Screen

`MiniGame1ResultScreenUI` currently supports:

```text
Final score
Tier text
Drift score
Camera score
Speed score
Progress bars
Apply Improvements
Recalibrate Movement
```

Fail result behavior:

```text
Apply Improvements is disabled
Apply click handler returns defensively
Recalibrate remains available
```

Passed result behavior:

```text
Apply Improvements closes result screen and continues post-MG1 story flow
```

---

## 11. MG1 Robot Stat Update

After MG1 ends, `MiniGame1Manager` evaluates and updates robot stats:

```text
MiniGame1ScoringEngine.Evaluate()
MiniGame1RobotStatUpdater.ApplyUpdateOnce()
```

If tier is `Fail`, updater returns immediately.

If passed, updater changes:

```text
stability
pathAccuracy
inputResponsiveness
driftErrorRate
cameraErrorRate
speedErrorRate
hasSavedCalibrationResult
```

Core stat deltas come from tier.

Fault probabilities come from per-challenge score curves.

---

## 12. Post-MG1 Fault Behavior

All post-MG1 fault event settings now live on `RobotStabilityApplier`.

Faults roll every random interval:

```text
Min Fault Check Interval Seconds -> Max Fault Check Interval Seconds
```

Current post-MG1 faults:

| Fault | Probability | Effect |
|------|-------------|--------|
| Drift | `driftErrorRate` | temporary movement yaw drift |
| Camera | `cameraErrorRate` | temporary robot camera pitch offset |
| Speed | `speedErrorRate` | cancel sprint and block sprint temporarily |

Faults do not run while MG1 or MG2 is running.

Camera faults also require robot control to be active and input not locked.

---

## 13. MG1-to-MG2 Flow

`MG1ToMG2FlowCoordinator` handles the story transition after Apply Improvements.

Current flow:

```text
Apply Improvements clicked
Grandfather/corrupted message prepared
Player opens/closes message
Objective updates to storage room
Storage door can transition to MG2
```

MG2 scene path currently referenced:

```text
Assets/Scenes/Oraby/Second MiniGame.unity
```

---

## 14. Mini-Game 2 Current Direction

MG2 is the audio card / storage room trial.

Current known code includes:

```text
MiniGame2Manager.cs
MiniGame2LearningProfileSO.cs
MiniGame2RobotStatUpdater.cs
MiniGame2ResultScreenUI.cs
GridManager.cs
TileClickMover.cs
FloorTile.cs
MG2CinemachineTopDownInput.cs
```

MG2 updates:

```text
energyEfficiency
pathAccuracy
decisionConfidence
```

Next likely project milestone:

```text
Make MG1 -> message -> storage objective -> MG2 start fully reliable, then polish MG2 mechanics/scoring/UI.
```

---

## 15. UI Notes

Robot POV UI:

```text
MiniGame1RobotPovUI
```

It displays:

```text
challenge labels
prompt/error text
clock
logs
```

TextMeshPro overflow must be configured in the Inspector. If text should not draw outside borders, avoid TMP Overflow mode and use truncation/masking/scrolling setup.

---

## 16. Git / Unity Merge

The project includes:

```text
.gitattributes
setup-git.ps1
```

Purpose:

```text
Use UnityYAMLMerge for Unity scene/prefab/asset merge conflicts.
```

Each teammate should run:

```powershell
.\setup-git.ps1
```

once per machine.

---

## 17. Known Legacy Items

- `TrackProgress`, `TrackAccuracyTracker`, and `MG1_Track` still exist but are not required for drift scoring.
- Raw metric scoring still exists but MG1 defaults to displayed challenge scores.
- `MiniGame1FaultState.GetSpeedMultiplier()` still exists for MG1-only speed challenge injection.
- `RobotStabilityApplier` should be manually added to the Robot in the scene for inspector tuning, although `RobotMovement` can auto-add it at runtime.
