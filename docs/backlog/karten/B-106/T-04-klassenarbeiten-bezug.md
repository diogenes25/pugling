# T-04 · Bekommt `Klassenarbeit` einen `SeriesUnit`-Bezug?

Status: entschieden     <!-- offen | beansprucht | entschieden -->
Typ: grilling           <!-- research | prototype | grilling | task -->
Blockiert durch: T-01

## Frage

`Klassenarbeit.SubjectId` ist heute optional (`KlassenarbeitEntities.cs:73`), ohne Chapter- oder
Lehrwerk-Bezug. Nach der Verschmelzung referenzieren die enthaltenen Übungen `SeriesUnit` statt
`Chapter` — soll `Klassenarbeit` diesen Bezug sichtbar/filterbar machen (z. B. „Klassenarbeit zu Unit 3"),
oder bleibt sie bewusst lehrwerk-agnostisch (mehrere Units/Reihen in einer Klausur zulässig, wie heute
mehrere Chapter)?

## Antwort

**Bewusst lehrwerk-agnostisch — kein neuer Bezug, kein Schema-Umbau.** Am Code nachgeprüft
(`KlassenarbeitEntities.cs:67-95`): `Klassenarbeit` ist heute schon ein reiner Container mit
**optionalem** `SubjectId` und einer `KlassenarbeitExercise`-Join-Liste ohne jede weitere Scope-Prüfung —
Übungen aus verschiedenen Kapiteln (künftig Units) und sogar verschiedenen Fächern durften schon immer
gemeinsam in einer Klausur stehen. Das ist keine Lücke, sondern spiegelt die Realität: eine echte
Klassenarbeit prüft oft Stoff aus mehreren Unterrichtseinheiten zugleich, nicht nur aus einer Unit.
Einen `SeriesUnitId`-Bezug einzuführen würde eine Einschränkung erfinden, die niemand verlangt hat —
weder der Live-Test noch eine der drei Rollen hat in dieser oder der vorigen Runde einen Bedarf an
„Klassenarbeit zu Unit 3" geäußert. Die Verschmelzung selbst ändert an dieser Entkopplung nichts:
`KlassenarbeitExercise` referenziert weiterhin nur `ExerciseId`, unabhängig davon, ob die Übung dahinter
an einem Chapter oder einer `SeriesUnit` hängt. **Kosten: keine** — kein Code, keine Migration.
