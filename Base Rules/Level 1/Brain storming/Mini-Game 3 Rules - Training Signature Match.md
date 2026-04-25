---
dg-publish: true
---

# Mini-Game 3 Rules - Training Signature Match

This file defines the current intended direction for Mini-Game 3 based on the implemented MG1 learning/fault system and the planned MG2 energy/path system.

MG3 should not be another basic tutorial. It should be a validation challenge that checks whether the robot has learned from previous mini-games.

---

## 1. Core Design Rule

Mini-Game 3 is a combined validation test.

It should use the history of:

```text
MG1 movement calibration
MG2 path/energy efficiency
```

It should not block the grandfather message forever. The message progression should already be meaningfully advanced after MG2.

---

## 2. Story Purpose

By MG3, the robot has been trained in basic movement and efficiency. MG3 should test whether it can apply those skills in a more autonomous or semi-autonomous situation.

Possible story purpose:

```text
The house security system asks for a training signature match.
The robot must prove it can repeat learned behavior patterns reliably.
```

This makes MG3 feel like a system validation rather than random extra training.

---

## 3. Relationship To MG1

MG1 produced three future fault probabilities:

```text
driftErrorRate
cameraErrorRate
speedErrorRate
```

MG3 can reference those categories:

```text
drift correction memory
camera alignment memory
speed/sprint reliability
```

Important: MG3 should not duplicate MG1 exactly. It should test learned behavior in a new context.

---

## 4. Relationship To MG2

MG2 should teach:

```text
energy efficiency
path accuracy
decision confidence
```

MG3 can include route choices or limited intervention where the robot must use efficient behavior.

---

## 5. Proposed Core Mode - Training Signature Match

The system shows or generates a target behavior signature, and the player/robot must match it.

Examples of target signatures:

```text
move straight under mild drift
align camera after sudden offset
choose efficient route around obstacle
maintain sprint timing without wasting energy
reach target with limited corrections
```

The player may have limited override chances.

---

## 6. Suggested Rules

### Rule A - Movement Echo

The system creates movement scenarios inspired by MG1.

Examples:

```text
brief drift event
camera pitch offset
sprint cancellation risk
```

The player/robot must recover faster than in MG1.

Evaluation:

```text
correction response time
average correction error
number of interventions
```

### Rule B - Energy/Path Echo

The system creates route choices inspired by MG2.

Examples:

```text
short risky route
long safe route
energy-saving route
blocked or inefficient route
```

Evaluation:

```text
chosen route efficiency
energy remaining
collision count
decision timing
```

### Rule C - Limited Override Check

The robot performs part of the task semi-autonomously.

The player has limited overrides.

Recommended default:

```text
3 free overrides
extra overrides reduce score
```

Evaluation:

```text
fewer overrides = better robot learning
good overrides = acceptable
bad or excessive overrides = lower score
```

---

## 7. Possible Final Score

Suggested MG3 final categories:

```text
Training Match Accuracy = 40%
Autonomy / Override Efficiency = 30%
Energy & Path Discipline = 30%
```

Alternative if movement is central:

```text
Movement Recovery = 35%
Decision Accuracy = 35%
Override Efficiency = 30%
```

---

## 8. Fail / Pass Rules

Recommended:

```text
Excellent = 90-100
Good = 70-89
Average = 50-69
Fail = below 50
```

Fail behavior should follow the project rule:

```text
No robot stat update
Retry required
Do not progress through locked story gate
```

---

## 9. Robot Update Direction

MG3 should not overwrite MG1 and MG2 results blindly.

It should improve higher-level stats such as:

```text
decisionConfidence
inputResponsiveness
stability
energyEfficiency
```

If MG3 uses future fault probabilities, it should only reduce them slightly, not reset them to zero.

Example:

```text
Excellent MG3 -> reduce all current fault probabilities by 10-15%
Good MG3 -> reduce by 5-10%
Average MG3 -> small/no reduction
Fail -> no update
```

---

## 10. Implementation Notes For Later

When MG3 is implemented, it should have:

```text
MiniGame3Manager
MiniGame3LearningProfileSO
MiniGame3ScoringEngine
MiniGame3RobotStatUpdater
MiniGame3ResultScreenUI
```

It should follow MG1/MG2 patterns:

- collect data during game;
- evaluate at the end;
- result screen gates apply/retry;
- stat update only on pass;
- no mid-game permanent stat updates.

---

## 11. Current Dependency Warning

MG3 should understand the current MG1 post-fault system:

```text
RobotStabilityApplier owns drift/camera/speed event faults.
```

If MG3 needs to temporarily inject its own faults, it should not fight `RobotStabilityApplier`. Options:

```text
temporarily pause persistent faults during MG3
or reuse the same event system with explicit MG3 control
```

The project already disables persistent MG1 faults while MG2 is running. MG3 should follow the same rule.
