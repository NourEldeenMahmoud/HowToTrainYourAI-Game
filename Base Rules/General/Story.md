---
dg-publish: true
---

# Story Reference - How To Train Your AI

This file describes the current story direction and how it connects to the implemented Level 1 systems.

---

## 1. Core Premise

The player is a 25-year-old software worker whose life has become boring and unstable. He loses his job because AI replaces him. While preparing to leave his apartment, he receives a message from his grandfather saying he inherited an old house.

The player goes to the house and discovers that the grandfather left behind a robot and a set of recorded messages. The robot is not fully trained. It is clumsy, unreliable, and needs the player to teach it through mini-games.

The player starts by controlling the robot through a pod/interface.

---

## 2. Long-Term Story Direction

The house is divided into several floors or locked sections. Each section trains a different robot capability.

Possible progression:

```text
Level 1 -> movement, camera, speed, energy/path basics
Later levels -> perception, decision making, autonomy, memory, strategy
Final level -> robot can operate as a real AI assistant/friend
```

At the start, the robot is unreliable. By the end, it becomes competent and emotionally meaningful to the player.

The story theme:

```text
AI caused the player's life problem, but training this robot helps him recover purpose and build something useful.
```

---

## 3. Level 1 Current Implemented Arc

Level 1 currently focuses on the robot's first calibration.

Flow:

1. Player enters the house.
2. Player finds the robot/message setup.
3. Player tries to access the grandfather message.
4. The robot movement system is unstable.
5. Player must complete Mini-Game 1.
6. MG1 calibrates movement, camera, and speed control.
7. If MG1 fails, the player must retry.
8. If MG1 passes, the player can apply improvements.
9. The grandfather/corrupted message flow continues.
10. Player is directed toward the storage room.
11. Storage/audio-card flow leads toward Mini-Game 2.

---

## 4. Mini-Game 1 Story Role

MG1 is the robot's movement calibration.

The robot has three major issues:

```text
drift
camera misalignment
speed instability
```

The player completes challenges that match these issues.

The robot improves after passing, but the problems do not disappear completely. Instead, each problem gets a future probability based on the player's score.

Example:

```text
Good drift score -> low chance of future drift problem
Bad camera score -> higher chance of future camera pitch fault
Excellent speed score -> very low chance of sprint fault
```

This supports the story idea that the robot is learning gradually, not magically becoming perfect.

---

## 5. Post-MG1 Robot Behavior In Story Terms

After MG1, the robot's problems become occasional events:

```text
It might drift briefly.
Its camera might get misaligned briefly.
Its sprint may cancel and need to be pressed again.
```

These problems happen based on probabilities saved in `RobotStatsSO`.

The player can feel that training mattered because better scores produce fewer interruptions.

---

## 6. Mini-Game 2 Story Role

MG2 is connected to the audio card / storage room objective.

After MG1, the robot can move well enough to continue, but the grandfather message is still corrupted or incomplete. The player needs the robot to reach an audio card or system component in storage.

MG2 should teach:

```text
energy awareness
path efficiency
decision confidence
```

It should move the story from basic robot control into practical robot problem solving.

---

## 7. Robot Development Stages

Current story stages for movement/control:

### Stage 1 - Direct Control

The player controls the robot through the pod. The robot is unreliable and needs calibration.

### Stage 2 - Assisted Control

After MG1, the robot works better but still has probabilistic faults depending on training quality.

### Stage 3 - Learned Assistance

After more mini-games, the robot should become more autonomous and reliable.

### Stage 4 - Trusted AI Partner

By late game, the robot should operate confidently and become a genuine assistant/friend.

---

## 8. Design Rule For Story And Systems

Every mini-game should satisfy both:

```text
Story reason
Gameplay learning reason
```

Example:

```text
MG1 story reason: robot movement system is unstable.
MG1 gameplay reason: teach robot control and calibrate fault probabilities.

MG2 story reason: audio card/storage objective blocks message progress.
MG2 gameplay reason: teach energy/path decision making.
```

---

## 9. Current Level 1 Practical Goal

The current project milestone is:

```text
Make MG1 completion -> Apply Improvements -> corrupted message -> storage objective -> MG2 start fully reliable.
```

After that, the focus should shift to polishing MG2 mechanics and result behavior.

---

## 10. Backstory Notes

- The player may find a box of tools, including the pod and analytic screens.
- The robot is too heavy to carry, so the player must operate it through the pod.
- The robot's learning curve should be visible through stats, logs, and gameplay behavior.
- Grandfather messages should reward progress and explain the purpose of the house.

---

<!-- opencode:related-start -->
## Related
- [[01 Projects/VrProject/VR Project.md|VrProject Hub]]
- [[01 Projects/01 Projects Index|Projects Index]]
- [[02 Areas/02 Personal/Main/01 Current Areas|Current Areas]]
<!-- opencode:related-end -->
