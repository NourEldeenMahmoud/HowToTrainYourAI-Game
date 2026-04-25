---
dg-publish: true
---

# Mini-Game 1 - Control Calibration Flow

This document is the implementation reference for the current Mini-Game 1 flow in the Unity project. It describes the gameplay flow, challenge scoring, result screen behavior, robot stat updates, and post-MG1 robot fault system as currently implemented.

Last updated to match the project after these changes:

- Drift evaluation is track-free.
- Failed MG1 results disable `Apply Improvements`.
- Post-MG1 robot problems are probability-based events.
- Drift, camera, and speed fault settings are centralized on `RobotStabilityApplier`.
- `ControlManager` no longer owns camera fault event logic.

---

## 1. Story Purpose

Mini-Game 1 is the first robot training/calibration sequence. The player finds the robot and tries to use it to access the grandfather message, but the movement system is unstable. The system blocks progress until the player calibrates the robot.

The purpose is twofold:

- Teach the player how robot control works.
- Convert player performance into robot stat changes and future problem probabilities.

The game does not immediately make the robot perfect. Instead, the robot can still experience the same categories of problems after MG1, but the probability depends on how well the player performed.

---

## 2. Runtime Flow

The active flow is managed mostly by `IntroRobotController`, `MiniGame1Manager`, `MiniGame1ResultScreenUI`, and `MG1ToMG2FlowCoordinator`.

### Main Flow

1. Player interacts with the message/robot setup.
2. The system explains that robot movement needs calibration.
3. Player starts MG1 from the UI.
4. `MiniGame1Manager.StartMiniGame()` starts the sequence coroutine.
5. MG1 runs through the configured phases.
6. Each challenge contributes metrics and displayed challenge scores.
7. Final result is evaluated.
8. Result screen appears.
9. If the result is `Fail`, `Apply Improvements` is disabled.
10. If the result is pass tier (`Average`, `Good`, or `Excellent`), `Apply Improvements` is enabled.
11. On Apply, robot stats and fault probabilities are already saved, result screen closes, and story flow continues toward MG2.

### Current Phase Order

```text
FreeMoveInitial
DriftLeft
FreeMoveBetween_DriftLeft_DriftRight
DriftRight
FreeMoveBetween_DriftRight_Camera
CameraAlignment
FreeMoveBetween_Camera_Speed
SpeedConsistency
Completed
```

### Main Manager

`MiniGame1Manager` controls this sequence.

Important fields:

```text
learningProfile
robotStats
trackProgress                 legacy/optional
trackAccuracyTracker          legacy/optional
driftLeft
driftRight
cameraAlignment
speedConsistency
initialFreeMoveSeconds
freeMoveBetweenChallengesSeconds
challengeFlow
enableLogging
```

Important events:

```text
PhaseChanged
MiniGameCompleted
LogMessage
```

`LogMessage` is used by robot POV UI to show terminal-style training logs.

---

## 3. Challenge 1 - Drift Calibration

Script:

```text
Assets/Scripts/MiniGames/MiniGame1/Challenges/DriftChallenge.cs
```

### Current Design

The drift challenge is now independent from track objects. It does not require `MG1_Track`, `TrackProgress`, or waypoints to score drift.

The challenge injects a yaw drift using:

```csharp
faultState.faultsEnabled = true;
faultState.yawDriftDeg = driftAngleDeg;
```

The player must counter-steer against the injected drift.

### Important Fields

```text
robotTransform
robotMovement
faultState
driftAngleDeg
challengeDurationSeconds
correctionStartThresholdDeg
stableThresholdDeg
```

`robotMovement` is used to read `RobotMovement.MoveInput`. If it is not assigned, the challenge tries to find it from the robot transform, parents, children, or scene.

### Evaluation Logic

When player input is available, drift scoring uses the input angle:

```text
inputAngleDeg = atan2(moveInput.x, moveInput.y)
stabilizedError = abs(DeltaAngle(0, inputAngleDeg + driftAngleDeg))
```

Examples:

```text
driftAngleDeg = +30
holding forward -> inputAngle 0, error 30
counter-steer right/left depending on axis -> inputAngle about -30, error near 0

driftAngleDeg = -30
holding forward -> inputAngle 0, error 30
counter-steer opposite direction -> inputAngle about +30, error near 0
```

If input is not available but movement is detected, the challenge falls back to comparing actual movement direction against the expected no-drift direction.

### Response Time

Response time is recorded when:

```text
stabilizedError < correctionStartThresholdDeg
```

If the player never corrects, response time defaults to the full challenge duration.

### Drift Score Formula

Implemented in `MiniGame1ScoringEngine.ScoreDrift()`:

```text
DriftScore = 55% responseScore + 45% errorScore
```

Where:

```text
responseScore = ResponseTimeScore(profile, responseTimeSeconds)
errorScore = clamp01(1 - averageErrorDeg / correctionWorstErrorDeg) * 100
```

The displayed drift score is the average of the left and right drift challenge scores when both exist.

---

## 4. Challenge 2 - Camera Alignment

Script:

```text
Assets/Scripts/MiniGames/MiniGame1/Challenges/CameraAlignmentChallenge.cs
```

### Current Design

The camera alignment challenge temporarily offsets camera pitch. The player must return the camera to the target pitch and keep it steady.

### Important Fields

```text
cameraTransform
orbitalFollow
cameraPitchPivot
injectedPitchOffsetDeg
challengeDurationSeconds
alignmentThresholdDeg
```

### Measurement

The challenge measures pitch error each frame:

```text
pitch = NormalizePitch(cameraTransform.eulerAngles.x)
absErr = abs(DeltaAngle(pitch, targetPitchDeg))
```

Response time is recorded when:

```text
absErr <= alignmentThresholdDeg
```

### Camera Score Formula

Implemented in `MiniGame1ScoringEngine.ScoreCamera()`:

```text
CameraScore = 50% responseScore + 50% alignmentScore
```

Where:

```text
alignmentScore = clamp01(1 - averageErrorDeg / cameraWorstErrorDeg) * 100
```

---

## 5. Challenge 3 - Speed Consistency

Script:

```text
Assets/Scripts/MiniGames/MiniGame1/Challenges/SpeedConsistencyChallenge.cs
```

### Current Design

The speed challenge injects temporary speed wobble only during MG1. This is not the same as post-MG1 speed faults.

During the challenge:

```csharp
faultState.speedWobbleAmplitude = injectedSpeedWobble;
```

At the end:

```csharp
faultState.speedWobbleAmplitude = 0f;
```

### Important Fields

```text
robotTransform
faultState
targetSpeed
sampleWindowSeconds
sampleWarmupSeconds
sampleIntervalSeconds
injectedSpeedWobble
```

### Measurement

The challenge measures robot distance traveled over sample intervals and calculates speed standard deviation using Welford variance.

Warmup prevents acceleration from unfairly lowering the score:

```text
sampleWarmupSeconds
```

### Speed Score Formula

Implemented in `MiniGame1ScoringEngine.ScoreSpeed()`:

```text
target = max(0.01, metrics.averageSpeed > 0.01 ? metrics.averageSpeed : metrics.speedTarget)
worst = target * speedWorstStdDevTargetRatio
SpeedScore = clamp01(1 - speedStdDev / worst) * 100
```

---

## 6. Final Score

The project currently uses displayed challenge scores for MG1 final score by default.

Controlled by:

```text
MiniGame1LearningProfileSO.useDisplayedChallengeScoresForFinal = true
```

Default challenge weights:

```text
Drift = 0.40
CameraAlignment = 0.25
SpeedConsistency = 0.35
```

Formula:

```text
FinalScore = DriftScore * 0.40 + CameraScore * 0.25 + SpeedScore * 0.35
```

Legacy metric scoring still exists:

```text
PathAccuracy
CorrectionAccuracy
ResponseTime
SpeedConsistency
TargetAlignment
```

But it is only used if the profile disables displayed challenge scores for final scoring.

---

## 7. Tier Rules

Default tiers are defined in `MiniGame1LearningProfileSO`.

```text
Excellent >= 90
Good >= 70
Average >= 50
Fail < passScore, default 50
```

Pass/fail is controlled by:

```text
passScore
```

If final score is below `passScore`, tier is `Fail`.

---

## 8. Result Screen Behavior

Script:

```text
Assets/Scripts/MiniGames/MiniGame1/UI/MiniGame1ResultScreenUI.cs
```

The result screen displays:

- Final score.
- Tier.
- Drift score.
- Camera score.
- Speed score.
- Progress bars implemented as read-only `Scrollbar` components.
- `Apply Improvements` button.
- `Recalibrate Movement` button.

### Fail Gating

If:

```text
result.tier == MiniGameTier.Fail
```

Then:

```text
Apply Improvements.interactable = false
```

The click handler also has a defensive guard:

```text
if (!canApplyImprovements) return;
```

So even code-triggered Apply clicks cannot continue after a failed result.

### Retry Behavior

`Recalibrate Movement` calls:

```text
miniGame1Manager.StartMiniGame()
```

It hides the result screen, unlocks input, restores robot POV if needed, and reruns MG1.

---

## 9. Robot Stat Update After MG1

Script:

```text
Assets/Scripts/MiniGames/MiniGame1/Update/MiniGame1RobotStatUpdater.cs
```

At MG1 completion, `MiniGame1Manager` does:

```csharp
LastResult = MiniGame1ScoringEngine.Evaluate(learningProfile, evaluationInput);
MiniGame1RobotStatUpdater.ApplyUpdateOnce(learningProfile, robotStats, LastResult);
MiniGameCompleted?.Invoke(LastResult);
```

### Fail Results

Fail results do not update robot stats:

```csharp
if (eval.tier == MiniGameTier.Fail)
{
    return;
}
```

### Core Stat Deltas

If the player passes, tier deltas are applied:

| Tier | Stability | Path Accuracy | Input Responsiveness |
|------|-----------|---------------|----------------------|
| Excellent | +0.15 | +0.15 | +0.10 |
| Good | +0.10 | +0.10 | +0.07 |
| Average | +0.05 | +0.05 | +0.03 |
| Fail | +0.00 | +0.00 | +0.00 |

All values are clamped to `0..1`.

### Fault Probability Update

Each challenge score is converted to a probability using curves in `MiniGame1LearningProfileSO`:

```text
driftScoreToErrorRate
cameraScoreToErrorRate
speedScoreToErrorRate
```

Default curve:

```text
Score 0   -> probability 0.95
Score 50  -> probability 0.50
Score 100 -> probability 0.05
```

Saved into `RobotStatsSO`:

```text
driftErrorRate
cameraErrorRate
speedErrorRate
hasSavedCalibrationResult = true
```

Despite the historical field name `ErrorRate`, these are now used as per-roll fault probabilities.

---

## 10. Post-MG1 Fault Event System

Script:

```text
Assets/Scripts/MiniGames/MiniGame1/Integration/RobotStabilityApplier.cs
```

All three post-MG1 problem settings are now on the Robot object through `RobotStabilityApplier`.

### Shared Timing

The same timing range is used for drift, camera, and speed fault rolls:

```text
Min Fault Check Interval Seconds
Max Fault Check Interval Seconds
```

Every random interval in this range, the related fault rolls against its probability.

Example:

```text
driftErrorRate = 0.20
Every random 6-10 seconds, drift has a 20% chance to trigger.
```

### Drift Fault Event

Probability:

```text
robotStats.driftErrorRate
```

Settings:

```text
Drift Fault Yaw Deg
Drift Fault Duration Seconds
```

Behavior:

- Temporarily applies yaw drift to robot movement.
- Randomly chooses positive or negative direction.
- Returns to zero after the duration.

Consumed by `RobotMovement`:

```csharp
float persistentYawDrift = stabilityApplier.GetYawDriftDegrees();
```

### Camera Fault Event

Probability:

```text
robotStats.cameraErrorRate
```

Settings:

```text
Robot Camera
Camera Fault Pitch Offset Deg
Camera Fault Duration Seconds
```

Behavior:

- Temporarily offsets robot camera pitch.
- Randomly chooses up/down direction.
- Restores the offset safely after the event ends or when faults become unavailable.

Camera references:

- `RobotStabilityApplier.robotCamera` can be assigned directly.
- If not assigned, it can read `ControlManager.RobotCamera`.
- It requires `CinemachineOrbitalFollow` on the robot camera.

### Speed Fault Event

Probability:

```text
robotStats.speedErrorRate
```

Settings:

```text
Sprint Block Duration Seconds
```

Behavior:

- Cancels sprint.
- Blocks sprint for the configured duration.
- Player must press sprint again after the block ends.

Implemented through:

```text
RobotMovement.CancelSprintFromFault(float blockDurationSeconds)
```

---

## 11. Fault Safety Rules

Post-MG1 persistent faults only run when:

```text
robotStats.hasSavedCalibrationResult == true
MG1 is not running
MG2 is not running
```

Camera fault also requires robot control to be active, unless no `ControlManager` exists:

```text
!controlManager.IsPlayerControlActive
!controlManager.IsInputLocked
```

This prevents conflicts with:

- Active MG1 challenge fault injection.
- Active MG2 gameplay.
- Result screens and input locks.
- Player-control camera state.

---

## 12. MG1-Only Temporary Faults vs Post-MG1 Persistent Faults

There are two fault systems and they must not be confused.

### MG1 Temporary Faults

Script:

```text
MiniGame1FaultState.cs
```

Used only during MG1 challenges:

```text
yawDriftDeg
speedWobbleAmplitude
speedWobbleHz
```

Purpose:

- Inject drift during drift challenge.
- Inject speed wobble during speed challenge.

### Post-MG1 Persistent Fault Events

Script:

```text
RobotStabilityApplier.cs
```

Used after MG1 passes:

```text
drift fault event
camera pitch fault event
sprint cancel/block fault event
```

Purpose:

- Make the robot occasionally experience the same problem categories after calibration.
- Problem probability depends on MG1 challenge performance.

---

## 13. MG1-to-MG2 Story Flow

Script:

```text
Assets/Scripts/MiniGames/MiniGame1/Integration/MG1ToMG2FlowCoordinator.cs
```

After a successful MG1 result and Apply click:

1. Result screen closes.
2. `ApplyImprovementsClicked` event fires.
3. `MG1ToMG2FlowCoordinator.BeginAfterApplyImprovements()` starts post-MG1 flow.
4. The grandfather/corrupted message text is prepared.
5. Player is expected to open/read/close the message UI.
6. Objective text updates to storage room direction.
7. Storage door transition becomes available.
8. Player can enter MG2 flow.

---

## 14. Current Code Map

| File | Current Role |
|------|--------------|
| `IntroRobotController.cs` | Intro/message/configure flow and MG1 start button path. |
| `MiniGame1Manager.cs` | MG1 phase coroutine, evaluation input collection, final result, stat update call, completion event. |
| `DriftChallenge.cs` | Track-free drift challenge and counter-steering scoring. |
| `CameraAlignmentChallenge.cs` | Camera pitch injection and alignment scoring. |
| `SpeedConsistencyChallenge.cs` | MG1-only speed wobble injection and speed consistency scoring. |
| `MiniGame1ScoringEngine.cs` | Challenge scores, metric scores, final score, tier selection. |
| `MiniGame1Scoring.cs` | Shared scoring helpers such as response time score. |
| `MiniGame1LearningProfileSO.cs` | Pass score, tier thresholds, score weights, tolerances, score-to-probability curves. |
| `MiniGame1RobotStatUpdater.cs` | Applies tier deltas and post-MG1 fault probabilities. |
| `RobotStatsSO.cs` | Stores core robot stats and post-MG1 fault probabilities. |
| `RobotStabilityApplier.cs` | Centralized post-MG1 drift/camera/sprint event faults. |
| `RobotMovement.cs` | Robot movement, movement drift consumption, sprint cancel/block support. |
| `MiniGame1ResultScreenUI.cs` | Result screen, bar updates, fail-gated Apply, retry behavior. |
| `MiniGame1RobotPovUI.cs` | Robot POV challenge labels, prompts, clock, logs. |
| `MiniGame1FaultState.cs` | Temporary MG1-only drift and speed wobble injection. |
| `MG1ToMG2FlowCoordinator.cs` | After-Apply story/objective transition toward MG2. |

---

## 15. Scene/Inspector Setup Notes

On the `Robot` object:

```text
RobotMovement
MiniGame1FaultState
RobotStabilityApplier
```

Recommended `RobotStabilityApplier` references:

```text
Robot Stats -> RobotStats_Main
Robot Movement -> Robot object RobotMovement component
Robot Camera -> Robot FreeLook/robot Cinemachine camera
```

If `RobotStabilityApplier` is missing, `RobotMovement` can auto-add it at runtime, but adding it manually in the scene is recommended so designers can edit values before pressing Play.

On `ControlManager`:

```text
robotCamera
playerCamera
playerInput
robotInput
switchAction
podUiRoot
```

`ControlManager` no longer owns post-MG1 camera fault settings. It only exposes `RobotCamera` for `RobotStabilityApplier` to resolve automatically.

---

## 16. Known Legacy/Optional Pieces

- `MG1_Track` is no longer needed by drift scoring.
- `TrackProgress` and `TrackAccuracyTracker` still exist as optional/legacy path metric support.
- If `useDisplayedChallengeScoresForFinal` is true, final result does not depend on track path accuracy.
- `MiniGame1FaultState.GetSpeedMultiplier()` is still valid because it is MG1-only speed challenge wobble, not post-MG1 behavior.
