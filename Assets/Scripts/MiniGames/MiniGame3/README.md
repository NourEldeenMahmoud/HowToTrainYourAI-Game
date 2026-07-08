Stage 1 scaffolding for Mini-Game 3

This folder contains the planned MG3 runtime and editor scripts.

Stage 1 deliverables implemented by files in this commit:

- Folder structure:
  - Assets/Scripts/MiniGames/MiniGame3/
  - Assets/Scripts/MiniGames/MiniGame3/UI/
  - Assets/Scripts/MiniGames/MiniGame3/Editor/
  - Assets/Data/MiniGames/MiniGame3/

- An Editor scaffolder script is provided to create the required scene root GameObjects
  (MG3_Root, MG3_Grid, MG3_Tasks, MG3_UI, MG3_Cameras) and optionally reparent existing
  scene objects that match common MG3 names (Task 1 Devices, Task 2 Devices, Task 3 Devices,
  Grid Floor, CameraPivot, CM vcam1, Main Camera, Robot). The scaffolder does not run
  automatically; use the Unity Editor menu Tools -> MG3 -> Setup Scene Roots to apply.

Notes:
- This commit does not modify any existing scenes. The scaffolder provides a safe,
  reversible way to update the scene in the Editor (supports Undo).
- Follow the implementation plan in mini-game-3-training-signature-match-implementation.md
  for the next steps. Attach runtime components (MG3GridManager, MG3PushableDevice, etc.)
  only after the scene roots and grid tiles are in place.
