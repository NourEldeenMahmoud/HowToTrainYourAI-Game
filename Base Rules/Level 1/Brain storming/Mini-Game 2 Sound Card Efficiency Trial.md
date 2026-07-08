---
share_link: https://share.note.sx/dmx8cfjx#GAHBhRYfJczp3no+2FT+AaDF8N2NEzdq39mkKE3rVMA
share_updated: 2026-04-12T00:36:09+02:00
dg-publish: true
---

# Mini-Game 2 - Sound Card Efficiency Trial

This file describes the intended MG2 design and how it connects to the currently implemented MG1 flow.

MG2 begins after the player has calibrated robot movement in MG1 and follows the story beat that the grandfather message/audio system still needs repair or recovery.

---

## 1. Story Context

After MG1, the robot can move and the player can apply movement improvements. The next obstacle is the audio/message system. The grandfather message is corrupted or incomplete, and the player needs to reach the storage room / audio card objective.

MG2 is the next training step:

```text
movement calibration -> audio card / energy-path efficiency trial
```

---

## 2. Current Transition From MG1

Current implemented transition direction:

1. Player passes MG1.
2. Player clicks `Apply Improvements`.
3. `MG1ToMG2FlowCoordinator` starts the post-MG1 story flow.
4. Grandfather/corrupted message text is prepared.
5. Player opens/closes the message.
6. Objective updates toward the storage room.
7. Storage door transition leads toward MG2.

The MG2 scene path currently referenced by code is:

```text
Assets/Scenes/Oraby/Second MiniGame.unity
```

---

## 3. Core Idea

MG2 teaches the robot route planning and energy-aware decision making.

The robot may move in a top-down/grid/planning style rather than direct real-time movement.

Main lesson:

```text
Do not just reach the target. Choose the efficient route.
```

---

## 4. Objective

The player must guide the robot to the audio card / target while preserving energy and avoiding poor path choices.

Core objectives:

- Reach the audio card.
- Avoid wasting energy.
- Avoid unnecessary collisions or inefficient routes.
- Teach the robot how to select a better path.

---

## 5. Current MG2 Code Direction

Known MG2 scripts in the project:

```text
MiniGame2Manager.cs
MiniGame2LearningProfileSO.cs
MiniGame2RobotStatUpdater.cs
MiniGame2Types.cs
MiniGame2ResultScreenUI.cs
GridManager.cs
FloorTile.cs
TileClickMover.cs
MG2CinemachineTopDownInput.cs
MG2MovableTileHighlighter.cs
EnableCursor.cs
```

Current MG2 robot stat update direction:

```text
energyEfficiency
pathAccuracy
decisionConfidence
```

---

## 6. Relationship To MG1

MG1 affects robot behavior before and during later gameplay through fault probabilities:

```text
driftErrorRate
cameraErrorRate
speedErrorRate
```

These are applied by `RobotStabilityApplier` as random fault events after MG1 passes.

MG2 should not duplicate MG1 scoring. MG2 should evaluate a different skill set:

```text
path choice
energy use
decision quality
collision/path safety
```

---

## 7. Proposed MG2 Metrics

Recommended metrics:

```text
Energy Efficiency
Path Efficiency
Collision Safety
Decision Confidence
```

If keeping only three metrics, recommended weights:

```text
Energy Efficiency = 40%
Path Efficiency = 35%
Collision Safety / Decision Quality = 25%
```

If the current code uses three displayed categories, keep final result aligned with what the result screen shows.

---

## 8. Energy System Design

Energy should be limited and meaningful.

Energy can be reduced by:

- movement steps;
- moving to expensive floor tiles;
- collisions;
- unnecessary turns or inefficient choices if implemented;
- choosing a longer route when a clearly better route exists.

There is no requirement for a full charging system in this mini-game. The main test is efficient energy use.

---

## 9. Path Design

MG2 should give clear route choices.

Example route types:

```text
short but risky
long but safe
energy-saving path
blocked path
```

The goal is not to create a confusing maze. The goal is to test decision making.

The player should understand why a path was good or bad.

---

## 10. Result and Fail Rules

Recommended pass rule:

```text
finalScore >= 50 -> pass
finalScore < 50 -> fail/retry
```

Fail should not apply robot stat improvements.

Passed result should update:

```text
energyEfficiency
pathAccuracy
decisionConfidence
```

The current `MiniGame2RobotStatUpdater` already follows a tier-delta update style.

---

## 11. UI Requirements

MG2 UI should clearly show:

- target objective;
- current energy;
- path/step feedback;
- result screen with final score and tier;
- retry/apply or continue behavior consistent with MG1.

If MG2 can fail, it should follow the same design rule as MG1:

```text
Do not apply robot stat updates on fail.
```

---

## 12. Suggested Logs

Robot/system logs can include:

```text
Energy level critical
Collision detected
Path inefficiency detected
Alternative route recommended
Energy saving surface detected
Target reached: Audio Card
Route efficiency updated
Decision confidence improved
```

---

## 13. Current Next Step For MG2

The project should next verify:

```text
MG1 pass -> Apply -> message objective -> storage door -> MG2 scene load
```

Then focus on:

- MG2 playable loop;
- MG2 scoring;
- MG2 result screen;
- MG2 stat update;
- scene reset/control state after loading MG2.

---

## 14. Notes

- MG2 should remain environment independent where possible.
- The audio card story should justify why the player must go to storage.
- MG2 should teach a new robot skill, not repeat MG1 movement calibration.
- MG1 fault events can still happen after MG1, but they should not interfere with MG2 if MG2 is actively running because `RobotStabilityApplier` disables persistent faults while MG2 is running.

---

<!-- opencode:related-start -->
## Related
- [[01 Projects/VrProject/VR Project.md|VrProject Hub]]
- [[01 Projects/01 Projects Index|Projects Index]]
- [[02 Areas/02 Personal/Main/01 Current Areas|Current Areas]]
<!-- opencode:related-end -->
