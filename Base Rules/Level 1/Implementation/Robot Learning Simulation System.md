---
share_link: https://share.note.sx/f6c9fncp#gV0/gmFsqbySHf14js+7lzW3T5DotPcctVe/4U4b5Vg
share_updated: 2026-04-12T00:38:00+02:00
dg-publish: true
---



> [!Caution] المقاييس والصفات الي هتتغير في الروبوت والحاجات دي مجرد امثله لسه كل واحد لما يعمل لعبته هيحددها خصوصا اول لعبه 

## الفكرة العامة

كل ميني جيم في المشروع هتمشي على نفس 3 مراحل:

### 1) Data Collection

أثناء اللعب، السيستم بيسجل أفعال اللاعب والمؤشرات المهمة الخاصة بالميني جيم.

### 2) Evaluation

في نهاية الميني جيم، السيستم يحسب Score لكل مقياس من المقاييس المطلوبة.

### 3) Robot Update


بعد ظهور النتيجة، السيستم يعدل قيم الروبوت مرة واحدة فقط بناءً على الأداء.

مهم:

- مفيش أي تحديث لقيم الروبوت أثناء الميني جيم
- التحديث بيحصل بعد النهاية فقط

---

## الشكل الموحد لأي Mini-Game

كل ميني جيم لازم يتكتب بنفس الفورمات ده:

- **Tracked Metrics**  
    إيه الحاجات اللي هتتقاس
    
- **Score Calculation**  
    إزاي هنحول القياس لدرجات
    
- **Affected Robot Stats**  
    إيه القيم اللي هتتعدل في الروبوت
    
- **Update Rules**  
    إزاي التعديل هيحصل
    

ده يضمن إن أي حد في التيم يقدر يشتغل بنفس النظام.

---

# 1) Robot Stats System

## الروبوت هيكون له ملف واحد ثابت

نعمل `RobotStats` كـ ScriptableObject أو Data Asset فيه كل القيم الأساسية اللي بتتعدل طول المشروع.


> [!danger] بص هنا 
> - كل واحد في اللعبه بتاعته هيضيف القيم الي محتاجها يعني في اللعبه الاولي هنضيف مثلا قيمة لاحتمالية ان الروبوت ينحرف فجاة 
> - المفروض يبقي فيه نسبة سماحية دايما


## القيم المقترحة

### Movement

- `MoveSpeed`
    
- `TurnSpeed`
    
- `PathAccuracy`
    
- `Stability`
    
- `InputResponsiveness`
    

### Systems

- `EnergyConsumptionRate`
    
- `BatteryCapacity`
    
- `AudioClarity`
    

### Cognition

- `CommandUnderstanding`
    
- `DecisionConfidence`
    

دول كفاية جدًا كبداية، وأي حاجة زيادة تتضاف بعدين لو فعلًا احتجناها.

> [!Caution] الي هينفذ اللعبه دي هو الي هيحدد ايه القيم الي هتتقاس , ايه القيم الي تتسجل ,  ايه الصفات الي تتحسن


---

# 2) Metric System

## كل ميني جيم يختار من 3 إلى 4 Metrics فقط

ماينفعش كل ميني جيم يقيس 8 حاجات، لأن ده هيعقد الحسابات ويشتت الفريق.

### Metrics Library مقترحه موحدة للمشروع

دي قائمة المقاييس المقترحه اللي أي ميني جيم يختار منها:

- `PathAccuracy`
    
- `CorrectionAccuracy`
    
- `ResponseTime`
    
- `EnergyEfficiency`
    
- `CollisionSafety`
    
- `CommandExecution`
    
- `TargetAlignment`
    
- `SpeedConsistency`
    

كل ميني جيم يختار المقاييس المناسبة له فقط.

---

# 3) طريقة الحساب الموحدة

## كل Metric بيتحول لنسبة من 0 إلى 100

يعني في الآخر كل Metric لازم يطلع Score واضح:

- 0 = أداء سيئ جدًا
    
- 50 = أداء مقبول
    
- 100 = أداء ممتاز
    

بعد كده نستخدم الأوزان.

## Final Score Formula

```text
Final Score = Sum(Metric Score × Metric Weight)
```

لازم مجموع الأوزان = 100%

---

# 4) مثال ثابت على الحساب

## Mini-Game 1: Control Calibration

### Tracked Metrics

- PathAccuracy = 40%
    
- CorrectionAccuracy = 35%
    
- ResponseTime = 25%
    

### Example

- PathAccuracy = 80
    
- CorrectionAccuracy = 60
    
- ResponseTime = 70
    

### Final Score

```text
Final Score = (80 × 0.40) + (60 × 0.35) + (70 × 0.25)
Final Score = 32 + 21 + 17.5 = 70.5
```

النتيجة النهائية = 70.5 / 100

---

# 5) Performance Tier System

بدل ما كل ميني جيم يخترع Rules جديدة، نستخدم Tiers ثابتة في المشروع كله:

- **Excellent**: 85 – 100
    
- **Good**: 70 – 84
    
- **Average**: 50 – 69
    
- **Fail**: أقل من 50
    

وده يخلي الفريق كله شغال بنفس المنطق.

---

# 6) Robot Update Rules

## التعديل على الروبوت ما يكونش مباشر بنسبة الدرجة

يعني غلط جدًا نقول:

- اللاعب جاب 90 → نزود Stability بـ 90
    

الصح:

- كل Tier له تعديل ثابت وصغير
    

## Update Values المقترحة

### Excellent

- تحسين كبير
    

### Good

- تحسين متوسط
    

### Average

- تحسين بسيط
    

### Fail

- بدون تحديث أو إعادة المحاولة
    

---

## Example Update Table

### لو الميني جيم بتأثر على:

- `Stability`
    
- `PathAccuracy`
    
- `InputResponsiveness`
    

يبقى التحديث يبقى كده:

|Tier|Stability|PathAccuracy|InputResponsiveness|
|---|---|---|---|
|Excellent|+0.15|+0.15|+0.10|
|Good|+0.10|+0.10|+0.07|
|Average|+0.05|+0.05|+0.03|
|Fail|0|0|0|

كل القيم لازم تتعمل لها `Clamp` بين 0 و 1

---

# 7) قاعدة التوازن

لازم كل Robot Stat يبقى له:

- Minimum Value
    
- Maximum Value
    
- Default Value
    

### مثال

- `Stability = 0.40` في البداية
    
- أقل قيمة = 0
    
- أعلى قيمة = 1
    

بعد أي تحديث:

```text
New Value = Clamp(Current Value + Delta, Min, Max)
```

ده يمنع أي Stat يبوظ أو يكسر اللعبة.

---

# 8) إزاي كل Mini-Game تعرف تعدل إيه

كل ميني جيم لازم يبقى فيها Data واضح بالشكل ده:

## Mini-Game Learning Profile

- **Metrics Used**
    
- **Weights**
    
- **Required Pass Score**
    
- **Affected Robot Stats**
    
- **Update Amounts per Tier**
    

---

## Example: Control Calibration Profile

### Metrics Used

- PathAccuracy
    
- CorrectionAccuracy
    
- ResponseTime
    

### Weights

- PathAccuracy = 40%
    
- CorrectionAccuracy = 35%
    
- ResponseTime = 25%
    

### Pass Score

- 50
    

### Affected Robot Stats

- Stability
    
- PathAccuracy
    
- InputResponsiveness
    

### Update Amounts

- Excellent → +0.15 / +0.15 / +0.10
    
- Good → +0.10 / +0.10 / +0.07
    
- Average → +0.05 / +0.05 / +0.03
    
- Fail → No Update
    

---

## Example: Sound Card Efficiency Trial Profile

### Metrics Used

- EnergyEfficiency
    
- CollisionSafety
    
- PathAccuracy
    

### Weights

- EnergyEfficiency = 40%
    
- CollisionSafety = 35%
    
- PathAccuracy = 25%
    

### Pass Score

- 50
    

### Affected Robot Stats

- EnergyConsumptionRate
    
- DecisionConfidence
    
- PathAccuracy
    

### Update Amounts

- Excellent → -0.10 / +0.10 / +0.08
    
- Good → -0.07 / +0.07 / +0.05
    
- Average → -0.03 / +0.03 / +0.02
    
- Fail → No Update
    

مهم:

- القيم اللي زي `EnergyConsumptionRate` تتحسن بالنقصان، لأن الأقل أفضل
    

---

# 9) Standard Flow for Every Mini-Game

كل ميني جيم لازم تمشي بالترتيب ده:

### Step 1: Start

- الميني جيم تبدأ
    
- السيستم يقرأ الـ Learning Profile الخاص بيها
    

### Step 2: Track

- أثناء اللعب، السيستم يجمع الداتا المطلوبة فقط
    

### Step 3: Evaluate

- عند النهاية، يتحسب Score لكل Metric
    
- يتحسب Final Score
    
- يتحدد الـ Tier
    

### Step 4: Apply Update

- لو اللاعب نجح:
    
    - نعدل Robot Stats حسب الـ Tier
        
- لو فشل:
    
    - مفيش تعديل
        
    - يعيد الميني جيم
        

### Step 5: Save Result

- نحفظ النتيجة
    
- نحدث الـ RobotStats
    
- نعرض التقرير
    

---

# 10) Tech Structure المقترح

## ملفات أساسية

### 1. `RobotStatsSO`

فيه القيم الحالية للروبوت

### 2. `MiniGameLearningProfile`

فيه إعدادات كل ميني جيم:

- metrics
    
- weights
    
- affected stats
    
- deltas
    
- pass score
    

### 3. `MiniGameTracker`

يجمع الداتا أثناء اللعب

### 4. `MiniGameEvaluator`

يحسب النتيجة النهائية

### 5. `RobotStatUpdater`

يطبق التحديث على الروبوت

---

# 11) النظام البسيط اللي الفريق كله يمشي عليه

## Rule 1

كل ميني جيم تستخدم من 3 إلى 4 metrics فقط

## Rule 2

كل Metric لازم تتحول لدرجة من 100

## Rule 3

كل ميني جيم لازم يكون لها Learning Profile واضح

## Rule 4

كل Robot Stat تتحدث في النهاية فقط

## Rule 5

كل التحديثات تكون قيم صغيرة ومقفولة بـ Clamp

## Rule 6

كل ميني جيم تعدل 2 أو 3 stats فقط

---

# 12) Template جاهز لأي Mini-Game جديدة

تقدر تخلي الفريق يملأ التمبلت ده في أي ميني جيم:

## Mini-Game Learning Template

- **Mini-Game Name:**
    
- **Pass Score:**
    
- **Metrics Used:**
    
- **Metric Weights:**
    
- **Tracked Events:**
    
- **Final Score Formula:**
    
- **Affected Robot Stats:**
    
- **Excellent Updates:**
    
- **Good Updates:**
    
- **Average Updates:**
    
- **Fail Behavior:**
    

---

# 13) هل فيه حاجات زيادة عندك لازم تتشال؟

أيوه، علشان النظام يفضل سهل:

## أجّل دلوقتي:

- HallucinationChance
    
- MemoryRetention
    
- SensorRange
    
- ObstacleAvoidance
    

مش عشان وحشين، لكن لأنهم:

- مش ضروريين لأول vertical slice
    
- وهيعقدوا الـ balancing
    
- وبعضهم محتاج systems أكبر من اللي عندك حاليًا
    

---

# 14) البداية العملية الصح

ابدأوا كفريق بالترتيب ده:

### أولًا

اعتمدوا قائمة Robot Stats الأساسية فقط

### ثانيًا

اعملوا Learning Profile للميني جيم الأولى

### ثالثًا

اعملوا Tracker بسيط لثلاث metrics فقط

### رابعًا

اعملوا Evaluator يطلع:

- metric scores
    
- final score
    
- tier
    

### خامسًا

اعملوا Updater يعدل الـ ScriptableObject

---

# الخلاصة

السيستم اللي أنصحك تعتمدوه للمشروع كله هو:

- RobotStats ثابتة في ScriptableObject واحد
    
- كل ميني جيم لها Learning Profile خاص
    
- كل ميني جيم تقيس 3–4 metrics فقط
    
- كل metric تتحول لدرجة من 100
    
- النتيجة النهائية تطلع Tier موحد
    
- الـ Tier هو اللي يحدد التعديل
    
- التحديث يحصل مرة واحدة في آخر الميني جيم
    
- كل التعديلات صغيرة ومقفولة
    

ده System:

- سهل على الفريق
    
- سهل يتبرمج
    
- سهل يتظبط
    
- وسهل يتكرر على كل المشروع
    

لو عايز، أقدر أكتب لك بعده مباشرة:  
**نسخة technical table جاهزة للميني جيم الأولى والثانية** بحيث تبقوا أول حاجتين شغالين على نفس السيستم.

<!-- opencode:related-start -->
## Related
- [[01 Projects/VrProject/VR Project.md|VrProject Hub]]
- [[01 Projects/01 Projects Index|Projects Index]]
- [[02 Areas/02 Personal/Main/01 Current Areas|Current Areas]]
<!-- opencode:related-end -->
