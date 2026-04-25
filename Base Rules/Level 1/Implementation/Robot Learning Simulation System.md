---
share_link: https://share.note.sx/f6c9fncp#gV0/gmFsqbySHf14js+7lzW3T5DotPcctVe/4U4b5Vg
share_updated: 2026-04-12T00:38:00+02:00
dg-publish: true
---

# Robot Learning Simulation System

This is the current design and implementation rulebook for how mini-games train the robot. The project currently uses a hybrid system:

- Mini-games collect performance data.
- Mini-games evaluate player performance into scores and tiers.
- Passing mini-games update robot stats once at the end.
- MG1 additionally converts challenge scores into future robot fault probabilities.

---

## 1. Core Rule

Robot learning happens at the end of a mini-game, not during the middle of a mini-game.

This prevents confusing behavior like the robot improving while the player is still being tested.

The standard loop is:

```text
Data Collection -> Evaluation -> Result Screen -> Robot Update -> Future Behavior
```

---

## 2. Data Asset

The robot uses one shared ScriptableObject for persistent learning values:

```text
Assets/Scripts/MiniGames/MiniGame1/RobotStatsSO.cs
```

Current fields:

```text
hasSavedCalibrationResult

Core Stats:
stability
pathAccuracy
inputResponsiveness

Post-MiniGame2 Stats:
energyEfficiency
decisionConfidence

Post-MG1 Fault Probabilities:
driftErrorRate
cameraErrorRate
speedErrorRate
```

Important: the field names still say `ErrorRate`, but in the current design they are treated as event probabilities.

Example:

```text
driftErrorRate = 0.20
Every drift fault roll has a 20% chance to trigger.
```

---

## 3. Mini-Game Standard Structure

Every mini-game should define these sections:

```text
Tracked Data
Score Calculation
Pass / Fail Rules
Affected Robot Stats
Post-Game Behavior Impact
Update Rules
UI Result Rules
```

This keeps all mini-games understandable and consistent.

---

## 4. Score System

Every score should be normalized to `0..100`.

Meaning:

```text
0   = very poor
50  = pass/acceptable baseline
100 = excellent
```

Final score can be metric-based or challenge-score based. The current MG1 implementation uses challenge-score based final scoring.

---

## 5. Current MG1 Final Score

MG1 uses displayed challenge scores by default:

```text
MiniGame1LearningProfileSO.useDisplayedChallengeScoresForFinal = true
```

Current weights:

```text
Drift = 40%
Camera Alignment = 25%
Speed Consistency = 35%
```

Formula:

```text
Final Score = DriftScore * 0.40 + CameraScore * 0.25 + SpeedScore * 0.35
```

Legacy metric scoring still exists and can be enabled by changing the profile, but it is not the default intended MG1 path now.

---

## 6. Current MG1 Challenge Scores

### Drift

The drift challenge is track-free.

It evaluates counter-steering against the injected drift:

```text
stabilizedError = abs(DeltaAngle(0, inputAngleDeg + driftAngleDeg))
```

Score:

```text
DriftScore = 55% responseScore + 45% errorScore
```

### Camera Alignment

The camera challenge measures pitch error from target pitch.

Score:

```text
CameraScore = 50% responseScore + 50% alignmentScore
```

### Speed Consistency

The speed challenge measures speed standard deviation during a sample window.

Score:

```text
SpeedScore = 1 - speedStdDev / allowedWorstStdDev
```

---

## 7. Tier System

Current MG1 default tiers:

| Tier | Score Range |
|------|-------------|
| Excellent | 90-100 |
| Good | 70-89 |
| Average | 50-69 |
| Fail | below pass score, usually below 50 |

Fail means:

```text
No robot update
Apply Improvements disabled
Retry required
```

---

## 8. Robot Stat Update Rule

Robot core stats are updated by tier, not directly by exact final score.

Current MG1 deltas:

| Tier | Stability | Path Accuracy | Input Responsiveness |
|------|-----------|---------------|----------------------|
| Excellent | +0.15 | +0.15 | +0.10 |
| Good | +0.10 | +0.10 | +0.07 |
| Average | +0.05 | +0.05 | +0.03 |
| Fail | +0.00 | +0.00 | +0.00 |

All values are clamped:

```text
newValue = clamp01(oldValue + delta)
```

---

## 9. MG1 Fault Probability Rule

MG1 also maps each challenge score into a future fault probability.

Current curves live in:

```text
MiniGame1LearningProfileSO
```

Fields:

```text
driftScoreToErrorRate
cameraScoreToErrorRate
speedScoreToErrorRate
```

Default mapping:

```text
0 score   -> 0.95 probability
50 score  -> 0.50 probability
100 score -> 0.05 probability
```

The updater saves:

```text
robotStats.driftErrorRate
robotStats.cameraErrorRate
robotStats.speedErrorRate
robotStats.hasSavedCalibrationResult = true
```

---

## 10. Post-MG1 Fault Event Model

After MG1 is passed, the robot can experience problem events. These are not continuous sine wobble effects anymore. They are random timed events.

All three problem categories share the same timing configuration in `RobotStabilityApplier`:

```text
Min Fault Check Interval Seconds
Max Fault Check Interval Seconds
```

At each random interval, a roll is made:

```text
if random(0..1) < relatedProbability:
    trigger fault
```

### Drift Fault

Probability:

```text
driftErrorRate
```

Effect:

```text
Temporary yaw drift in robot movement
```

Settings:

```text
Drift Fault Yaw Deg
Drift Fault Duration Seconds
```

### Camera Fault

Probability:

```text
cameraErrorRate
```

Effect:

```text
Temporary pitch offset in robot camera
```

Settings:

```text
Robot Camera
Camera Fault Pitch Offset Deg
Camera Fault Duration Seconds
```

### Speed Fault

Probability:

```text
speedErrorRate
```

Effect:

```text
Cancel sprint and block sprint temporarily
```

Settings:

```text
Sprint Block Duration Seconds
```

---

## 11. Fault Ownership

All post-MG1 fault event logic is centralized here:

```text
RobotStabilityApplier.cs
```

Responsibilities:

```text
roll drift fault
roll camera fault
roll speed fault
apply movement yaw drift
apply camera pitch offset
call RobotMovement.CancelSprintFromFault()
clear camera offset safely when faults are unavailable
```

`ControlManager` does not implement camera fault logic anymore. It only exposes:

```text
RobotCamera
```

So `RobotStabilityApplier` can auto-resolve the robot camera if it was not assigned directly.

---

## 12. Temporary MG1 Challenge Faults

The project also has a temporary challenge-only fault object:

```text
MiniGame1FaultState.cs
```

This is not post-MG1 behavior.

It is used during MG1 only:

```text
yawDriftDeg -> drift challenge
speedWobbleAmplitude -> speed challenge
```

This object is intentionally separate from `RobotStabilityApplier`.

---

## 13. Result Screen Rule

If the player fails a mini-game that controls progression, the game should not let the player apply bad updates.

For MG1 this is implemented:

```text
MiniGame1ResultScreenUI disables Apply Improvements on Fail
```

Fail behavior:

```text
Apply disabled
Recalibrate/retry available
No robot update
No fault probabilities saved from that run
```

---

## 14. Mini-Game 2 Direction

MG2 should follow the same overall learning system, but with different stats.

Current intended MG2 concerns:

```text
Energy efficiency
Path efficiency
Decision confidence
Collision/path safety
```

MG2 stat updater already exists and updates:

```text
energyEfficiency
pathAccuracy
decisionConfidence
```

---

## 15. Universal Mini-Game Template

Use this for future mini-games:

```text
Mini-Game Name:
Story Purpose:
Start Condition:
End Condition:
Tracked Data:
Challenge Scores:
Final Score Formula:
Pass Score:
Tier Rules:
Affected Robot Stats:
Fault/Behavior Impact:
Result Screen Behavior:
Fail Behavior:
Scene Setup:
Scripts:
```

---

## 16. Current Practical Rules

- Do not update robot stats mid-challenge.
- Do not allow failed MG1 runs to continue story progression.
- Keep post-MG1 problem categories connected to MG1 challenge categories.
- Keep designer tuning values exposed in the Inspector.
- Keep post-MG1 fault logic centralized on `RobotStabilityApplier`.
- Keep challenge-only fault injection separate in `MiniGame1FaultState`.
- Prefer challenge-score final results for MG1 so the result screen matches the final percentage.
