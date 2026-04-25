---
share_link: https://share.note.sx/uuigjbxs#WdaoQeS9CObnXOCSL2x4r8LGyIXeSa6Xb5MxVInWRf8
share_updated: 2026-04-12T00:35:33+02:00
dg-publish: true
---

# Mini-Game 1 - Control Calibration

This file describes the design intent of Mini-Game 1 and how it now matches the current implementation.

Mini-Game 1 is the first robot training sequence. It teaches the player how to control the robot and calibrates the three main robot movement problems:

```text
drifting
camera misalignment
speed/sprint instability
```

---

## 1. Story Context

The player finds the robot and tries to access the grandfather message. The robot is not ready. Its movement system is unstable, so the game asks the player to calibrate it before continuing.

The player is not just playing a tutorial. The player is training the robot's future behavior.

The result of MG1 decides:

- whether the story can continue;
- how much the core robot stats improve;
- how likely the robot is to suffer future drift/camera/speed faults.

---

## 2. Start Condition

MG1 starts when the player attempts to progress through the robot/message setup and chooses to configure movement.

Current implementation path:

```text
Message / robot interaction
IntroRobotController
Configure button
MiniGame1Manager.StartMiniGame()
```

---

## 3. Objective

The objective is to calibrate robot movement by completing three challenge categories:

```text
Drift Handling
Camera Alignment
Speed Consistency
```

The player must score at least the pass score, normally `50%`, to apply improvements and continue.

---

## 4. Gameplay Flow

Current implemented flow:

```text
Free Move Initial
Drift Left
Free Move Between
Drift Right
Free Move Between
Camera Alignment
Free Move Between
Speed Consistency
Result Screen
```

The free-move gaps give the player time to return to normal before the next injected problem.

---

## 5. Drift Challenge Design

### Old idea

Originally the drift challenge could depend on a track or corridor direction.

### Current implementation

The drift challenge is now environment-independent and track-free.

It does not need:

```text
MG1_Track
TrackProgress
TrackWaypoint
```

It injects movement yaw drift through `MiniGame1FaultState`:

```text
yawDriftDeg = driftAngleDeg
```

It evaluates whether the player counter-steers the injected drift.

Core scoring idea:

```text
If drift is +30 degrees, the player should input about -30 degrees.
If drift is -30 degrees, the player should input about +30 degrees.
```

The challenge reads `RobotMovement.MoveInput` and calculates:

```text
inputAngleDeg = atan2(moveInput.x, moveInput.y)
stabilizedError = abs(DeltaAngle(0, inputAngleDeg + driftAngleDeg))
```

Holding forward during a 30-degree drift should not produce a perfect result. It should produce about 30 degrees of error.

---

## 6. Camera Alignment Challenge Design

The camera challenge simulates the robot camera being misaligned.

The player must return the camera to the target pitch and keep it stable.

Important values:

```text
injectedPitchOffsetDeg
targetPitchDeg
alignmentThresholdDeg
challengeDurationSeconds
```

Design goal:

- fast correction;
- low average pitch error;
- stable camera after correction.

---

## 7. Speed Consistency Challenge Design

The speed challenge injects speed wobble only during MG1.

The player is evaluated on speed consistency using sampled movement speed.

Important values:

```text
targetSpeed
sampleWindowSeconds
sampleWarmupSeconds
sampleIntervalSeconds
injectedSpeedWobble
```

The score is based on standard deviation. More inconsistent speed gives lower score.

---

## 8. Data Collection

MG1 collects:

```text
drift response time
drift average correction error
camera response time
camera average pitch error
speed standard deviation
speed target
optional/legacy path accuracy
```

The current final score uses challenge scores by default, not legacy path metrics.

---

## 9. Scoring Summary

Current final score weights:

```text
Drift = 40%
Camera = 25%
Speed = 35%
```

Current default tiers:

```text
Excellent = 90-100
Good = 70-89
Average = 50-69
Fail = below 50/pass score
```

---

## 10. Result Rules

When MG1 ends, the result screen appears.

If player fails:

```text
Apply Improvements is disabled
Player must retry/recalibrate
No stats are updated
No fault probabilities are saved from this run
```

If player passes:

```text
Apply Improvements is enabled
Robot stat updates are applied
Fault probabilities are saved
Post-MG1 story flow can continue
```

---

## 11. Gameplay Impact After MG1

MG1 does not simply remove problems forever.

Instead, each challenge controls the future probability of that same type of problem:

| MG1 Challenge | Future Problem |
|--------------|----------------|
| Drift Handling | random drift fault |
| Camera Alignment | random camera pitch fault |
| Speed Consistency | random sprint cancel/block fault |

Example:

```text
Good drift performance -> lower drift probability
Bad camera performance -> higher camera fault probability
Excellent speed performance -> very low sprint fault probability
```

Fault probabilities are stored in `RobotStatsSO`:

```text
driftErrorRate
cameraErrorRate
speedErrorRate
```

---

## 12. Post-MG1 Fault Behavior

All post-MG1 fault settings live on the Robot object's `RobotStabilityApplier` component.

Shared roll timing:

```text
Min Fault Check Interval Seconds
Max Fault Check Interval Seconds
```

Every random interval, the system rolls against each probability.

### Drift Fault

```text
Probability: driftErrorRate
Effect: temporary yaw drift
Settings: Drift Fault Yaw Deg, Drift Fault Duration Seconds
```

### Camera Fault

```text
Probability: cameraErrorRate
Effect: temporary camera pitch offset
Settings: Robot Camera, Camera Fault Pitch Offset Deg, Camera Fault Duration Seconds
```

### Speed Fault

```text
Probability: speedErrorRate
Effect: sprint is cancelled and blocked briefly
Settings: Sprint Block Duration Seconds
```

---

## 13. Design Notes

- MG1 should be environment independent.
- Drift scoring should not depend on scene track objects.
- Player feedback should be shown in robot POV logs.
- Result screen should clearly communicate pass/fail and category scores.
- The robot should feel improved after success but still imperfect if scores are not perfect.
- Future robot problems should be the same categories trained in MG1.
- All tuning values for post-MG1 faults should be exposed on `RobotStabilityApplier`.

---

## 14. Outcome

After MG1, the player should understand:

- how the robot moves;
- how to correct drift;
- how to fix camera alignment;
- how to manage speed consistency;
- that robot learning affects future gameplay.

After passing MG1, the player can apply improvements and proceed toward the storage/audio-card objective and MG2.

---

<!-- opencode:related-start -->
## Related
- [[01 Projects/VrProject/VR Project.md|VrProject Hub]]
- [[01 Projects/01 Projects Index|Projects Index]]
- [[02 Areas/02 Personal/Main/01 Current Areas|Current Areas]]
<!-- opencode:related-end -->
