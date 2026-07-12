# How To Train Your AI

> *A 25-year-old software worker loses his job to AI. He inherits an old house from his grandfather — and discovers an unfinished robot inside.*

---

<p align="center">
  <img src="Documentation/Imgaes/Game hero.png" alt="How To Train Your AI - Banner" width="80%" />
</p>

<p align="center">
  <strong>A narrative-driven training simulator about trust, purpose, and the machines we build.</strong>
</p>

<p align="center">
  <h2><a href="https://youtu.be/bdoZxAmPt6U">Watch Game Preview</a></h2>
</p>

---

## About

**How To Train Your AI** is a Unity 6 first-person experience where you teach a broken robot to think, move, and solve problems — one mini-game at a time. The irony is the point: the same technology that displaced the player becomes the thing that restores his purpose.

Through three distinct training modules, the robot evolves from an unreliable prototype into a capable companion. Every mistake you make has consequences. Every improvement feels earned.

---

## Story

<p align="center">
  <img src="Documentation/Imgaes/House.png" alt="The inherited house" width="70%" />
</p>

<p align="center">
  <img src="Documentation/Imgaes/House Gate.png" alt="House gate" width="70%" />
</p>

You are Nour — a software engineer who just got laid off to AI automation. While packing up your life, a lawyer hands you a letter: your grandfather left you his old house.

Inside, buried under dust and memories, you find a half-finished robot and a series of recorded messages. Your grandfather was building something — something he never got to finish. His final message is corrupted, but the last thing it says is clear:

> *"Trust the robot. It will show you what matters."*

<p align="center">
  <img src="Documentation/Imgaes/Grandfather message.png" alt="Grandfather message" width="70%" />
</p>

<p align="center">
  <img src="Documentation/Imgaes/Dialogue 1.png" alt="Dialogue 1" width="45%" />
  &nbsp;&nbsp;
  <img src="Documentation/Imgaes/Dialogue 2.png" alt="Dialogue 2" width="45%" />
</p>

The journey begins in the living room, but it ends somewhere you didn't expect.

---

## Gameplay

The game is built around **three training mini-games**, each teaching the robot a fundamental skill. Performance is measured, scored, and — critically — it matters.

### Mini-Game 1: Control Calibration

<p align="center">
  <img src="Documentation/Imgaes/Robot before miniagme 1.png" alt="Robot before Mini-Game 1" width="70%" />
</p>

<p align="center">
  <img src="Documentation/Imgaes/minigame 1.png" alt="Mini-Game 1 - Control Calibration" width="70%" />
</p>

The robot's movement system is unstable. You need to calibrate it.

| Challenge | What You Do |
|---|---|
| **Drift Handling** | The robot drifts left or right. Counter-steer to stay on course. |
| **Camera Alignment** | The camera pitch is offset. Return it to the target angle. |
| **Speed Consistency** | Speed wobbles unpredictably. Maintain a steady pace. |

<p align="center">
  <img src="Documentation/Imgaes/minigame 1.1.png" alt="Mini-Game 1 detail" width="70%" />
</p>

Your scores determine how reliable the robot is going forward. Nail it, and the robot barely stumbles. Fail, and you'll be fighting random faults for the rest of the game.

**Scoring:** Drift (40%) + Camera (25%) + Speed (35%) → Normalized 0–100 scale

| Tier | Score | Result |
|---|---|---|
| Excellent | 90+ | Minimal faults |
| Good | 70–89 | Occasional faults |
| Average | 50–69 | Frequent faults |
| Fail | < 50 | Retry required |

<p align="center">
  <img src="Documentation/Imgaes/minigame 1 result .png" alt="Mini-Game 1 result" width="70%" />
</p>

---

### Mini-Game 2: Sound Card Efficiency Trial

<p align="center">
  <img src="Documentation/Imgaes/minigame 2.png" alt="Mini-Game 2 - Path Efficiency" width="70%" />
</p>

A top-down grid challenge. The robot needs to collect an audio card while managing a limited energy budget.

- **Click to move** the robot across tiles
- **Different tiles cost different energy** — some are efficient, some are expensive
- **Energy depletion = mission failure**
- **Path efficiency** is measured against the ideal shortest path

<p align="center">
  <img src="Documentation/Imgaes/minigame 2.1.png" alt="Mini-Game 2 detail" width="70%" />
</p>

This teaches the robot to make smart decisions under constraints — not just fast ones.

**Scoring:** Energy Efficiency (40%) + Path Efficiency (35%) + Decision Quality (25%)

---

### Mini-Game 3: Training Signature Match

<p align="center">
  <img src="Documentation/Imgaes/minigame 3.png" alt="Mini-Game 3 - Push Puzzle Lab" width="70%" />
</p>

A sci-fi lab. Push devices to their correct positions. Sounds simple — until the puzzles get layered.

| Task | Mechanic |
|---|---|
| **Exact Placement** | Push specific devices to their exact matching target tiles |
| **Group Placement** | Group devices by type — any device of the correct group works |
| **Size Ordering** | Sort devices by size rank in the correct sequence |

<p align="center">
  <img src="Documentation/Imgaes/minigame 3.1.png" alt="Mini-Game 3 detail" width="70%" />
</p>

- Right-click to pathfind to a destination
- Press **E** to push objects one grid step
- Objects block movement and future devices can't be pushed
- Deadlock detection resets the task if you get stuck

No timer. No score pressure. The challenge is pure spatial logic.

<p align="center">
  <img src="Documentation/Imgaes/minigame 3 result .png" alt="Mini-Game 3 result" width="70%" />
</p>

---

## Post-Training: Fault Events

After completing Mini-Game 1, the robot starts experiencing **random fault events** based on your training quality:

| Fault | Effect |
|---|---|
| **Drift Fault** | Temporary yaw drift during movement |
| **Camera Fault** | Temporary pitch offset in the robot's camera |
| **Speed Fault** | Sprint gets blocked or canceled |

Better training = fewer interruptions. The robot's reliability is a direct reflection of your effort.

---

## Key Features

- **Player/Robot Control Switching** — Tab to toggle between controlling the player and the robot, with Cinemachine camera blending
- **Persistent Robot Stats** — Your training performance carries forward through the entire game via ScriptableObject data
- **Event-Driven Architecture** — All systems communicate through C# events, keeping modules decoupled
- **A\* Pathfinding** — Custom grid-based pathfinding with energy costs, diagonal support, and obstacle detection
- **Deadlock Detection** — MG3 automatically detects when puzzles are unsolvable and resets
- **Scene Transitions** — Smooth fade-to-black transitions between story and gameplay segments
- **Narrative Delivery** — Corrupted grandfather messages that slowly reveal the story
- **Developer Tools** — Built-in skip shortcuts (F8 in MG2, Enter in MG3) for testing

---

## Controls

| Action | Key |
|---|---|
| Move | WASD |
| Look Around | Mouse |
| Interact | E |
| Toggle Robot Control | Tab |
| Sprint | Left Shift |
| Push Object | E (when adjacent) |
| Pathfind (MG2/MG3) | Right-Click |
| Move to Tile (MG2) | Left-Click on tile |

---

## Screenshots

<p align="center">
  <img src="Documentation/Imgaes/Robot before miniagme 1.png" alt="Robot before Mini-Game 1" width="45%" />
  &nbsp;&nbsp;
  <img src="Documentation/Imgaes/minigame 1 result .png" alt="Mini-Game 1 result" width="45%" />
</p>

<p align="center">
  <img src="Documentation/Imgaes/minigame 2.1.png" alt="Mini-Game 2 detail" width="45%" />
  &nbsp;&nbsp;
  <img src="Documentation/Imgaes/minigame 3.1.png" alt="Mini-Game 3 detail" width="45%" />
</p>

---

## Architecture

```
HowToTrainYourAI/
├── Assets/
│   ├── Scripts/
│   │   ├── Managers/          # ControlManager, SceneTransitionFader
│   │   ├── Robot/             # RobotMovement, CameraLook, FollowPlayer
│   │   ├── Mini Game 1/       # Challenges, Scoring, Fault System
│   │   ├── Mini Game 2/       # GridManager, EnergySystem, TileClickMover
│   │   ├── Mini Game 3/       # PushPuzzle, Pathfinder, TaskValidation
│   │   └── UI/                # Navigation, SlideUI, TimerUI
│   ├── Scenes/
│   │   ├── Nour/              # Main Scene (house, player, robot)
│   │   ├── Omar/              # Mini-Game 1
│   │   ├── Oraby/             # Mini-Game 2
│   │   ├── Aya/               # Mini-Game 3, Post-Credits
│   │   └── Dialogue/          # Message & Office dialogue
│   ├── Data/
│   │   ├── Robot/             # RobotStats_Main.asset
│   │   └── MiniGames/         # Learning profile ScriptableObjects
│   ├── Prefabs/               # Player, Robot, Home, UI
│   └── Mini Game 3/           # Lab GLB, animations
├── Base Rules/                # Design documentation
└── Documentation/             # Technical specs and reports
```

### Design Patterns Used

| Pattern | Where |
|---|---|
| Singleton | `SceneTransitionFader`, `MG1InstructionSequenceController` |
| ScriptableObject | `RobotStatsSO`, `MiniGame1LearningProfileSO`, `MiniGame2LearningProfileSO` |
| State Machine | Phase enums in MG1, MG2, MG3 |
| Strategy / Template | `MiniGame1ChallengeBase` → Drift, Camera, Speed challenges |
| Observer | UI scripts subscribe to manager events |
| Flow Coordinator | `MG1ToMG2FlowCoordinator` for story transitions |
| Component | Grid tiles, pushable devices, target slots as composable MonoBehaviours |

---

## Tech Stack

| Technology | Version | Purpose |
|---|---|---|
| Unity | 6000.5.1f1 | Game engine |
| Universal Render Pipeline | 17.5.0 | Rendering |
| Cinemachine | 3.1.6 | Camera system |
| Input System | 1.19.0 | Player input |
| AI Navigation | 2.0.13 | NavMesh |
| glTFast | 6.19.0 | 3D model import |
| TextMeshPro | 2.5.0 | UI text |
| Timeline | 1.8.12 | Cutscenes |

---

## Getting Started

### Prerequisites

- [Unity Hub](https://unity.com/download)
- Unity **6000.5.1f1** (install via Unity Hub)

### Clone & Run

```bash
git clone https://github.com/NourEldeenMahmoud/HowToTrainYourAI-Game.git
cd HowToTrainYourAI-Game
git lfs install
git lfs pull
```

Open the folder in **Unity Hub**. First open will regenerate the `Library/` folder locally.

Open the `Main Scene` to start.

<p align="center">
  <img src="Documentation/Imgaes/Main Menue.png" alt="Main Menu" width="70%" />
</p>

---

## Team

| Name | Role | GitHub |
|---|---|---|
| **Nour** | Core Systems, MG1, Robot Logic | [@NourEldeenMahmoud](https://github.com/NourEldeenMahmoud) |
| **Omar** | Mini-Game 1 | [@OmarAbouelkheirr](https://github.com/OmarAbouelkheirr) |
| **Oraby** | Mini-Game 2 | [@abdalrhman541](https://github.com/abdalrhman541) |
| **Aya** | Mini-Game 3, Post-Credits | [@AyaSheta13](https://github.com/AyaSheta13) |

---

## License

This project is for educational purposes. See the repository for license details.
