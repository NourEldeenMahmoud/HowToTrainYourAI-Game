# UnityYAMLMerge Setup Reference

This file documents the Unity merge setup used by the project.

It is not gameplay logic, but it is important because the project uses Unity scenes, prefabs, ScriptableObjects, and `.meta` files that are easy to break with normal text merges.

---

## 1. The Problem

Unity stores many project files as YAML:

```text
.unity
.prefab
.asset
.anim
.controller
.meta
```

These files contain IDs and references such as:

```text
fileID
guid
type
serialized field values
scene object references
prefab overrides
```

If two people edit the same scene or prefab, a normal Git text merge can break:

- Inspector references.
- Component links.
- Prefab overrides.
- Scene object IDs.
- ScriptableObject references such as `RobotStats_Main` or learning profiles.

---

## 2. The Solution

Use Unity's Smart Merge tool:

```text
UnityYAMLMerge
```

The project includes:

```text
.gitattributes
setup-git.ps1
```

`.gitattributes` tells Git which files should use the Unity merge driver.

Example rules:

```text
*.unity   merge=unityyamlmerge eol=lf
*.prefab  merge=unityyamlmerge eol=lf
*.asset   merge=unityyamlmerge eol=lf
*.anim    merge=unityyamlmerge eol=lf
*.meta    merge=unityyamlmerge eol=lf
```

`setup-git.ps1` registers the merge driver on each developer machine.

---

## 3. Team Setup

Every teammate should run this once after pulling the project:

```powershell
.\setup-git.ps1
```

The script locates Unity and configures Git to use `UnityYAMLMerge`.

This setup is per machine, not per repository clone only.

---

## 4. Current Project Files That Benefit From This

The project now has many serialized references that should be protected from bad merges:

```text
MiniGame1Manager references
DriftChallenge references
CameraAlignmentChallenge references
SpeedConsistencyChallenge references
MiniGame1ResultScreenUI references
RobotStabilityApplier references
RobotStats_Main ScriptableObject
MiniGame1LearningProfileSO assets
ControlManager camera/input/UI references
MG1ToMG2FlowCoordinator references
MiniGame2 scene/grid/UI references
```

Because the post-MG1 fault settings now live in `RobotStabilityApplier`, scene/prefab merges must preserve that component's serialized fields:

```text
Robot Stats
Robot Movement
Robot Camera
Min/Max Fault Check Interval Seconds
Drift Fault Yaw Deg
Drift Fault Duration Seconds
Sprint Block Duration Seconds
Camera Fault Pitch Offset Deg
Camera Fault Duration Seconds
```

---

## 5. Good Team Practice

- Avoid multiple people editing the same scene at the same time when possible.
- Prefer prefab variants or separate scenes for parallel work.
- Pull before editing shared scenes.
- Commit related scene + script changes together when they depend on serialized fields.
- After merge, open Unity and check the Inspector for missing references.
- Always verify important components like `RobotStabilityApplier`, `MiniGame1Manager`, and `ControlManager` after scene merges.

---

## 6. When A Merge Still Fails

If UnityYAMLMerge cannot solve a conflict:

1. Do not blindly accept both sides.
2. Open the conflicted `.unity` or `.prefab` carefully.
3. Prefer resolving through Unity Editor if possible.
4. Check missing references in the Inspector after resolution.
5. Run a build/compile check after resolving.

---

## 7. Current Status

The project includes the required files.

The remaining action for each teammate is:

```powershell
.\setup-git.ps1
```

Run it once per machine.
