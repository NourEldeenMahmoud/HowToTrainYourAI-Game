---
dg-publish: true
---

# مرجع تقدّم المشروع — How To Train Your AI

ملف مرجعي يُحدَّث عند إضافة ميزات جديدة.

| | |
|---|---|
| **آخر تحديث للملف** | **17 أبريل 2026** |
| **آخر مسح أولي** | 23 مارس 2026 |

**GitHub:** [HowToTrainYourAI-Game](https://github.com/NourEldeenMahmoud/HowToTrainYourAI-Game) — البرانش النشط: **`MiniGame1/Nour`**

---

## 1. نظرة عامة

| البند | القيمة |
|--------|--------|
| اسم المنتج في Unity | How To Train Your AI |
| إصدار المحرك | Unity **6000.3.8f1** (Unity 6) |
| إدارة نسخ | Git + Git LFS — merge driver: **UnityYAMLMerge** (انظر §13) |

---

## 2. الحزم الرئيسية (`Packages/manifest.json`)

- **Universal Render Pipeline (URP)** — `com.unity.render-pipelines.universal` 17.3.0
- **Input System** — `com.unity.inputsystem` 1.18.0
- **AI Navigation** — `com.unity.ai.navigation` 2.0.10
- **Cinemachine** — `com.unity.cinemachine` 3.1.6
- **glTFast** — `com.unity.cloud.gltfast` 6.16.1
- **UGUI، Timeline، Visual Scripting، Test Framework**
- **IDE**: Rider و Visual Studio

---

## 3. المشاهد (`Assets/Scenes`)

| المشهد | الغرض |
|--------|--------|
| `Nour/Nour.unity` | مشهد التطوير الرئيسي (النشط حاليًا) |
| `Main Scene.unity` | المشهد العام المشترك للفريق |
| `Mini Game 1.unity` | مشهد MiniGame1 المخصص |
| `Aya/Temp Main Scene.unity` | مشهد مؤقت للتجربة (آية) |
| `_Recovery/` | نسخ استرداد Unity — لا تُعدَّل يدويًا |

**ملاحظة Post Processing:** في URP الكاميرا الحاملة للـ `CinemachineBrain` لازم يكون عليها **Render Post Processing** مفعّل في `Universal Additional Camera Data`، وإلا الـ Volumes لن تظهر في Game View.

---

## 4. الإدخال (`Assets/Inputs`)

| الملف | Map / إجراءات | ملاحظات |
|--------|----------------|---------|
| `Global.inputactions` | **Switch / Switch** | ربط: **Tab** — تبديل اللاعب↔الروبوت عبر `ControlManager` |
| `PlayerMovment.inputactions` | **Player** — Move (Vector2)، Sprint (Button)، Interact (Button) | WASD، Shift، **E** |
| `RobotMovment.inputactions` | **Robot** — Move، Sprint | نفس أسلوب اللاعب |

---

## 5. السكريبتات (`Assets/Scripts`)

### 5.1 `Managers/ControlManager.cs`

**الدور:** إدارة التحويل بين تحكم اللاعب والروبوت، أولويات الكاميرا، FX Pod، قفل الإدخال.

| حقل / خاصية | الوظيفة |
|-------------|---------|
| `playerInput` / `robotInput` | مكوّن `PlayerInput` على كل منهما |
| `switchAction` | مرجع `Global → Switch/Switch` (Tab) |
| `playerCamera` / `robotCamera` | `CinemachineCamera` — تبديل بـ Priority |
| `podFxVolume` | `Volume` لـ post-processing عند التحكم بالروبوت |
| `fxStartDelay` / `fxBlendSpeed` | تأخير وسرعة blend الـ FX |
| `podUiRoot` | جذر واجهة الـ Pod Canvas |
| `timeText` | `TMP_Text` لعرض الوقت `HH:mm:ss` |
| `IsPlayerControlActive` | `bool` — هل اللاعب هو المتحكَّم به؟ |
| `IsInputLocked` | `bool` — هل الإدخال مقفول كليًا؟ |
| `IsPlayerLookSuppressed` | `bool` — هل حركة الكاميرا للاعب معطّلة؟ (`IsInputLocked \|\| messageBlocksPlayerControls`) |
| `LockInput()` / `UnlockInput()` | قفل/فتح كل الإدخال وإظهار/إخفاء المؤشر |

### 5.2 `PlayerMovment/PlayerMovement.cs`

- **الكلاس:** `PlayerMovementAdvanced`
- **المتطلبات:** `CharacterController`
- **الإدخال:** `OnMove`, `OnSprint` (Send Messages)
- **السلوك:** حركة نسبية للكاميرا، مشي/جري، جاذبية، Animator (`IsWalking`, `IsSprinting`)، قفل المؤشر.

### 5.3 `Robot/RobotMovment.cs`

- **الكلاس:** `RobotMovement`
- **المتطلبات:** `CharacterController`
- **الإدخال:** `OnMove`, `OnSprint`
- **السلوك:** حركة نسبية للكاميرا، دوران بصري للروبوت، `applyRootMotion = false`.

### 5.4 `Robot/RobotCameraLookInput.cs` *(جديد)*

- **الدور:** يُشغَّل على `Robot FreeLook Camera` — يقرأ `Mouse.current.delta` مباشرةً ويُحرّك `CinemachineOrbitalFollow` (HorizontalAxis / VerticalAxis).
- **لماذا؟** `CinemachineInputAxisController` كان يسبب zoom بدلًا من orbit بسبب `OrbitStyle = ThreeRing`.
- **يتوقف عن العمل إذا:** اللاعب هو المتحكَّم به (`IsPlayerControlActive`) أو الإدخال مقفول أو المؤشر ظاهر.
- **Inspector:** `horizontalSensitivity` (افتراضي 0.15)، `verticalSensitivity` (افتراضي 0.10).

### 5.5 `PlayerMovment/PlayerCameraLookInput.cs` *(جديد)*

- **الدور:** نفس `RobotCameraLookInput` لكن للاعب — يعمل فقط عندما `IsPlayerControlActive = true` ولم تُكبَّت الكاميرا.
- **Inspector:** `horizontalSensitivity`، `verticalSensitivity`.

### 5.6 `PlayerMovment/PlayerInteractor.cs`

- **الدور:** Raycast من كاميرا اللاعب، عرض نص التلميح، تنفيذ التفاعل عند **E**.
- **المراجع:** `playerCamera`, `interactPromptText` (TMP)، `interactDistance`, `interactLayers`.

### 5.7 `SimpleInteractable.cs` / `MessageInteractable.cs`

| الكلاس | الوظيفة |
|--------|---------|
| `SimpleInteractable` | نقطة تفاعل بسيطة — `Interact()` يطلق حدث |
| `MessageInteractable` | يعرض رسالة نصية عند التفاعل (Canvas/Panel) |

### 5.8 `IntroRobotController.cs`

- **الدور:** تشغيل تسلسل الـ Intro قبل بدء MiniGame1: إظهار رسائل الروبوت، انتظار إغلاقها، ثم إطلاق الـ MiniGame.
- **الربط الرئيسي:** `robotMessagesCanvas`، `closeButton` (يبحث عنه تلقائيًا بالتسمية أو الموضع إن لم يُحدَّد)، `miniGame1Trigger`.
- **ملاحظة:** `ResolveRobotMessagesCanvasRefs()` فيها fallback متعدد المراحل للبحث عن زر الإغلاق (بالمسار → بالنص → بالموضع).

### 5.9 `MiniGames/MiniGame1/MiniGame1Manager.cs`

- **الدور:** تشغيل تسلسل MiniGame1 (FreeMoveInitial → DriftLeft → DriftRight → CameraAlignment → SpeedConsistency → Completed).
- **الفيزات:** `PhaseChanged` (event)، `MiniGameCompleted` (event)، `LogMessage` (event — يُطلق كل رسالة log للـ UI).
- **المراجع:** `learningProfile`، `robotStats`، `trackProgress`، `driftLeft/Right`، `cameraAlignment`، `speedConsistency`.
- **الخيارات:** `enableLogging` (يطبع في Console **ويُرسل** لـ `LogMessage`)، `autoStart`، `initialFreeMoveSeconds`، `freeMoveBetweenChallengesSeconds`.

### 5.10 `MiniGames/MiniGame1/UI/MiniGame1RobotPovUI.cs`

- **الدور:** عرض حالة MiniGame1 على شاشة POV الروبوت في الوقت الفعلي.
- **الربط:** `miniGame1Manager` (يجده تلقائيًا إن لم يُحدَّد)، `povUiRoot` (Canvas الـ POV).
- **العناصر التي يملؤها تلقائيًا** (`autoFind = true`):

| عنصر UI | مسار البحث |
|---------|------------|
| `challengeNameText` | `Objective Texts → Instruction Text` |
| `promptText` | `Error Message → Text (TMP)` |
| `logText` | `Status Messages → Text (TMP)` |
| `clockText` | `TimerText → Text (TMP)` |

- **سلوك الـ Log:** يستمع على `MiniGame1Manager.LogMessage`، يُلحق كل رسالة، يُقطع السطور الطويلة بـ `…` عند `maxCharsPerLine` (افتراضي 28)، يحتفظ بآخر `maxLogLines` سطر (افتراضي 6) — يمسح عند بدء اللعبة (`FreeMoveInitial`).

### 5.11 `MiniGames/MiniGame1/UI/MiniGame1ResultScreenUI.cs`

- **الدور:** عرض نتائج MiniGame1 بعد الانتهاء.
- **يملأ تلقائيًا:** نصوص الدرجات، شريط التقدم، `tierText` (Pass/Fail).
- **الأزرار:**
  - **Apply Improvements** — يُخفي الشاشة ويفتح الإدخال (تابع اللعبة).
  - **Recalibrate Movement** — يُعيد تشغيل MiniGame1 من البداية.

### 5.12 `MiniGames/MiniGame1/Integration/StartMiniGame1OnRobotControl.cs`

- **الدور:** يستمع على `ControlManager` — يبدأ MiniGame1 تلقائيًا عندما ينتقل التحكم للروبوت.

---

## 6. الـ Prefabs (`Assets/prefabs`)

| الـ Prefab | التغييرات الأخيرة |
|-----------|-------------------|
| `Player/Player.prefab` | أُضيف `PlayerCameraLookInput`، `OrbitStyle` → Sphere، عُطِّل `Look Orbit X` في `CinemachineInputAxisController` |
| `Robot/Base Robot.prefab` | أُضيف `RobotCameraLookInput`، `OrbitStyle` → Sphere، عُطِّل `Look Orbit X` |
| `UI Prefabs/Result Screen prefab/Result Screen Canvas.prefab` | شاشة نتائج MiniGame1 مع درجات وأزرار |
| `UI Prefabs/Robot pov prefabs/Robot POV Canvas.prefab` | HUD الروبوت: Instruction Text، Error Message، Status Messages، TimerText |

---

## 7. الأنيميشن والتحكم

- **Animator اللاعب:** `AnimationControllers/Player.controller`
- **Animator الروبوت:** `AnimationControllers/MovmentController.controller`
- **أنيميشن اللاعب:** `Animations/Player/` — `PlayerWalk.anim`, `PlayerRun.anim`
- **أنيميشن الروبوت:** `Animations/Robot/`

---

## 8. الأصول والإعدادات

- **`Imported Assets/`:** مواد، نسيج، نماذج بيئية.
- **`Assets/Settings/`:** URP (`PC_RPAsset`, `Mobile_RPAsset`, `DefaultVolumeProfile`).
- **`Assets/UI/`:** Sprites وعناصر واجهة المستخدم.

---

## 9. إعداد سريع في المشهد (Checklist)

**تبديل اللاعب ↔ الروبوت**
- `ControlManager` مع ربط `playerInput`, `robotInput`, `switchAction`, كاميرتان Cinemachine، `podFxVolume`, `podUiRoot`, `timeText`.
- `PlayerInput` اللاعب: Actions = `PlayerMovment`، Default Map = Player.
- `PlayerInput` الروبوت: Actions = `RobotMovment`، Default Map = Robot.

**كاميرا الحركة الحرة**
- على `Player FreeLook Camera`: مكوّن `PlayerCameraLookInput`.
- على `Robot FreeLook Camera`: مكوّن `RobotCameraLookInput`.
- `CinemachineOrbitalFollow.OrbitStyle` = **Sphere** على الاثنين.
- `CinemachineInputAxisController` → عطّل `Look Orbit X`.

**MiniGame1**
- `MG1_Manager` فيه: `MiniGame1Manager`، `MiniGame1RobotPovUI` (اربط `miniGame1Manager` + `povUiRoot`)، `StartMiniGame1OnRobotControl`، `MiniGame1ResultScreenUI`.
- Intro: `IntroRobotController` مع `robotMessagesCanvas` و`miniGame1Trigger`.

**التفاعل (Interact)**
- على اللاعب: `PlayerInteractor` + ربط الكاميرا و`Interaction Text`.
- على الهدف: `SimpleInteractable` أو `MessageInteractable` + Collider.

---

## 10. ما تم إنجازه (ملخص)

1. مشروع Unity 6 مع URP، Input System، Cinemachine 3.
2. حركة لاعب وروبوت بـ `CharacterController` وربط أنيميشن.
3. نظام تبديل تحكم عالمي بـ **Tab** عبر `ControlManager`.
4. تأثيرات Pod عبر Volume (blend + تأخير) وواجهة Pod.
5. عرض الوقت الحقيقي على UI.
6. نظام Interact: Raycast، تلميح نصي، ضغط E.
7. **Intro Sequence:** رسائل الروبوت قبل MiniGame1 عبر `IntroRobotController`.
8. **MiniGame1 كاملًا:**
   - تسلسل تحديات (Drift × 2، Camera Alignment، Speed Consistency).
   - HUD الروبوت مع Instruction/Error/Log/Clock يتحدّث تلقائيًا مع المراحل.
   - شاشة نتائج مع درجات وتير ومؤشرات وزرارَي Continue/Restart.
   - Log يُعرض في الـ Status Messages بسلوك terminal scrolling.
9. **إصلاح كاميرا الحركة الحرة:** سكريبتات مخصصة تقرأ mouse delta مباشرةً وتتجاوز مشكلة Zoom في CinemachineInputAxisController.
10. **إعداد Git للفريق:** `.gitattributes` + `setup-git.ps1` لتسجيل UnityYAMLMerge كـ merge driver.

---

## 11. اقتراحات لمتابعة التوثيق

- عند إضافة **ML-Agents**: سجّل إصدار الحزمة، مسارات YAML، وأسماء الـ Behaviors.
- عند كل ميزة كبيرة: حدّث **آخر تحديث للملف** في أعلى الجدول.
- عند إضافة MiniGame2+: أضف قسمًا مشابهًا لـ §5.9–5.11.

---

## 12. هيكل المجلدات الرئيسية

```text
Assets/
├── Inputs/                   # Input Actions files
├── Prefabs/
│   ├── Player/
│   ├── Robot/
│   └── UI Prefabs/
│       ├── Result Screen prefab/
│       └── Robot pov prefabs/
├── Scenes/
│   ├── Nour/Nour.unity       ← مشهد التطوير النشط
│   ├── Main Scene.unity
│   ├── Mini Game 1.unity
│   └── Aya/
├── Scripts/
│   ├── Managers/             # ControlManager
│   ├── PlayerMovment/        # PlayerMovement, PlayerInteractor, PlayerCameraLookInput
│   ├── Robot/                # RobotMovment, RobotCameraLookInput
│   ├── MiniGames/MiniGame1/
│   │   ├── MiniGame1Manager.cs
│   │   ├── Challenges/
│   │   ├── UI/               # MiniGame1RobotPovUI, MiniGame1ResultScreenUI
│   │   └── Integration/      # StartMiniGame1OnRobotControl
│   ├── IntroRobotController.cs
│   ├── SimpleInteractable.cs
│   └── MessageInteractable.cs
└── Settings/                 # URP assets
```

---

## 13. إعداد Git للفريق (UnityYAMLMerge)

لحل مشاكل تلغبط الـ Inspector وكسر الـ references عند الـ merge:

**مرة واحدة على كل جهاز:**

```powershell
.\setup-git.ps1
```

- السكريبت يجد `UnityYAMLMerge.exe` تلقائيًا من `Library/EditorInstance.json`.
- يُسجّله كـ merge driver في الـ global git config.
- الـ `.gitattributes` يُطبّقه تلقائيًا على `*.unity`, `*.prefab`, `*.asset`, `*.anim`, `*.meta`, إلخ.

---

*هذا الملف لا يستبدل التعليقات داخل الكود؛ يهدف ليكون خريطة مرجعية للمشروع.*
