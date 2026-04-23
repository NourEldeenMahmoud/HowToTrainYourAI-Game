---
dg-publish: true
---

# Mini-Game 1 — Control Calibration Flow

> **ملف مرجعي** لفريق العمل: يشرح فلو الميني جيم الأولى مرتين — مرة بلغة بسيطة للكل، ومرة تقنية للمبرمجين.

---

# الجزء الأول — الفلو ببساطة (للفريق كله)

## القصة بإيجاز

اللاعب بيفتح صندوق الروبوت ويحاول يشغل رسالة جده لأول مرة.
السيستم بيمنعه — لأن الروبوت محتاج **Calibration** قبل ما يتحكم فيه.
اللاعب بيتدرب على التحكم في ممر الجنينة، والروبوت في نفس الوقت بيتعلم من تصرفاته.

---

## الخطوات خطوة بخطوة

### 🔑 خطوة 0 — الدخول على الروبوت
اللاعب بيضغط **Tab** فبيدخل على كاميرا الروبوت.
أول ما يدخل، بيلاقي الـ POV UI محدود — مش بيشوف كل حاجة — ومفيش موفمنت.
رسالة بتظهر في الـ Messages Canvas بتقوله:
> "لازم تظبط اعدادات الموفمنت الأول عشان تقدر تشوف الرسالة"

في الـ Canvas في زرارين: **Configure** و**Close**.
اللاعب بيضغط **Configure** فبيتفتح الـ POV كاملاً وبتبدأ الميني جيم.

---

### 🟢 مرحلة 1 — Free Move (تعلّم الحركة الأساسية)

- الروبوت بيتحرك بشكل طبيعي.
- السيستم بيقيس مدى دقة اللاعب في الحفاظ على المسار.
- المدة: قابلة للضبط (افتراضي ~10 ثوانٍ).
- مفيش تحدٍّ هنا — بس بداية لجمع البيانات.

---

### 🔴 مرحلة 2 — Drift يسار (تحدي الانحراف الأول)

- **اللي بيحصل:** الروبوت فجأة بيبدأ ينحرف 45° ناحية الشمال.
- **المطلوب:** اللاعب يعوّض بالتحرك ناحية اليمين.
- **اللي بيتقاس:**
  - دقة التصحيح (كام درجة الفرق بعد ما صحّح؟)
  - سرعة الاستجابة (كام ثانية أخد قبل ما يبدأ يصحح؟)
- المدة: ~4 ثوانٍ.
- بعدها: **Free Move** قصير قبل التحدي التاني.

---

### 🔴 مرحلة 3 — Drift يمين (تحدي الانحراف التاني)

- **اللي بيحصل:** الروبوت ينحرف 45° ناحية اليمين.
- **المطلوب:** التحرك ناحية الشمال للتصحيح.
- **اللي بيتقاس:** نفس المرحلة السابقة.
- النتيجتين بيتحسبوا متوسط لـ **Drift Score** واحد.
- بعدها: **Free Move** قصير.

---

### 🎥 مرحلة 4 — Camera Alignment (تحدي الكاميرا)

- **اللي بيحصل:** الكاميرا بتتحرك لفوق (+25° تقريباً).
- **المطلوب:** اللاعب يرجعها للزاوية الطبيعية (0°).
- **اللي بيتقاس:**
  - متوسط خطأ الزاوية طول الفترة.
  - سرعة الاستجابة.
- بعدها: **Free Move** قصير.

---

### ⚡ مرحلة 5 — Speed Consistency (تحدي الثبات في السرعة)

- **اللي بيحصل:** السرعة بتبدأ تتذبذب (±15% تقريباً).
- **المطلوب:** اللاعب يحافظ على سرعة ثابتة.
- **اللي بيتقاس:** الانحراف المعياري للسرعة (Standard Deviation).
  - كلما قل → درجة أعلى.
- المدة: ~5 ثوانٍ.

---

### 🏁 مرحلة 6 — النتيجة

بعد انتهاء كل التحديات:
- الشاشة بتتوقف.
- **Result Screen** بيظهر فيه:
  - **Final Score** من 100.
  - **الـ Tier**: Excellent / Good / Average / Fail.
  - درجة كل تحدٍّ بشكل منفصل مع بار بياني.
  - زرارين: **Apply Improvements** (تكمّل) أو **Recalibrate Movement** (تعيد).

---

### 🤖 بعد النتيجة — تأثير على الروبوت

| الـ Tier | اللي بيحصل للروبوت بعدين |
|----------|--------------------------|
| Excellent (90+) | استقرار ممتاز، انحراف نادر جداً |
| Good (70–89) | أخطاء بسيطة من وقت لوقت |
| Average (50–69) | أخطاء ملحوظة، ممكن ينحرف فجأة |
| Fail (< 50) | مفيش تحديث — إعادة من الأول |

---

# الجزء الثاني — الفلو التقني (للمبرمجين)

## نظرة عامة على الـ Scripts

```text
IntroRobotController          ← يتحكم في الـ Intro ويمنع التحرك قبل Configure
StartMiniGame1OnRobotControl  ← يستمع على ControlManager ويبدأ الـ MiniGame
MiniGame1Manager              ← Coroutine رئيسية تدير كل الـ Phases
  ├── DriftChallenge          × 2 (يسار / يمين)
  ├── CameraAlignmentChallenge × 1
  └── SpeedConsistencyChallenge × 1
MiniGame1RawMetrics           ← تجمع البيانات الخام من كل التحديات
MiniGame1Evaluator            ← تحسب الـ Score النهائي من الـ Raw Metrics
MiniGame1RobotStatUpdater     ← تطبّق التحديث على RobotStatsSO
MiniGame1ResultScreenUI       ← تعرض النتيجة وتتحكم في الزرارين
MiniGame1RobotPovUI           ← تحدّث HUD الروبوت مع كل Phase
```

---

## Phase Sequence التفصيلي

```text
MiniGame1Manager.StartMiniGame()
        │
        ▼
  RunSequence() [Coroutine]
        │
        ├─► SetPhase(None) + raw.Reset()
        │
        ├─► [FreeMoveInitial]
        │     WaitForSeconds(initialFreeMoveSeconds)     ← default 10s
        │
        ├─► trackAccuracyTracker.ResetTracking()          ← يبدأ قياس المسار من هنا
        │
        ├─► [DriftLeft]
        │     DriftChallenge.BeginChallenge()
        │       ├── faultState.yawDriftDeg = +45°
        │       └── Update() لكل frame:
        │             • يحسب headingError = angle(robot.forward, driftedDir)
        │             • يسجل correctionStartTime لما stabilizedError < 20°
        │             • بعد challengeDurationSeconds: running = false
        │     WaitUntilComplete(driftLeft)
        │     driftLeft.ContributeToMetrics(ref raw)
        │       ├── raw.AddResponseTime(correctionStartTime - startTime)
        │       └── raw.AddCorrectionErrorDeg(integratedAbsError / samples)
        │
        ├─► [FreeMoveBetween_DriftLeft_DriftRight]
        │     WaitForSeconds(freeMoveBetweenChallengesSeconds)
        │
        ├─► [DriftRight]         ← نفس DriftLeft بـ driftAngleDeg = -45°
        │     ...
        │     challengeScores.driftScore = avg(leftScore, rightScore)
        │
        ├─► [FreeMoveBetween_DriftRight_Camera]
        │
        ├─► [CameraAlignment]
        │     CameraAlignmentChallenge.BeginChallenge()
        │       ├── orbitalFollow.VerticalAxis.Value += injectedPitchOffsetDeg (+25°)
        │       └── Update() لكل frame:
        │             • يحسب absErr = |camera.pitch - targetPitchDeg|
        │             • يسجل alignedStartTime لما absErr < 6°
        │             • بعد challengeDurationSeconds: reset pitch
        │     ContributeToMetrics:
        │       ├── raw.AddResponseTime(alignedStartTime - startTime)
        │       └── raw.AddCameraErrorDeg(sumAbsError / samples)
        │
        ├─► [FreeMoveBetween_Camera_Speed]
        │
        ├─► [SpeedConsistency]
        │     SpeedConsistencyChallenge.BeginChallenge()
        │       ├── faultState.speedWobbleAmplitude = 0.15
        │       └── Update() لكل frame:
        │             • speed = distance(pos, lastPos) / deltaTime
        │             • Welford online variance لحساب StdDev
        │             • بعد sampleWindowSeconds: running = false
        │     ContributeToMetrics:
        │       ├── raw.speedTarget = targetSpeed
        │       └── raw.speedStdDev = GetStdDev()
        │
        ├─► trackAccuracyTracker → raw.averageLateralDistanceMeters
        │
        ├─► MiniGame1Evaluator.Evaluate(profile, raw, challengeScores)
        │     ├── metricScores.pathAccuracy      = PathAccuracyScore(avgLateralDist)
        │     ├── metricScores.correctionAccuracy = 1 - (avgCorrErr / 45°) × 100
        │     ├── metricScores.responseTime       = ResponseTimeScore(avgRT)
        │     ├── metricScores.speedConsistency   = 1 - (stdDev / worstCase) × 100
        │     ├── metricScores.targetAlignment    = 1 - (avgCamErr / 30°) × 100
        │     └── finalScore = WeightedSum(profile.weightedMetrics)
        │           [PathAccuracy×0.40 + CorrectionAccuracy×0.35 + ResponseTime×0.25]
        │
        ├─► MiniGame1RobotStatUpdater.ApplyUpdateOnce(profile, robotStats, result)
        │     ├── if Fail → return (no update)
        │     ├── robotStats.ApplyDelta(stabilityΔ, pathAccΔ, responsivenessΔ)  [clamped 0..1]
        │     └── robotStats.SetErrorRates(driftRate, camRate, speedRate)
        │           └── from AnimationCurve: score 0→0.95 rate, score 100→0.05 rate
        │
        ├─► SetPhase(Completed)
        └─► MiniGameCompleted?.Invoke(result)
              └── MiniGame1ResultScreenUI.OnMiniGameCompleted(result)
                    ├── resultScreenRoot.SetActive(true)
                    ├── controlManager.SetInputLocked(true)
                    └── ApplyResult(r) → fills texts + scrollbars
```

---

## حساب الـ Score بالأرقام

### معادلة الـ Final Score

```text
Final Score = (PathAccuracy × 0.40) + (CorrectionAccuracy × 0.35) + (ResponseTime × 0.25)
```

### كيف تتحول كل Metric لدرجة

| Metric | المعادلة | مثال |
|--------|---------|------|
| **PathAccuracy** | `(1 - avgLateralDist / 1.0m) × 100` | 0.3m → 70 |
| **CorrectionAccuracy** | `(1 - avgCorrErr / 45°) × 100` | 15° → 67 |
| **ResponseTime** | Linear from `0.35s=100` to `3.0s=0` | 1.0s → 76 |
| **SpeedConsistency** | `(1 - stdDev / (targetSpeed × 0.5)) × 100` | stdDev=0.1, target=1 → 80 |
| **TargetAlignment** | `(1 - avgCamErr / 30°) × 100` | 8° → 73 |

### Tier Boundaries (في الكود الحالي)

| Tier | الحد الأدنى في الكود | الحد في الـ Design Doc |
|------|---------------------|----------------------|
| Excellent | 90 | 85 ⚠️ فرق بسيط |
| Good | 70 | 70 |
| Average | 50 | 50 |
| Fail | < 50 | < 50 |

> ⚠️ الـ threshold للـ Excellent في الكود `90` بدل `85` المذكور في الـ Design System — راجع الفريق وعدّله في `MiniGame1LearningProfileSO.excellentMinScore` لو اتفقتم على 85.

---

## تأثير النتيجة على الروبوت

### Robot Stats اللي بتتعدل

| Stat | Default | ما بيتعدل |
|------|---------|-----------|
| `stability` | 0.40 | يزيد حسب الـ Tier |
| `pathAccuracy` | 0.40 | يزيد حسب الـ Tier |
| `inputResponsiveness` | 0.40 | يزيد حسب الـ Tier |
| `driftErrorRate` | 0.50 | يُحدَّد من `driftScore` عبر AnimationCurve |
| `cameraErrorRate` | 0.50 | يُحدَّد من `cameraScore` عبر AnimationCurve |
| `speedErrorRate` | 0.50 | يُحدَّد من `speedScore` عبر AnimationCurve |

### جدول Tier Deltas (MiniGame1LearningProfileSO)

| Tier | `stability` | `pathAccuracy` | `inputResponsiveness` |
|------|-------------|----------------|----------------------|
| Excellent | +0.15 | +0.15 | +0.10 |
| Good | +0.10 | +0.10 | +0.07 |
| Average | +0.05 | +0.05 | +0.03 |
| Fail | 0 | 0 | 0 |

### ErrorRate Curve (AnimationCurve)

```text
Challenge Score  →  Error Rate بعد الـ Calibration
     0           →  0.95  (أداء سيء = أخطاء كتير بعد كده)
    50           →  0.50
   100           →  0.05  (أداء ممتاز = أخطاء نادرة)
```

### من يطبّق الـ Error Rates؟

`RobotStabilityApplier` (على الروبوت في الـ scene) — يقرأ الـ rates ويطبقها على الحركة الفعلية:
- `GetYawDriftDegrees()` ← يُضيف drift oscillating للحركة
- `GetSpeedMultiplier()` ← يُذبذب السرعة

يشتغل **بس** لو `robotStats.hasSavedCalibrationResult = true` (يعني بعد ما اللاعب يعدي الميني جيم بنجاح).

---

## الـ UI Connections

```text
Phase Change → MiniGame1RobotPovUI.OnPhaseChanged()
                  ├── challengeNameText ← "Drift (1)" / "Camera" / إلخ
                  ├── promptText        ← "Go Right / Go Left" / "Fix camera" / إلخ
                  └── logText           ← يتمسح في FreeMoveInitial، يتراكم بعدين

LogMessage event → MiniGame1RobotPovUI.AppendLog()
                  ├── يقطع السطر لو > 28 حرف
                  └── يحتفظ بآخر 6 سطور بس

MiniGameCompleted → MiniGame1ResultScreenUI.OnMiniGameCompleted()
                  ├── finalScoreText  ← "70%"
                  ├── tierText        ← "Good"
                  ├── driftScoreText  ← "83%"
                  ├── cameraBar.size  ← 0..1 normalized
                  ├── [Apply Improvements] → hide screen + unlock input
                  └── [Recalibrate]        → StartMiniGame() again
```

---

## ملفات الكود ذات الصلة

| الملف | الوظيفة |
|-------|---------|
| `IntroRobotController.cs` | Intro sequence + منع الحركة + Configure button |
| `StartMiniGame1OnRobotControl.cs` | يبدأ الـ MiniGame لما ControlManager يتبدل لـ Robot |
| `MiniGame1Manager.cs` | الـ Coroutine الرئيسية + PhaseChanged + LogMessage events |
| `DriftChallenge.cs` | Drift injection + per-frame heading error measurement |
| `CameraAlignmentChallenge.cs` | Camera pitch injection + alignment tracking |
| `SpeedConsistencyChallenge.cs` | Speed wobble injection + Welford online variance |
| `MiniGame1RawMetrics.cs` | Struct لتجميع البيانات الخام |
| `MiniGame1Evaluator.cs` | يحوّل Raw Metrics لـ Scores + Final Score |
| `MiniGame1Scoring.cs` | دوال الـ scoring (PathAccuracy, ResponseTime) |
| `MiniGame1LearningProfileSO.cs` | Weights + Tiers + Deltas + ScoreToErrorRate curves |
| `MiniGame1RobotStatUpdater.cs` | يطبّق التحديث على RobotStatsSO مرة واحدة |
| `RobotStatsSO.cs` | ScriptableObject فيه قيم الروبوت الحالية |
| `RobotStabilityApplier.cs` | يقرأ الـ error rates ويطبقها على الحركة الفعلية |
| `MiniGame1ResultScreenUI.cs` | يعرض النتيجة + يتحكم في الزرارين |
| `MiniGame1RobotPovUI.cs` | يحدّث الـ HUD مع Phase changes + Log scrolling |
| `MiniGame1FaultState.cs` | Fault injection مؤقت (yaw drift + speed wobble) أثناء التحديات |

---

*هذا الملف يُحدَّث مع أي تعديل في الـ flow أو الـ scoring.*
