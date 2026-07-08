# How To Train Your AI

> *A 25-year-old software worker loses his job to AI. He inherits an old house from his grandfather — and discovers an unfinished robot inside.*

---

<p align="center">
  <img src="docs/images/hero-banner.png" alt="How To Train Your AI - Banner" width="80%" />
</p>

<p align="center">
  <strong>A narrative-driven training simulator about trust, purpose, and the machines we build.</strong>
</p>

---

## About

**How To Train Your AI** is a Unity 6 first-person experience where you teach a broken robot to think, move, and solve problems — one mini-game at a time. The irony is the point: the same technology that displaced the player becomes the thing that restores his purpose.

Through three distinct training modules, the robot evolves from an unreliable prototype into a capable companion. Every mistake you make has consequences. Every improvement feels earned.

---

## Story

<p align="center">
  <img src="docs/images/story-screenshot.png" alt="The inherited house" width="70%" />
</p>

You are Nour — a software engineer who just got laid off to AI automation. While packing up your life, a lawyer hands you a letter: your grandfather left you his old house.

Inside, buried under dust and memories, you find a half-finished robot and a series of recorded messages. Your grandfather was building something — something he never got to finish. His final message is corrupted, but the last thing it says is clear:

> *"Trust the robot. It will show you what matters."*

The journey begins in the living room, but it ends somewhere you didn't expect.

---

## Gameplay

The game is built around **three training mini-games**, each teaching the robot a fundamental skill. Performance is measured, scored, and — critically — it matters.

### Mini-Game 1: Control Calibration

<p align="center">
  <img src="docs/images/mg1-screenshot.png" alt="Mini-Game 1 - Control Calibration" width="70%" />
</p>

The robot's movement system is unstable. You need to calibrate it.

| Challenge | What You Do |
|---|---|
| **Drift Handling** | The robot drifts left or right. Counter-steer to stay on course. |
| **Camera Alignment** | The camera pitch is offset. Return it to the target angle. |
| **Speed Consistency** | Speed wobbles unpredictably. Maintain a steady pace. |

Your scores determine how reliable the robot is going forward. Nail it, and the robot barely stumbles. Fail, and you'll be fighting random faults for the rest of the game.

**Scoring:** Drift (40%) + Camera (25%) + Speed (35%) → Normalized 0–100 scale

| Tier | Score | Result |
|---|---|---|
| Excellent | 90+ | Minimal faults |
| Good | 70–89 | Occasional faults |
| Average | 50–69 | Frequent faults |
| Fail | < 50 | Retry required |

---

### Mini-Game 2: Sound Card Efficiency Trial

<p align="center">
  <img src="docs/images/mg2-screenshot.png" alt="Mini-Game 2 - Path Efficiency" width="70%" />
</p>

A top-down grid challenge. The robot needs to collect an audio card while managing a limited energy budget.

- **Click to move** the robot across tiles
- **Different tiles cost different energy** — some are efficient, some are expensive
- **Energy depletion = mission failure**
- **Path efficiency** is measured against the ideal shortest path

This teaches the robot to make smart decisions under constraints — not just fast ones.

**Scoring:** Energy Efficiency (40%) + Path Efficiency (35%) + Decision Quality (25%)

---

### Mini-Game 3: Training Signature Match

<p align="center">
  <img src="docs/images/mg3-screenshot.png" alt="Mini-Game 3 - Push Puzzle Lab" width="70%" />
</p>

A sci-fi lab. Push devices to their correct positions. Sounds simple — until the puzzles get layered.

| Task | Mechanic |
|---|---|
| **Exact Placement** | Push specific devices to their exact matching target tiles |
| **Group Placement** | Group devices by type — any device of the correct group works |
| **Size Ordering** | Sort devices by size rank in the correct sequence |

- Right-click to pathfind to a destination
- Press **E** to push objects one grid step
- Objects block movement and future devices can't be pushed
- Deadlock detection resets the task if you get stuck

No timer. No score pressure. The challenge is pure spatial logic.

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
  <img src="docs/images/gallery-1.png" alt="Screenshot 1" width="45%" />
  &nbsp;&nbsp;
  <img src="docs/images/gallery-2.png" alt="Screenshot 2" width="45%" />
</p>

<p align="center">
  <img src="docs/images/gallery-3.png" alt="Screenshot 3" width="45%" />
  &nbsp;&nbsp;
  <img src="docs/images/gallery-4.png" alt="Screenshot 4" width="45%" />
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

---

## Team

| Name | Role |
|---|---|
| **Nour** | Core Systems, MG1, Robot Logic |
| **Omar** | Mini-Game 1 |
| **Oraby** | Mini-Game 2 |
| **Aya** | Mini-Game 3, Post-Credits |

---

## Documentation

- [`PROJECT_PROGRESS_REFERENCE.md`](PROJECT_PROGRESS_REFERENCE.md) — Implementation map with script responsibilities
- [`TEAM_COLLABORATION_GUIDE.md`](TEAM_COLLABORATION_GUIDE.md) — Team workflow guide (Arabic)
- [`TRAILER_PLAN.md`](TRAILER_PLAN.md) — YouTube trailer structure and shot list
- [`Base Rules/`](Base Rules/) — Design documentation and brainstorming
- [`Documentation/`](Documentation/) — Technical specs and session handoffs

---

## License

This project is for educational purposes. See the repository for license details.
