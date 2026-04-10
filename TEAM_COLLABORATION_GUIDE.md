# دليل العمل الجماعي — How To Train Your AI

مشروع Unity 6 على GitHub مع **Git LFS** للأصول الكبيرة. اتبعوا الدليل عشان الشغل يفضل منظم ومحدش يبوّظ شغل التاني.

---

## 1. روابط سريعة

| | |
|---|---|
| **الريبو** | [https://github.com/NourEldeenMahmoud/HowToTrainYourAI-Game](https://github.com/NourEldeenMahmoud/HowToTrainYourAI-Game) |
| **Clone (HTTPS)** | `https://github.com/NourEldeenMahmoud/HowToTrainYourAI-Game.git` |
| **إصدار Unity** | **6000.3.8f1** (أو نفس الـ patch اللي الفريق يتفق عليه ويُدوَّن هنا) |

---

## 2. إعداد كل عضو (مرة واحدة على الجهاز)

### 2.1 Git

- ثبّت [Git for Windows](https://git-scm.com/download/win) (أو Git على macOS/Linux).
- افتح Terminal/PowerShell وتحقق:
  ```bash
  git --version
  ```

### 2.2 Git LFS

- ثبّت من [git-lfs.com](https://git-lfs.com) ثم:
  ```bash
  git lfs install
  ```
- **مهم:** بدون `git lfs install` على الجهاز، الـ `clone` قد يجلب **مؤشرات** فقط للملفات الكبيرة بدل الملف الحقيقي.

### 2.3 تسجيل الدخول إلى GitHub

- **HTTPS:** GitHub لم يعد يقبل كلمة مرور الريبو؛ استخدم **Personal Access Token (Classic)** ككلمة مرور عند أول `git push`، أو فعّل **Git Credential Manager**.
- **SSH:** بديل أقوى للمستقبل: أضف مفتاح SSH في GitHub واستخدم رابط `git@github.com:...`.

### 2.4 استنساخ المشروع

```bash
cd المسار_اللي_تحب_تشتغل_فيه
git clone https://github.com/NourEldeenMahmoud/HowToTrainYourAI-Game.git
cd HowToTrainYourAI-Game
git lfs pull
```

- افتح المجلد في **Unity Hub** → **Add project from disk** → اختر المجلد.
- أول فتح: Unity يبني مجلد **`Library/`** محليًا — **لا ترفعه** للـ Git (موجود في `.gitignore`).

---

## 3. ما الذي يُرفع وما الذي لا يُرفع؟

| يُرفع | لا يُرفع |
|--------|-----------|
| `Assets/` (مع كل `.meta`) | `Library/` |
| `Packages/` (`manifest.json`, `packages-lock.json`) | `Temp/`, `Logs/` |
| `ProjectSettings/` | `UserSettings/` |
| `.gitignore`, `.gitattributes` | `Build/`, `Builds/` |

- **ملفات `.meta`:** لازم تترفع مع كل أصل؛ حذفها أو نسيانها يكسر المراجع داخل Unity.

### Git LFS (ما تم تفعيله في المشروع)

الامتدادات التالية تُدار عبر LFS (انظر `.gitattributes`):

`*.fbx`, `*.blend`, `*.psd`, `*.exr`, `*.tif`, `*.tiff`, `*.wav`, `*.mp4`, `*.aiff`, `*.tga`

- لو احتجتم نوع ملف جديد كبير:  
  `git lfs track "*.ext"` ثم أضيفوا `.gitattributes` في commit منفصل.

---

## 4. سير عمل Git يومي (للجميع)

### قبل ما تبدأ تشتغل

```bash
git checkout main
git pull origin main
```

### لكل ميزة أو تذكرة

```bash
git checkout -b feature/وصف-قصير-بالانجليزي
# مثال: feature/door-interaction
```

- اعمل التعديلات في Unity / الكود.
- راجع ما ستُرفع:

```bash
git status
git diff
```

- ارفع التغييرات:

```bash
git add .
git commit -m "وصف واضح: ماذا تغير ولماذا"
git push -u origin feature/وصف-قصير-بالانجليزي
```

- على GitHub: افتح **Pull Request** من فرعك إلى **`main`**.
- بعد الموافقة (Review): **Merge** (يفضّل **Squash merge** لو عايزين تاريخ أنظف).

### بعد الـ Merge

على جهازك:

```bash
git checkout main
git pull origin main
```

---

## 5. إدارة الفريق (منظم / Lead)

### 5.1 توزيع المهام

- قسّموا العمل بحيث **ما يشتغلش اثنين على نفس المشهد الكبير** في نفس الوقت إن أمكن.
- استخدموا **Issues** أو **Projects** على GitHub: كل Issue = فرع + PR واحد.

### 5.2 حماية الفرع `main` (مستحسن)

في GitHub: **Settings → Branches → Branch protection rules** للفرع `main`:

- Require pull request قبل الدمج.
- (اختياري) Require 1 approval.
- منع push المباشر لـ `main`.

### 5.3 من يعمل Merge؟

- إما **شخص واحد** (Maintainer) أو **أي عضو** بعد Review من زميل — واتفقوا كتابة في القناة.

### 5.4 الاجتماعات القصيرة

- مرة بالأسبوع: ماذا دخل `main`، ماذا التالي، من يمسك أي مشهد.

---

## 6. أفضل ممارسات Unity (تقليل كسر شغل بعض)

### 6.1 إصدار Unity

- **نفس الإصدار** لكل الفريق. اختلاف الـ minor قد يغيّر `ProjectSettings` بدون قصد.

### 6.2 المشاهد (`.unity`)

- تعارض الدمج في الملفات النصية للمشاهد **صعب القراءة**.
- **قللوا التعديل المتزامن** على نفس المشهد.
- فضّلوا: **Prefab** للعناصر + تعديل الـ Prefab في فرع، والمشهد يبقى “تجميع” بسيط.

### 6.3 Prefabs

- أي شيء يتكرر (لاعب، باب، UI panel): اجعلوه **Prefab** وعدّلوا الـ Prefab بدل نسخ يدوي في المشهد.

### 6.4 إعدادات المشروع (موجودة عندكم)

- **Visible Meta Files** + **Force Text** للأصول — مناسبة للـ Git (لا تغيّروها لـ Binary بدون سبب قوي).

### 6.5 قبل الـ Commit

- أغلقوا المشهد إن أمكن، احفظوا (**Ctrl+S**).
- شغّلوا المشهد مرة سريعة للتأكد أنه لا يوجد أخطاء واضحة في الـ Console.

### 6.6 أصول ضخمة جديدة

- لو الملف أكبر من حد معقول: تأكد أن امتداده **مغطى بـ LFS** في `.gitattributes` قبل إضافته.

---

## 7. أوامر مفيدة

```bash
# من رفع على LFS؟
git lfs ls-files

# تحديث من main بدون فقدان فرعك (بعد commit أو stash)
git fetch origin
git merge origin/main
# أو: git rebase origin/main (اتفقوا على السياسة)
```

---

## 8. أول رفع من الجهاز المحلي (لو لم يكتمل من Cursor)

إذا كان الريبو ما زال فارغًا على GitHub وعندك بالفعل `commit` محلي:

```bash
cd "مسار/How To Train Your AI"
git branch -M main
git remote add origin https://github.com/NourEldeenMahmoud/HowToTrainYourAI-Game.git
# إذا remote موجود مسبقًا: git remote set-url origin https://github.com/NourEldeenMahmoud/HowToTrainYourAI-Game.git
git push -u origin main
```

سيطلب GitHub المصادقة — استخدم **Token** أو **GitHub CLI** (`gh auth login`).

بعد رفع `main`، أضيفوا الدليل والتحديثات الجديدة بنفس سير الـ PR إن فعّلتم الحماية على `main`.

---

## 9. مرجع المشروع التقني

- تفاصيل الأنظمة (ControlManager، Input، Interact، إلخ): **`PROJECT_PROGRESS_REFERENCE.md`**

---

## 10. ملخص قواعد ذهبية

1. **Pull من `main` قبل ما تبدأ.**  
2. **فرع لكل مهمة** — لا تعمل كل شيء على `main`.  
3. **لا تنسَ ملفات `.meta`.**  
4. **لا ترفع `Library`.**  
5. **`git lfs install` على كل جهاز.**  
6. **نفس إصدار Unity.**  
7. **قلل التعارض: Prefabs + تقسيم المشاهد + تواصل من يعدل أي مشهد.**

---

*حدّثوا تاريخ الإصدار Unity في القسم 1 عند أي ترقية للمشروع.*
