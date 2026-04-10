# مرجع تقدّم المشروع — How To Train Your AI

ملف مرجعي يُحدَّث يدويًا عند إضافة ميزات جديدة.

| | |
|---|---|
| **آخر تحديث للملف** | **10 أبريل 2026** |
| **آخر مسح أولي** | 23 مارس 2026 |

**GitHub:** [HowToTrainYourAI-Game](https://github.com/NourEldeenMahmoud/HowToTrainYourAI-Game) — دليل العمل الجماعي: **`TEAM_COLLABORATION_GUIDE.md`**

---

## 1. نظرة عامة

| البند | القيمة |
|--------|--------|
| اسم المنتج في Unity | How To Train Your AI |
| إصدار المحرر | Unity **6000.3.8f1** (Unity 6) |
| مسار المشروع | `How To Train Your AI` |
| إدارة نسخ | Git + Git LFS على GitHub (انظر `TEAM_COLLABORATION_GUIDE.md`) |

---

## 2. الحزم الرئيسية (`Packages/manifest.json`)

- **Universal Render Pipeline (URP)** — `com.unity.render-pipelines.universal` 17.3.0  
- **Input System** — `com.unity.inputsystem` 1.18.0  
- **AI Navigation** — `com.unity.ai.navigation` 2.0.10  
- **Cinemachine** — `com.unity.cinemachine` 3.1.6  
- **glTFast** — `com.unity.cloud.gltfast` 6.16.1 (استيراد نماذج glTF)  
- **UGUI، Timeline، Visual Scripting، Test Framework**  
- **IDE**: Rider و Visual Studio  

**ملاحظة:** لا يوجد في `manifest.json` حزمة **ML-Agents** حتى تاريخ التوثيق؛ اسم المشروع يشير لتدريب ذكاء اصطناعي، ويمكن إضافة ML-Agents لاحقًا عند البدء بالتدريب.

---

## 3. المشاهد (`Assets/Scenes`)

| المشهد | الغرض المفترض |
|--------|----------------|
| `How to train your ai.unity` | المشهد الرئيسي للمشروع |
| `PlayerMovement.unity` | اختبار/عرض حركة اللاعب |
| `Test.unity` | مشهد تجريبي |

**استرجاع:** توجد نسخ في `Assets/_Recovery/` (`0.unity`, `0 (1).unity`) — عادة من استرجاع Unity؛ راجعها قبل الاعتماد عليها كنسخة رسمية.

**Post Processing على الكاميرا:** في URP لازم الكاميرا الفعلية (التي ترى المشهد، غالبًا `Main Camera` مع `CinemachineBrain`) تكون مفعّل عليها **Render Post Processing** في `Universal Additional Camera Data`، وإلا الـ Volumes (Grain/Vignette إلخ) لن تظهر في الـ Game View.

---

## 4. الإدخال — ملفات Input Actions (`Assets/Inputs`)

| الملف | Map / إجراءات | ملاحظات |
|--------|----------------|---------|
| `Global.inputactions` | Map: **Switch** — Action: **Switch** | ربط افتراضي: **Tab**؛ يُستخدم للتبديل بين اللاعب والروبوت عبر `ControlManager` كـ `InputActionReference`. |
| `PlayerMovment.inputactions` | Map: **Player** — **Move** (Vector2)، **Sprint** (Button)، **Interact** (Button) | WASD للحركة، Shift للجري، **E** للتفاعل (Interact). |
| `RobotMovment.inputactions` | Map: **Robot** — **Move**، **Sprint** | نفس أسلوب اللاعب؛ ملف منفصل لـ `PlayerInput` الخاص بالروبوت. |

أسماء الملفات تستخدم إملاء **Movment** كما في المشروع.

---

## 5. السكربتات المخصصة (`Assets/Scripts`)

### 5.1 `Managers/ControlManager.cs`

**الدور:** إدارة التحويل بين تحكم اللاعب وتحكم الروبوت، الكاميرات، تأثيرات الـ Pod، وواجهة الـ Pod، والوقت على الـ UI.

**مراجع الـ Inspector (مختصرة):**

| حقل | الوظيفة |
|-----|---------|
| `playerInput` / `robotInput` | مكوّن `PlayerInput` على اللاعب والروبوت |
| `switchAction` | مرجع لـ `Global` → `Switch/Switch` (Tab) |
| `playerCamera` / `robotCamera` | `CinemachineCamera`؛ التبديل بـ **Priority** (نشط = `activeCameraPriority`، غير نشط = `inactiveCameraPriority`) |
| `podFxVolume` | `Volume` (مثل Pod Volume) لـ post-processing |
| `fxStartDelay` | تأخير (ثوانٍ) قبل بدء blend الـ FX وظهور `podUiRoot` بعد انتهاء الترانزشن |
| `fxBlendSpeed` | سرعة انتقال `podFxVolume.weight` نحو 0 أو 1 |
| `podUiRoot` | جذر واجهة الـ Pod (Canvas/Panel) |
| `timeText` | `TMP_Text` لعرض الوقت الحقيقي بصيغة `HH:mm:ss` |

**تسلسل منطقي مهم:**

1. عند التحويل: تعطيل/تفعيل `PlayerInput` المناسب، تعديل أولويات الكاميرات، تصفير وزن الـ Volume فورًا، إخفاء `podUiRoot`.  
2. بعد `fxStartDelay`: تعيين `targetFxWeight` (0 للاعب، 1 للروبوت) وإظهار `podUiRoot` فقط عند التحكم بالروبوت.  
3. في `Update`: تنعيم `podFxVolume.weight` نحو `targetFxWeight`.  
4. في `Awake`: بعد `SetControlState(true)` يُجبر `podFxVolume.weight = 0` حتى لا يظهر الإيفكت قبل أول تحكم بالروبوت.

**ملاحظة:** تمت إزالة عرض نص «كنترولز الروبوت» من هذا السكربت؛ الـ HUD الحالي للـ Pod بدون ذلك القسم.

---

### 5.2 `PlayerMovment/PlayerMovement.cs`

- **الكلاس:** `PlayerMovementAdvanced`  
- **المتطلبات:** `CharacterController`  
- **الإدخال:** `OnMove`, `OnSprint` (Input System — Send Messages على `PlayerInput`)  
- **السلوك:** محاذاة `orientation` مع اتجاه الكاميرا على المستوى الأفقي، حركة نسبية، مشي/جري، جاذبية، تنعيم سرعة، Animator (`IsWalking`, `IsSprinting`)، كاميرا اختيارية `basicCam`، قفل المؤشر.

---

### 5.3 `Robot/RobotMovment.cs`

- **الكلاس:** `RobotMovement`  
- **المتطلبات:** `CharacterController`  
- **الإدخال:** `OnMove`, `OnSprint`  
- **السلوك:** حركة نسبية للكاميرا، تسطيح/حركة أفقية، مشي/جري، `applyRootMotion = false` على الـ Animator، دوران بصري للروبوت.

---

### 5.4 `PlayerMovment/PlayerInteractor.cs`

- **الدور:** كشف أشياء قابلة للتفاعل أمام كاميرا اللاعب، عرض نص التلميح، وتنفيذ التفاعل عند الضغط على **Interact**.  
- **المراجع:** `playerCamera`, `interactPromptText` (TMP)، `interactDistance`, `interactLayers`.  
- **التنفيذ:** `Physics.RaycastAll` مرتبة بالمسافة؛ تجاهل الاصطدام بجذر اللاعب (`transform.root`) حتى لا يختار كوليدر اللاعب.  
- **العرض:** تحويل موضع العالمي من `SimpleInteractable.PromptWorldPosition` إلى شاشة (`WorldToScreenPoint`) ووضع `interactPromptText.transform.position` — يظهر النص فوق الأوبجكت تقريبًا.  
- **الإدخال:** `OnInteract(InputValue)` يجب أن يُستدعى من `PlayerInput` عند Action اسمها **Interact** (زر E).

---

### 5.5 `PlayerMovment/SimpleInteractable.cs`

- **الدور:** يُضاف على أي GameObject فيه Collider (باب، مفتاح، إلخ).  
- **الحقول:** `interactPrompt` (نص التلميح)، `promptAnchor` (اختياري)، `promptHeightOffset` (إذا لم يوجد anchor).  
- **`Interact()`:** حاليًا `Debug.Log` باسم الـ GameObject — نقطة بداية لاحقًا لفتح باب أو أنيميشن.

---

## 6. الأنيميشن والتحكم

- **Animator للاعب:** `AnimationControllers/Player.controller`  
- **Animator للروبوت:** `AnimationControllers/MovmentController.controller`  
- **أنيميشن لاعب:** `Animations/Player/` — `PlayerWalk.anim`, `PlayerRun.anim`, FBX مرتبطة.  
- **أنيميشن روبوت:** `Animations/Robot/` — FBX (مشي، Idle، إلخ).

---

## 7. الـ Prefabs (`Assets/prefabs`)

- `Player.prefab`, `Robot.prefab`, `front Garden.prefab`, `road wall.prefab`

---

## 8. التضاريس (`Assets/Terrains`)

- `New Terrain.asset`  
- `TerrainData_268bd3ce-0e08-40ac-bb9f-9c5425624f67.asset`

---

## 9. الأصول والإعدادات

- **`Imported Assets/`:** مواد، نسيج، نماذج (منزل، أبواب، بيئة، إلخ).  
- **`Imported Assets/Materials/Robot Materials/`:** مواد الروبوت.  
- **`Assets/Settings/`:** URP (`PC_RPAsset`, `Mobile_RPAsset`, `DefaultVolumeProfile`, إلخ).

---

## 10. إعداد سريع في المشهد (Checklist)

**تبديل اللاعب ↔ الروبوت**

- GameObject فيه `ControlManager` مع ربط `playerInput`, `robotInput`, `switchAction` (Global/Switch), كاميرات Cinemachine، `podFxVolume`, `podUiRoot`, `timeText` حسب الحاجة.  
- `PlayerInput` اللاعب: Actions = `PlayerMovment`، Default Map = Player.  
- `PlayerInput` الروبوت: Actions = `RobotMovment`، Default Map = Robot؛ يُفضّل أن يبدأ معطّلًا إن لم يضبط `ControlManager` من أول إطار.

**التفاعل (Interact)**

- على اللاعب: `PlayerInteractor` + ربط الكاميرا و`Interaction Text`.  
- على الهدف: `SimpleInteractable` + Collider.  
- التأكد من وجود Action **Interact** في `PlayerMovment.inputactions` ومربوطة بـ E، ومطابقة الاسم في `PlayerInput` (Send Messages).

**Post Processing**

- تفعيل **Post Processing** على كاميرا اللعبة + Volume Profile مناسب على `Global Volume` أو `Pod Volume`.

---

## 11. ما تم إنجازه (ملخص تنفيذي موسّع)

1. مشروع Unity 6 مع URP و Input System و Cinemachine 3.  
2. حركة لاعب وروبوت بـ CharacterController وربط أنيميشن.  
3. نظام تبديل تحكم عالمي بـ **Tab** (`Global.inputactions`) عبر `ControlManager` مع أولويات كاميرا Cinemachine.  
4. تأثيرات Pod عبر **Volume** (blend + تأخير بعد الترانزشن) وواجهة Pod تظهر بعد التأخير وليس أثناء النقل المباشر.  
5. عرض **الوقت الحقيقي** على UI من `ControlManager`.  
6. نظام **Interact**: Raycast من كاميرا اللاعب، تلميح نصي فوق الأوبجكت، ضغط **E** و`Debug.Log`.  
7. بيئة ومشاهد وPrefabs وتضاريس وأصول مستوردة كما في الأقسام أعلاه.

---

## 12. اقتراحات لمتابعة التوثيق والتطوير

- عند إضافة **ML-Agents**: سجّل إصدار الحزمة، مسارات YAML، وأسماء الـ Behaviors.  
- عند كل ميزة كبيرة: حدّث **آخر تحديث للملف** في أعلى الجدول.  
- عند استخدام Git: أضف رابط المستودع والفرع هنا.  
- استبدال `Debug.Log` في `SimpleInteractable.Interact()` بمنطق فعلي (Animator، فتح باب، صوت، إلخ).

---

*هذا الملف لا يستبدل التعليقات داخل الكود؛ يهدف ليكون خريطة مرجعية للمشروع.*
