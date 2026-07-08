# Current Level 1 Delivery Flow Draft

This file is the short delivery-flow draft for Level 1. It has been updated to match the current implementation and should be used as the high-level narrative checklist.

---

## 1. Opening Flow

1. Player starts the game and enters the inherited house.
2. Player finds the robot and the grandfather message setup.
3. Player interacts with the message.
4. The game explains that the grandfather left messages through the robot system.
5. Player is directed to use the robot/pod control system.
6. When the player tries to continue, the robot movement system is not reliable enough.
7. The game asks the player to run movement calibration first.
8. The Configure/Start button begins Mini-Game 1.

---

## 2. Mini-Game 1 Purpose

Mini-Game 1 teaches and tests robot control. It calibrates three problem categories:

```text
Drift handling
Camera alignment
Speed consistency
```

The result does not just create a final grade. It also sets the robot's future problem probabilities.

---

## 3. Mini-Game 1 Flow

Current implemented order:

```text
Free movement introduction
Drift challenge left/right
Free movement gap
Opposite drift challenge
Free movement gap
Camera alignment challenge
Free movement gap
Speed consistency challenge
Result screen
```

The drift challenge is now track-free. It does not need the old `MG1_Track` object to score correction.

---

## 4. Result Screen Rules

After MG1 finishes:

```text
Final score appears
Tier appears
Drift / Camera / Speed scores appear
```

If the player fails:

```text
Apply Improvements is disabled
Player must retry/recalibrate
No robot stat update is saved
No post-MG1 fault probabilities are applied
```

If the player passes:

```text
Apply Improvements is enabled
Robot stats are updated
Fault probabilities are saved
Player can continue story flow
```

---

## 5. Post-MG1 Robot Behavior

After passing MG1, the robot is improved but not perfect.

The three trained problem categories can still happen later as random fault events:

```text
Drift problem
Camera pitch problem
Sprint/speed problem
```

Every random interval, the game rolls against the stored probability.

Example:

```text
driftErrorRate = 0.20
Every 6-10 seconds, there is a 20% chance of a drift event.
```

All post-MG1 fault settings are now on the `RobotStabilityApplier` component on the Robot.

---

## 6. After Apply Improvements

After a passed result and Apply click:

1. Result screen closes.
2. Robot stats/fault probabilities are already saved.
3. Post-MG1 story flow begins.
4. The corrupted/grandfather message is prepared.
5. Player reads/closes the message.
6. Objective changes toward the storage room.
7. Storage door progression leads toward Mini-Game 2.

---

## 7. Mini-Game 2 Direction

MG2 should focus on the audio card / storage room problem.

Current intended purpose:

```text
Reach audio card
Use efficient path/energy decisions
Teach robot path and energy awareness
```

The next project milestone after MG1 polish is to make the MG1-to-MG2 path reliable and then finish MG2 mechanics/scoring.
