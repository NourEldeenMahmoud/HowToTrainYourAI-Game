كل حاجة شغالة صح. خلاصة اللي اتعمل:

---

## المشكلة كانت إيه؟

ا Unity بتحفظ الـ scenes والـ prefabs كـ YAML. لما اتنين يعدّلوا نفس الملف، Git بيعمل merge نصي عادي بيبوظ الـ structure وبيكسر الـ references لأن الـ fileIDs والـ GUIDs حساسة جداً للترتيب.

---

## الحل اللي اتطبّق

**`.gitattributes`** — إخبار Git يستخدم `UnityYAMLMerge` (مدمج مع Unity) لكل ملفات Unity:
```
*.unity   merge=unityyamlmerge eol=lf
*.prefab  merge=unityyamlmerge eol=lf
*.asset   merge=unityyamlmerge eol=lf
...
```

**`setup-git.ps1`** — سكريبت لازم كل واحد في الفريق يشغله **مرة واحدة بس** على الجهاز بتاعه.

---

## اللي المفروض تعمله دلوقتي

1. **Commit** التغييرات (`.gitattributes` + `setup-git.ps1`) وادفعهم
2. **قول لكل زميل** يعمل `git pull` وبعدين يشغل:
```powershell
.\setup-git.ps1
```

كل واحد في الفريق شغّله مرة واحدة على جهازه — وبعدها لما يحصل merge في الـ scenes والـ prefabs، Unity SmartMerge هيتعامل معها بذكاء بدل الـ merge العادي.