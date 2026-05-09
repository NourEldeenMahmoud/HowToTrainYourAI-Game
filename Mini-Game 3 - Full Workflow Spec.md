# Mini-Game 3 - Training Signature Match

## 1. Core Idea

A robot training puzzle inside a laboratory.
The player controls the robot in a normal-looking lab floor, but the gameplay is built on an invisible grid.

The goal of each task is to push specific devices to their correct marked tiles.
This mini-game teaches spatial logic, grid movement, and puzzle planning.

---

## 2. Player Fantasy

- The player is training a robot to organize a lab.
- The robot does not carry objects.
- The robot only moves around and pushes devices.
- Success comes from reading the layout and placing every object correctly.

---

## 3. Game Structure

The mini-game contains 3 tasks:

- Task 1: Place specific devices
- Task 2: Group devices by type
- Task 3: Sort devices by size

The tasks are strictly linear:

- Task 1 must be completed before Task 2
- Task 2 must be completed before Task 3
- Task 3 completes the mini-game

Each task uses a fixed authored layout.
No randomization.

---

## 4. World Rules

- The floor consists of blocks or tiles prefabs , which is divided into two categories , normal tiles and targeted tiles (the correct place for the objects) ,gameplay uses an invisible grid.
- Movement is 4-direction only.
- The robot pushes one grid step at a time.
- Objects move one grid step per push.
- Objects can only be pushed from the logical direction behind them.
- One push affects only one object.
- Objects block movement.
- Solved objects remain physical obstacles.
- Solved objects do not need to be crossed by later routes.
- The player must press interact to start a push.
- Movement is locked while the push is happening.
- A push must fully complete once started.
- There is no undo.
- There is no timer or score pressure.

---

## 5. Controls

### Movement

- Click on the floor to move the robot.
- The click is converted into a grid destination.
- The robot pathfinds automatically through empty tiles only.
- If the destination is unreachable, the move is refused

### Pushing

- The robot must stand in a valid push position behind an object.
- The player presses interact to start the push.
- The object moves one tile in the push direction.
- The robot cannot steer during the push.
- The push cannot be cancelled once committed.

### Reset

- If a dead tile is reached, the current task resets.
- Reset restores the full task state.
- Reset also restores the robot position and UI for that task.

---

## 6. Task Flow

### Start

1. Player enters the lab.
2. Intro UI explains the current task.
3. The full target layout is shown up front.
4. The player begins moving the robot.

### During Play

1. Player clicks a destination.
2. Robot pathfinds to the nearest valid reachable tile.
3. Player positions the robot behind a device.
4. Player presses interact.
5. Robot pushes the device one tile.
6. The game checks whether the object is now in a valid tile.
7. If the object is correct, it locks in place and lighten up (signal for success).
8. If the object is wrong, it remains movable.
9. If a dead tile is reached, the task fails and resets.

### Completion

1. The game waits until all objects are stationary.
2. After a short settle delay, completion is checked.
3. If all required placements are correct at the same time, the task completes.
4. A short completion popup appears.
5. The next task starts instantly.

---

## 7. Task Definitions

### Task 1 - Place Specific Devices

Goal:

- Each device has one exact matching tile.

Behavior:

- The player places each device in its exact target slot.
- Wrong placement does not green the tile.
- The device can still be moved again.

### Task 2 - Group Devices

Goal:

- Devices of the same type belong to the same group.

Behavior:

- Any device from the correct group can go on any tile in that group.
- Group validation is based on type matching.

### Task 3 - Sort by Size

Goal:

- Devices must be arranged by size.

Behavior:

- The final order matters.
- The layout checks the correct size arrangement.

###### note:
- all tasks are happening in the same place in the same scene , the flow is linear which mean that the second task cannot start until the first task ends 

---

## 8. Validation Rules

- A tile only turns green when the right object is in the right place.
- Non-target tiles are allowed as temporary positions.
- Only marked tiles count toward completion.
- One object per slot.
- Extra objects are rejected or pushed out.
- The task completes only when all required objects are correct at the same time.
- Completion is checked only after all movement stops.
- Solved objects are locked in place.

---

## 9. Dead Tiles

Dead tiles are tiles that cannot be moved out of in any direction once reached by an object.

Rules:

- Dead tiles only affect pushed objects.
- The robot itself is not failed by a dead tile.
- Deadlocks are discovered during play.
- Deadlock triggers a short fail popup.
- Then the current task resets completely.
- All tasks can contain dead tiles.

---

## 10. UI

The UI should show:

- Current task name
- Short task instructions
- Full target layout
- Device references or images if needed
- Completion popup
- Fail popup for deadlock/reset

The UI should not hide the core layout logic from the player.

---

## 11. State Flow

Game states:

- Idle
- Task 1
- Task 2
- Task 3
- Complete

Within a task, the important states are:

- Waiting for input
- Pathfinding
- Moving
- Ready to push
- Pushing
- Settling
- Checking completion
- Resetting

---

## 12. Edge Cases

### Movement Edge Cases

- Clicking an unreachable tile should do nothing and show feedback.
- The robot should not move while a push is in progress.
- The robot should not accept a second move until the current push is done.

### Push Edge Cases

- A push can only start if the robot is in the valid push position.
- A push affects one object only.
- A push cannot be interrupted.
- A push cannot be cancelled after commitment.

### Slot Edge Cases

- If the wrong object is placed, the tile stays unlit.
- The object remains movable.
- If the correct object is placed, it locks.
- Locked objects remain physical blockers.

### Reset Edge Cases

- Reset must restore the whole task.
- Reset must restore objects, robot, and UI.
- Reset must not carry over partial progress.

### Completion Edge Cases

- Completion only happens after everything is stationary.
- A short settle delay prevents false checks.
- The next task starts only after the brief popup.

---

## 13. Implementation Notes

- Use grid logic even if the floor art looks continuous.
- Pathfinding should treat objects as hard obstacles.
- Pushing should resolve on the grid, not by freeform physics.
- Validation should be driven by tile IDs and object signatures.
- The authored layout must be designed around locked blockers and dead tiles.

---

## 14. Final Flow Summary

Click to move -> pathfind to reachable empty tiles -> reach push position -> press interact -> push one object one tile -> settle -> check tile match -> lock if correct -> continue until all required objects are correct -> short completion popup or reset on deadlock.
