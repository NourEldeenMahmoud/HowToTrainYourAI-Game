# MiniGame 3 - Complete Status Report

**Generated:** 2026-07-03
**Scene:** `Assets/Scenes/Aya/MiniGame 3.unity`
**Scripts:** `Assets/Scripts/MiniGames/MiniGame3/` (16 files)
**Render Pipeline:** URP (Universal Render Pipeline)
**Color Space:** Linear

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Scene Hierarchy](#scene-hierarchy)
3. [Critical Issues (BLOCKER)](#critical-issues-blocker)
4. [High Severity Issues](#high-severity-issues)
5. [Medium Severity Issues](#medium-severity-issues)
6. [Low Severity Issues](#low-severity-issues)
7. [Lighting Analysis](#lighting-analysis)
8. [Camera Analysis](#camera-analysis)
9. [Script Analysis](#script-analysis)
10. [Lab Objects Status](#lab-objects-status)
11. [Recommendations](#recommendations)

---

## Executive Summary

MiniGame 3 is a **push-puzzle game** where a robot pushes devices to target slots on a grid. The scene contains 3 tasks with increasing complexity. The game has **2 BLOCKER issues** that prevent Task 2 and Task 3 from functioning, **2 HIGH severity issues** that make the game visually broken, and several medium/low issues.

**Current State:**
- Task 1: Partially functional (1 of 4 devices has null reference)
- Task 2: **COMPLETELY NON-FUNCTIONAL** (all 8 devices null)
- Task 3: **COMPLETELY NON-FUNCTIONAL** (all 4 slots null)
- Visual: **BROKEN** (camera FOV clamped to 10-20 degrees, scene appears black)
- Camera: Top-down camera disabled, wrong perspective active

---

## Scene Hierarchy

```
MiniGame 3 (Scene)
├── Directional Light [Light, UniversalAdditionalLightData]
├── sci-fi_lab (Position: -105.34, 0, -14.49 | Scale: 3.18x)
│   ├── (2 children - lab environment meshes)
├── MG3_Root (Position: -56.7, 0, -8.24)
│   ├── MG3_Cameras
│   │   ├── CameraPivot (Position: 30, 0, -5.2 | Scale: 3x) [EMPTY]
│   │   ├── CM vcam1 (CinemachineCamera)
│   │   └── Main Camera [Tag: MainCamera, Camera, AudioListener, CinemachineBrain, MG3CameraFovLimiter]
│   └── Robot Top-Down Camera [**DISABLED**, CinemachineCamera, CinemachinePositionComposer, CinemachineRotationComposer]
├── MG3_UI (Position: -56.7, 0, -8.24)
│   ├── InstructionCanvas
│   ├── TaskCanvas
│   └── ResultCanvas
├── MG3_Tasks (Position: -106.7, 0, -14.2)
│   ├── Task 1 Devices [MG3TaskDefinition]
│   ├── Task 2 Devices [MG3TaskDefinition] [**ALL DEVICES NULL**]
│   └── Task 3 Devices [MG3TaskDefinition] [**ALL SLOTS NULL**]
├── MG3_Grid (Position: -70.2, 0, -10.4)
│   ├── (7 children - grid tiles)
├── Spot Light [Light]
└── Object_26 [Light - Point, Intensity: 14.4, Range: 0.44]
```

---

## Critical Issues (BLOCKER)

### 1. Task 2: All 8 Devices Null

**Location:** `MG3TaskDefinition` on "Task 2 Devices" (line 30253 in scene YAML)

**Serialized Data:**
```yaml
taskName: Task2 - Group Devices
taskType: 1  # MG3TaskType.Group
devices:
- {fileID: 0}  # null
- {fileID: 0}  # null
- {fileID: 0}  # null
- {fileID: 0}  # null
- {fileID: 0}  # null
- {fileID: 0}  # null
- {fileID: 0}  # null
- {fileID: 0}  # null
slots:
- {fileID: 0}         # null
- {fileID: 1001884872} # valid
- {fileID: 0}         # null
- {fileID: 1875267182} # valid
- {fileID: 1585543483} # valid
- {fileID: 0}         # null
- {fileID: 1370326356} # valid
- {fileID: 0}         # null
```

**Impact:**
- `MiniGame3Manager.ResetTaskState()` (line 235-248 of MiniGame3Manager.cs) iterates `task.Devices` and skips all null devices
- `MG3TaskValidator.ValidateTask()` has no devices to validate against
- Task 2 is **completely non-functional** - no devices can be pushed
- 5 of 8 slots are also null, so even if devices existed, validation would fail

**Root Cause:** Device references were never assigned in Inspector, or were lost during scene reorganization.

**Fix:** Reassign all 8 device references and 5 null slot references in Inspector.

---

### 2. Task 3: All 4 Slots Null

**Location:** `MG3TaskDefinition` on "Task 3 Devices" (line 28191 in scene YAML)

**Serialized Data:**
```yaml
taskName: Task3 - Size Ordering
taskType: 2  # MG3TaskType.SizeOrdering
devices:
- {fileID: 7239904309096287468}  # valid
- {fileID: 2033363939}           # valid
- {fileID: 8412562021090031105}  # valid
- {fileID: 6994084}              # valid
slots:
- {fileID: 0}  # null
- {fileID: 0}  # null
- {fileID: 0}  # null
- {fileID: 0}  # null
```

**Impact:**
- Devices exist and can be pushed
- But `MG3TaskValidator.ValidateSizeOrdering()` cannot match devices to slots
- Task 3 can **never be solved** - validation always fails

**Root Cause:** Slot references were never assigned in Inspector.

**Fix:** Reassign all 4 slot references in Inspector.

---

## High Severity Issues

### 3. Camera FOV Clamped to 10-20°

**Location:** `MG3CameraFovLimiter` on Main Camera (line 20925 in scene YAML)

**Script:** `Assets/Scripts/MiniGames/MiniGame3/MG3CameraFovLimiter.cs`

**Serialized Data:**
```yaml
targetCamera: {fileID: 1211046238}  # Main Camera
minFov: 10
maxFov: 20
enforceEveryFrame: 1  # true
```

**Script Logic (lines 45-48):**
```csharp
private void ApplyClamp()
{
    if (targetCamera == null) return;
    targetCamera.fieldOfView = Mathf.Clamp(targetCamera.fieldOfView, minFov, maxFov);
}
```

**Impact:**
- Camera authored at FOV 60, but `LateUpdate()` clamps it to 10-20 degrees every frame
- 20-degree FOV = extreme telephoto zoom (like looking through a telescope)
- Player sees a tiny sliver of the scene
- Combined with camera position, the game view appears mostly black
- Camera FOV still breaks Game View framing, but it does not explain Scene View invisibility; the lab mesh issue is tracked separately under Lab Objects Status.

**Why Scene Appears Black:**
- Camera at position (3.63, 12.11, -5.21) looking down at 90° rotation
- With 20° FOV, only a tiny area is visible
- Most of the frame is outside the viewing frustum → renders as background color (dark blue: 0.19, 0.30, 0.47)

**Fix:** Set `minFov: 40`, `maxFov: 80` or remove the component entirely.

---

### 4. Top-Down Camera Disabled

**Location:** "Robot Top-Down Camera" (line 39022 in scene YAML)

**Components:**
- CinemachineCamera (enabled)
- CinemachinePositionComposer (enabled)
- CinemachineRotationComposer (enabled)
- CinemachineFreeLookModifier (disabled)
- CinemachineInputAxisController (enabled)

**Serialized Data:**
```yaml
m_IsActive: 0  # DISABLED
m_Priority: {m_Value: 0}  # Lowest priority
m_Lens: {FieldOfView: 20}
m_Position: {x: -0.00024, y: 6.416, z: -0.133}  # Above player
m_FollowOffset: {x: 0, y: 12, z: -5}  # 12 units up, 5 back
```

**Impact:**
- This camera was designed for top-down gameplay (natural for push puzzles)
- CinemachineBrain on Main Camera would blend to it if enabled
- With priority 0 and disabled state, it never activates
- Player is stuck with the broken FOV-limited Main Camera

**Fix:** Enable the GameObject (`m_IsActive: 1`) and set priority > 0 (e.g., 10).

---

## Medium Severity Issues

### 5. nextSceneName Empty

**Location:** `MiniGame3UiFlowController` on MG3_UI (line 30204 in scene YAML)

**Script:** `Assets/Scripts/MiniGames/MiniGame3/UI/MiniGame3UiFlowController.cs`

**Serialized Data:**
```yaml
nextSceneName:  # empty string
```

**Script Logic (lines 284-290):**
```csharp
private void OnNextClicked()
{
    if (!string.IsNullOrWhiteSpace(nextSceneName))
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
```

**Impact:**
- When player completes all 3 tasks and clicks "Next" button on results screen, nothing happens
- `string.IsNullOrWhiteSpace("")` returns true → `SceneManager.LoadScene()` never called
- Player is stuck on results screen with no way to proceed

**Fix:** Set `nextSceneName` to target scene (e.g., "MainMenu" or appropriate scene name).

---

### 6. InputAction References Null

**Location:** `MG3RobotGridMover` on Robot (lines 26074-26075 in scene YAML)

**Script:** `Assets/Scripts/MiniGames/MiniGame3/MG3RobotGridMover.cs`

**Serialized Data:**
```yaml
clickMoveAction: {fileID: 0}      # null
pointerPositionAction: {fileID: 0} # null
```

**Script Logic:**
- `clickMoveAction` (line 80-83): Used in `OnEnable()` to subscribe to `performed` callback
- `pointerPositionAction` (line 286-289): Used in `ResolvePointerScreenPosition()`

**Fallback Behavior:**
- Script has mouse fallback in `Update()` (line 156-163): `Mouse.current.rightButton.wasPressedThisFrame`
- `pointerPositionAction` null → falls back to `Mouse.current.position`
- Right-click movement still works via mouse fallback

**Impact:**
- InputAction system non-functional (no gamepad/touch support)
- Mouse movement works as fallback
- Not critical for desktop, but breaks mobile/gamepad support

**Fix:** Assign InputActionReference assets in Inspector.

---

### 7. Task 1: 1 of 4 Devices Null

**Location:** `MG3TaskDefinition` on "Task 1 Devices" (line 2574 in scene YAML)

**Serialized Data:**
```yaml
devices:
- {fileID: 8021006224877416988}  # valid
- {fileID: 3985755718284490671}  # valid
- {fileID: 2757605003283948377}  # valid
- {fileID: 0}                    # null
```

**Impact:**
- 3 of 4 devices are valid
- Task 1 may partially work depending on puzzle design
- If the null device is required for solution, task cannot be completed

**Fix:** Verify if the 4th device is intentional (3-device puzzle) or needs assignment.

---

## Low Severity Issues

### 8. Invalid Baked Lightmap Data

**Location:** Scene lighting settings (line 96 in scene YAML)

**Serialized Data:**
```yaml
m_LightingDataAsset: {fileID: 20201, guid: 0000000000000000f000000000000000, type: 0}
m_LightingSettings: {fileID: 0}  # null
```

**Impact:**
- GUID `0000000000000000f000000000000000` is Unity's default placeholder
- No real baked lighting data exists for this scene
- Scene relies on real-time lighting only
- Objects marked as "Static" expecting baked GI will render incorrectly

**Mitigation:** Lab objects have `m_StaticEditorFlags: 0` (not static), so they use real-time lighting.

**Fix:** Clear lightmap reference or rebake lighting.

---

### 9. External Material Reference

**Location:** Lab mesh at line 41424 in scene YAML

**Serialized Data:**
```yaml
m_Materials:
- {fileID: -4518484804227798339, guid: 000feaa3f88de6a44a798b841fa5ab47, type: 3}
```

**Impact:**
- All other lab meshes reference embedded sub-asset materials from `sci-fi_lab.glb`
- This one mesh references a different GUID
- May show pink/missing material at runtime if GUID doesn't resolve

**Fix:** Verify material exists in project or reassign to embedded material.

---

### 10. Point Light Extreme Settings

**Location:** "Object_26" (line 39839 in scene YAML)

**Serialized Data:**
```yaml
m_Type: 2  # Point Light
m_Color: {r: 1, g: 1, b: 1, a: 1}
m_Intensity: 14.4
m_Range: 0.43794662
m_Shadows: {m_Type: 0}  # No shadows
```

**Impact:**
- Intensity 14.4 with range 0.44 = extremely bright but tiny light
- Creates a pinpoint glow effect
- May be intentional for visual effect, but unusual

---

### 11. Post-Processing Disabled

**Location:** Main Camera's `UniversalAdditionalCameraData`

**Serialized Data:**
```yaml
m_RenderPostProcessing: 0  # DISABLED
m_Antialiasing: 0  # None
m_Dithering: 0  # Disabled
```

**Impact:**
- No bloom, color grading, ambient occlusion, or other URP effects
- Scene looks flat/unfinished
- No PostProcessVolume in scene even if enabled

**Fix:** Enable post-processing and add a Volume with profile.

---

### 12. Fog Disabled

**Location:** RenderSettings (line 96 in scene YAML)

**Serialized Data:**
```yaml
m_Fog: 0  # DISABLED
m_FogColor: {r: 0.5, g: 0.5, b: 0.5, a: 1}
m_FogMode: 3  # Exponential Squared
m_FogDensity: 0.01
```

**Impact:**
- No atmospheric fog effect
- May be intentional for this game style

---

### 13. Sun Light Not Assigned

**Location:** RenderSettings (line 96 in scene YAML)

**Serialized Data:**
```yaml
m_Sun: {fileID: 0}  # null
```

**Impact:**
- Skybox has no designated sun light for directional shadows
- Default procedural skybox used

---

### 14. vSync Disabled

**Location:** Quality Settings (PC quality level)

**Serialized Data:**
```yaml
m_VSyncCount: 0  # Don't Sync
```

**Impact:**
- Potential screen tearing unless managed by external frame limiter
- May cause visual artifacts on some displays

---

## Lighting Analysis

### Light Sources (3 Total)

| Light | Type | Color | Intensity | Shadows | Range |
|-------|------|-------|-----------|---------|-------|
| Directional Light | Directional | Warm white (255/244/214) | 1.0 | Soft | N/A |
| Spot Light | Spot | White | 2.83 | None | 10 |
| Object_26 | Point | White | 14.4 | None | 0.44 |

### Directional Light Details
- **Rotation:** X: -41.55°, Y: -391.58°, Z: -127.73°
- **Shadow Strength:** 1.0
- **Shadow Bias:** 0.05
- **Shadow Normal Bias:** 0.4
- **Lightmapping:** Mixed
- **URP:** UsePipelineSettings=1, ShadowResolutionTier=2

### Render Settings
- **Ambient Mode:** Skybox
- **Ambient Sky Color:** Dark blue-gray (0.212, 0.227, 0.259)
- **Ambient Equator Color:** Dark gray (0.114, 0.125, 0.133)
- **Ambient Ground Color:** Near black (0.047, 0.043, 0.035)
- **Skybox Material:** Default-Skybox (built-in)
- **Default Reflection Mode:** Skybox
- **Reflection Resolution:** 128

### Lightmap Settings
- **Baked Lightmaps:** Enabled
- **Realtime Lightmaps:** Disabled
- **Bake Resolution:** 40
- **Atlas Size:** 1024
- **Bake Backend:** Progressive GPU
- **PVR Sample Count:** 512
- **Mixed Bake Mode:** Shadowmask

---

## Camera Analysis

### Camera 1: Main Camera (ACTIVE)

**Component:** Camera
| Property | Value |
|----------|-------|
| Enabled | Yes |
| Clear Flags | Skybox |
| Background Color | Dark blue (0.19, 0.30, 0.47) |
| Field of View | 60.0 (clamped to 10-20 by script) |
| Near Clip Plane | 0.3 |
| Far Clip Plane | 1000 |
| Orthographic | No (Perspective) |
| Depth | 0 |
| HDR | Enabled |
| Allow MSAA | Enabled |
| Occlusion Culling | Enabled |

**Position:** (3.63, 12.11, -5.21)
**Rotation:** (90°, -62.66°, 0°) - Looking straight down

**Component:** CinemachineBrain
| Property | Value |
|----------|-------|
| Show Debug Text | Off |
| Show Camera Frustum | On |
| Update Method | Smart Update |
| Blend Update Method | Fixed Update |
| Default Blend Style | Ease In Out |
| Default Blend Time | 2 seconds |

**Component:** MG3CameraFovLimiter
| Property | Value |
|----------|-------|
| Target Camera | Main Camera |
| minFov | **10** |
| maxFov | **20** |
| enforceEveryFrame | Yes |

**URP Additional Camera Data:**
| Property | Value |
|----------|-------|
| Render Shadows | Yes |
| Render Post Processing | **NO** |
| Antialiasing | None |
| Dithering | Disabled |

---

### Camera 2: CM vcam1 (Cinemachine Virtual Camera)

**Component:** CinemachineCamera
| Property | Value |
|----------|-------|
| Active | Yes |
| Priority | 0 |
| Field of View | 60.0 |
| Near Clip Plane | 0.3 |
| Far Clip Plane | 1000 |

**Position:** (3.63, 12.11, -5.21)
**Rotation:** (67.46°, -0.72°, 0°)

**Component:** CinemachineFollow
| Property | Value |
|----------|-------|
| Binding Mode | World Space |
| Position Damping | (0.5, 0.5, 0.5) |
| Rotation Damping | (0.5, 0.5, 0.5) |
| Follow Offset | (0, 12, -5) |

**Component:** CinemachineRotationComposer
| Property | Value |
|----------|-------|
| Screen Position | (0.011, -0.010) |
| Dead Zone Enabled | Yes |
| Dead Zone Size | (0.88, 0.82) |
| Target Offset | (0, 2.92, 0) |
| Damping | (0.5, 0.5) |

---

### Camera 3: Robot Top-Down Camera (DISABLED)

**Component:** CinemachineCamera
| Property | Value |
|----------|-------|
| Active | **NO** |
| Priority | **0** |
| Field of View | **20** |
| Near Clip Plane | **0.01** |
| Far Clip Plane | 1000 |

**Position:** (-0.00024, 6.416, -0.133) - Above player
**Rotation:** (7.42°, -307.41°, 9.59°) - Nearly top-down

**Component:** CinemachinePositionComposer
| Property | Value |
|----------|-------|
| Camera Distance | **15** |
| Dead Zone Depth | 0 |
| Screen Position | (0, 0) |
| Dead Zone Enabled | No |

**Component:** CinemachineRotationComposer
| Property | Value |
|----------|-------|
| Screen Position | (0, 0) |
| Dead Zone Enabled | No |
| Target Offset | (0, -0.09, 0) |

**Tracking Target:** PlayerRoot (line 42221)

---

## Script Analysis

### Complete Script Inventory

| # | Script | Type | Status | Issues |
|---|--------|------|--------|--------|
| 1 | MG3TaskType.cs | Enum | OK | None |
| 2 | MiniGame3Phase.cs | Enum | OK | None |
| 3 | MG3GridTile.cs | MonoBehaviour | OK | None |
| 4 | MG3GridManager.cs | MonoBehaviour | OK | None |
| 5 | MG3PushableDevice.cs | MonoBehaviour | OK | None |
| 6 | MG3TargetSlot.cs | MonoBehaviour | OK | None |
| 7 | MG3Pathfinder.cs | MonoBehaviour | OK | None |
| 8 | MG3CameraFovLimiter.cs | MonoBehaviour | **ISSUE** | FOV values too restrictive |
| 9 | MG3RobotGridMover.cs | MonoBehaviour | **ISSUE** | Null InputAction fallbacks; push animation wired to non-existent trigger; Push clip not imported |
| 10 | MG3PushController.cs | MonoBehaviour | OK | None |
| 11 | MG3TaskValidator.cs | MonoBehaviour | OK | Logic correct, data missing |
| 12 | MG3TaskDefinition.cs | MonoBehaviour | **ISSUE** | Null device/slot references |
| 13 | MiniGame3Manager.cs | MonoBehaviour | OK | Depends on Task data |
| 14 | MiniGame3UiFlowController.cs | MonoBehaviour | **ISSUE** | Empty nextSceneName |
| 15 | MG3DebugHudPlaceholder.cs | MonoBehaviour | OK | None |
| 16 | MG3Scaffolder.cs | Static class (Editor) | OK | None |

---

### Script Details

#### MG3GridManager.cs
**Purpose:** Manages the grid system, tile registry, and occupancy tracking.

**Key Fields:**
- `gridRoot`: Transform parent for grid tiles
- `origin`: World position offset
- `cellSize`: 2.9f (world units per cell)
- `gridWidth`: 15, `gridHeight`: 17

**Key Methods:**
- `BuildRegistry()`: Scans children for MG3GridTile components
- `RegisterOccupant()`: Tracks what's on each cell
- `MoveOccupant()`: Updates occupancy when objects move
- `IsWalkable()`: Checks if cell is free and walkable

**Dependencies:** MG3GridTile, MG3PushableDevice

---

#### MG3PushableDevice.cs
**Purpose:** Represents a pushable device on the grid.

**Key Fields:**
- `deviceId`: Unique identifier
- `groupId`: Group identifier for group puzzles
- `sizeRank`: Size order for ordering puzzles
- `locked`: Whether device can be pushed
- `currentCoordinate`: Current grid position
- `startingCoordinate`: Reset position

**Key Methods:**
- `SetLocked()`: Locks/unlocks device
- `ResetToTaskStart()`: Resets to starting position
- `ValidateOccupancyConsistency()`: Self-healing occupancy

**Dependencies:** MG3GridManager

---

#### MG3RobotGridMover.cs
**Purpose:** Controls robot movement on the grid.

**Key Fields:**
- `gridManager`, `pathfinder`: Dependencies
- `raycastCamera`: Camera for raycasting
- `clickMoveAction`, `pointerPositionAction`: Input actions (NULL)
- `floorLayer`: Layer mask for floor detection
- `moveSpeed`: 3.5f
- `rotationSpeedDegrees`: 540f

**Animation Fields:**
- `robotAnimator`: Assigned to Robot Variant Variant ✅
- `walkingBoolParameter`: "IsWalking" — exists in controller ✅
- `pushingTriggerParameter`: "Push" — **DOES NOT EXIST in controller** (Trigger param missing, but gracefully falls through)
- `pushingBoolParameter`: "IsPushing" — exists in controller ✅

**Push Animation Issue:**
- `SetPushingAnimation(true)` sets `IsPushing = true` → Animator AnyState → Push state transition works ✅
- However, the Push state's motion references `Push (1).fbx` which has **no extracted animation clips** (`clipAnimations: []` in its meta file). The Animator transitions to Push but plays nothing.
- Compare: Idle (`Sad Idle With Skin.fbx`) and Walk (`Walking without sking.fbx`) both have `clipAnimations` with a "mixamo.com" clip. Push has `clipAnimations: []`.
- Fix: Reimport `Assets/Mini Game 3/Animation/Push (1).fbx` with proper clip extraction settings.

**Key Methods:**
- `TryRequestMove()`: Pathfind and start movement
- `MovePath()`: Coroutine for smooth movement
- `ProcessClick()`: Handles click input

**Events:**
- `DestinationRejected`, `DestinationReached`, `DestinationRequested`, `MovementStarted`

**Dependencies:** MG3GridManager, MG3Pathfinder, MG3PushableDevice

---

#### MG3PushController.cs
**Purpose:** Handles pushing devices.

**Key Fields:**
- `gridManager`, `robotMover`: Dependencies
- `interactAction`: Input action for push
- `pushDuration`: 0.2f
- `prePushTurnDelay`: 0.06f

**Key Methods:**
- `TryPushFromCurrentPosition()`: Attempts push from robot position
- `HasValidPushCandidate()`: Checks if push is possible
- `PushRoutine()`: Coroutine for push animation

**Events:**
- `PushCompleted`, `PushStarted`, `PushRejected`

**Dependencies:** MG3GridManager, MG3RobotGridMover, MG3PushableDevice

---

#### MG3TaskDefinition.cs
**Purpose:** Defines a task with devices and slots.

**Key Fields:**
- `taskName`: Display name
- `taskType`: Exact/Group/SizeOrdering
- `devices`: Array of MG3PushableDevice (some NULL)
- `slots`: Array of MG3TargetSlot (some NULL)

**Status:** ISSUE - Many null references

---

#### MG3TaskValidator.cs
**Purpose:** Validates task completion.

**Key Methods:**
- `ValidateTask()`: Checks if devices match slots
- `LockSolvedDevices()`: Locks correctly placed devices

**Validation Types:**
- `ValidateExact`: Matches device.DeviceId == slot.RequiredDeviceId
- `ValidateGroup`: Matches device.GroupId == slot.RequiredGroupId
- `ValidateSizeOrdering`: Matches device.SizeRank == slot.RequiredSizeRank

**Dependencies:** MG3TaskDefinition, MG3PushableDevice, MG3TargetSlot

---

#### MiniGame3Manager.cs
**Purpose:** Main game controller.

**Key Fields:**
- `gridManager`, `robotMover`, `pushController`, `taskValidator`: Dependencies
- `tasks`: Array of MG3TaskDefinition
- `autoStartOnSceneLoad`: false
- `settleDelaySeconds`: 0.2f

**Key Methods:**
- `StartMiniGame()`: Resets and starts task 0
- `ResetCurrentTask()`: Resets active task
- `StartTask()`: Begins specific task
- `SetGameplayInputEnabled()`: Locks/unlocks input
- `ReloadCurrentScene()`: Reloads scene

**Events:**
- `PhaseChanged`, `TaskStarted`, `TaskCompleted`, `TaskReset`, `Feedback`, `MiniGameCompleted`, `StatsChanged`

**Dependencies:** All other MG3 scripts

---

#### MiniGame3UiFlowController.cs
**Purpose:** Manages UI flow and user interaction.

**Key Fields:**
- `manager`, `pushController`: Dependencies
- `instructionCanvasRoot`, `taskCanvasRoot`, `resultCanvasRoot`: UI canvases
- `beginButton`, `retryButton`, `nextButton`: UI buttons
- `task1Panel`, `task2Panel`, `task3Panel`: Task-specific panels
- `nextSceneName`: **EMPTY**

**Key Methods:**
- `BeginFlow()`: Starts game after instruction
- `OnFeedback()`: Shows user messages
- `OnMiniGameCompleted()`: Shows results
- `CalculateGrade()`: A (0 resets), B (1-2), C (3-4), D (5+)

**Dependencies:** MiniGame3Manager, MG3PushController, TMPro, UnityEngine.UI

---

#### MG3CameraFovLimiter.cs
**Purpose:** Clamps camera FOV to min/max range.

**Key Fields:**
- `targetCamera`: Camera to limit
- `minFov`: 10 (**TOO LOW**)
- `maxFov`: 20 (**TOO LOW**)
- `enforceEveryFrame`: true

**Key Methods:**
- `ApplyClamp()`: `Mathf.Clamp(targetCamera.fieldOfView, minFov, maxFov)`

**Called from:** `LateUpdate()` every frame

---

#### MG3Pathfinder.cs
**Purpose:** BFS pathfinding on grid.

**Key Methods:**
- `TryFindPath()`: Finds path from start to goal using BFS
- Uses 4-directional neighbors
- Returns reconstructed path

**Dependencies:** MG3GridManager

---

#### MG3GridTile.cs
**Purpose:** Data holder for grid tiles.

**Key Fields:**
- `coordinate`: Vector2Int
- `walkable`: bool
- `markAsDeadlockRisk`: bool
- `tileColor`: Color

---

#### MG3TargetSlot.cs
**Purpose:** Target location for pushable devices.

**Key Fields:**
- `coordinate`: Vector2Int
- `requiredDeviceId`, `requiredGroupId`, `requiredSizeRank`: Match criteria
- `isSolved`: bool
- `indicatorRenderer`: Visual indicator
- `unsolvedColor`, `solvedColor`: Visual states

---

#### MG3DebugHudPlaceholder.cs
**Purpose:** Debug HUD display.

**Key Fields:**
- `manager`: MiniGame3Manager reference
- `tmpStatusText`: TextMeshPro text
- `legacyStatusText`: Legacy Unity Text
- `mirrorToConsole`: bool

---

#### MG3Scaffolder.cs (Editor Only)
**Purpose:** Scene setup utility.

**Key Methods:**
- `SetupSceneRoots()`: Creates/organizes scene hierarchy
- Menu item: `Tools/MG3/Setup Scene Roots`

---

## Lab Objects Status

### Source Asset
- **File:** `Assets/Mini Game 3/Assets/sci-fi_lab.glb`
- **GUID:** `1f55a78e3ee224842a1c913e39de4f44`

### Mesh Status
- **All lab meshes properly referenced** from the GLB file
- Materials are embedded sub-asset materials within the GLB
- References use `type: 3` (asset reference)

### Material Status
- **27+ material references** found in scene YAML
- All reference embedded sub-asset materials from `sci-fi_lab.glb`
- **One exception:** Line 41424 references external GUID `000feaa3f88de6a44a798b841fa5ab47`

### Visibility Issue
- Lab root object `sci-fi_lab` **is active in the scene hierarchy**, but the lab geometry is not visible in Scene View or Game View.
- Current inspection shows 33 active child `MeshRenderer` components under `sci-fi_lab`, but all 33 child `MeshFilter.sharedMesh` references are null.
- Renderer bounds are zero-size, which means Unity has active renderer components but no mesh data to draw.
- The source file exists at `Assets/Mini Game 3/Assets/sci-fi_lab.glb`, but Unity currently reports it as a `DefaultAsset`, not as an imported model asset.
- This is not only a camera/FOV issue. The camera FOV problem can make Game View worse, but Scene View invisibility points to missing/unimported lab meshes.

Likely causes:
- GLB importer support is missing or not active in the project.
- The GLB import failed or was downgraded to `DefaultAsset`.
- Scene mesh references were lost after asset/package changes.

Fix direction:
1. Restore GLB model import support, likely by installing/enabling the required glTF/GLB importer package.
2. Reimport `Assets/Mini Game 3/Assets/sci-fi_lab.glb`.
3. Verify `sci-fi_lab` child `MeshFilter.sharedMesh` references are no longer null.
4. Only after meshes are visible in Scene View, continue with camera FOV/top-down camera fixes for Game View framing.

### Collider Status
- Many colliders have `m_Material: {fileID: 0}` (default physics material)
- This is normal and expected

---

## Recommendations

### Priority 1: Fix Camera (Immediate Visual Improvement)

1. **Option A (Quick):** Set `MG3CameraFovLimiter.minFov = 40`, `maxFov = 80`
2. **Option B (Better):** Enable "Robot Top-Down Camera" and set priority to 10
3. **Option C (Best):** Both - fix FOV limiter AND enable top-down camera

### Priority 2: Fix Task 2 (Critical for Gameplay)

1. Find all 8 MG3PushableDevice objects in scene
2. Assign them to Task 2 Devices' `devices` array
3. Find all 8 MG3TargetSlot objects
4. Assign them to Task 2 Devices' `slots` array

### Priority 3: Fix Task 3 (Critical for Gameplay)

1. Find all 4 MG3TargetSlot objects
2. Assign them to Task 3 Devices' `slots` array

### Priority 4: Fix UI Flow

1. Set `nextSceneName` to appropriate scene name
2. Verify retry button works

### Priority 5: Polish

1. Enable post-processing on Main Camera
2. Add Volume with profile for bloom/color grading
3. Fix external material reference (line 41424)
4. Consider enabling fog for atmosphere
5. Reassign InputAction references for full input support

---

## Appendix: Scene File Line References

| Object | Line | Notes |
|--------|------|-------|
| Directional Light | 230414 | Main light source |
| sci-fi_lab | 231236 | Lab environment |
| MG3_Root | 229946 | Game root |
| MG3_UI | 230798 | UI system |
| MG3_Tasks | 229962 | Task definitions |
| MG3_Grid | 230520 | Grid system |
| Main Camera | 230458 | Active camera |
| Robot Top-Down Camera | 39022 | **DISABLED** |
| Task 1 Devices | 2574 | 1 null device |
| Task 2 Devices | 30253 | **ALL DEVICES NULL** |
| Task 3 Devices | 28191 | **ALL SLOTS NULL** |
| MG3CameraFovLimiter | 20925 | FOV: 10-20 |
| RenderSettings | 96 | Invalid lightmap |

---

## Appendix: Dependency Graph

```
MG3TaskType (enum)
  └── MG3TaskDefinition
        └── MG3TaskValidator
              └── MiniGame3Manager
                    ├── MG3RobotGridMover
                    │     └── MG3Pathfinder
                    │           └── MG3GridManager
                    │                 └── MG3GridTile
                    ├── MG3PushController
                    │     └── MG3PushableDevice
                    └── MiniGame3UiFlowController
                          └── MG3DebugHudPlaceholder

MG3TargetSlot ─── MG3TaskDefinition
MG3CameraFovLimiter ─── (standalone)
MG3Scaffolder ─── (Editor utility)
```

---

**End of Report**
