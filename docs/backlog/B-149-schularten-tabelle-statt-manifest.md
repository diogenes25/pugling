---
tags: [typ/story, status/ausformuliert, bereich/frontend, bereich/katalog, rolle/creator]
aliases: [SCHOOL_TYPES handgepflegt, Schularten ohne Manifest, Enum-Kopie im Frontend]
status: ausformuliert
prio: P3
art: Aufräumen
groesse: ""
wo: ""
migration: ""
vertragsbruch: ""
quelle: frontend-reviewer im Nachtlauf Sprint 3 (2026-08-10)
unverifiziert: false
grund: ""
ersetzt_durch: []
entgangen_bei: []
nachgeschaut: ""
wartet_auf: ""
---

# B-149 · Die Schularten-Liste ist eine handgepflegte Kopie eines Server-Enums

## User Story

Als **Entwickler** möchte ich, dass eine neue Schulart im Server sie auch im Frontend erscheinen lässt —
ohne dass jemand daran denken muss.

## Ist-Stand am Code

Stand `2dd5a78`, alles nachgemessen.

**Die Liste.** `frontend/src/lib/labels.ts:31` hält `SCHOOL_TYPES` als Literal mit sechs Werten
(`Grundschule, Hauptschule, Realschule, Gymnasium, Gesamtschule, Berufsschule`); das Server-Enum
`SchoolTypes` führt dieselben sechs plus `None`. Der Typ hilft nicht: `SchoolTypes` kommt im generierten
Schema als nackter `string` an, `SCHOOL_TYPES.includes(...)` ist also eine reine **Laufzeit**-Prüfung
ohne Compiler-Netz.

**Das Dokument trägt die Werte NICHT maschinenlesbar — und das ist Absicht.**
`backend/Pugling.Api/Program.cs:313-314` lässt `schema.Enum` für `[Flags]`-Typen bewusst weg, mit
Begründung aus [B-60](B-60-flags-enum-im-dokument.md): Eine Enum-Liste der Einzelnamen wiese genau die
Kombinationen zurück, die der Server täglich sendet. Übrig bleiben die Werte nur in der **Prosa** der
Beschreibung („Comma-separated combination of: None, Grundschule, …", `Program.cs:316`). Damit ist der
billigste der drei Wege aus dem ersten offenen Punkt gestorben: aus dem Dokument ist die Liste nur durch
Parsen eines englischen Satzes zu gewinnen.

**Dreizehn Fundstellen in sieben Dateien, davon vier entscheidungstragend — in drei verschiedenen
Verhaltensweisen.** Das ist
der eigentliche Fund dieser Recherche: Die Liste ist nicht einmal, sondern viermal Schiedsrichter, und
die vier Stellen sind sich uneinig, was mit einem unbekannten Wert geschieht.

| Stelle | Was sie tut | Verhalten bei einer neuen Server-Schulart |
| --- | --- | --- |
| `VaterLehrwerke.tsx:319` (B-143) | gesperrte Option, wenn nicht in der Liste | zeigt den Einzelwert als „Kombination", **unwählbar** |
| `VaterFachlehrer.tsx:327` (B-148) | dieselbe Zeile, seit heute | dasselbe |
| `ExerciseEditModal.tsx:143` | `.filter(s => SCHOOL_TYPES.includes(s))` | **verwirft den Wert still** beim Laden ins Formular |
| `VaterKind.tsx:150,186` | normalisiert auf `"None"`, vergleicht gegen den Anzeigewert | Wert überlebt, aber `None` ist **nicht mehr herstellbar** |

Die übrigen **neun** sind reine `map`-Aufrufe fürs Pulldown (`ExerciseEditModal.tsx:211`,
`ExerciseFilterBar.tsx:91`, `VaterExerciseCreate.tsx:237`, `VaterFachlehrer.tsx:330`, `VaterKind.tsx:229`,
`VaterLehrwerke.tsx:77,322,658`, `VaterWizard.tsx:255`) — dort fehlt der neue Wert nur zur Auswahl, was
unschön, aber harmlos ist.

**Ein Guard-Test hätte kein Vorbild in C#.** Kein Test in `Pugling.Api.Tests` liest heute eine
Frontend-Datei (`ContractDocumentTests.cs` erwähnt das Frontend nur in Kommentaren). Das etablierte
Muster „Server-Liste gegen Oberfläche" liegt auf der **E2E**-Seite:
`frontend/e2e/uebungstypen.spec.ts` zählt am Ende die Einträge im Typ-Pulldown gegen die Manifest-Liste.

## Die echte Lücke

Dieselbe Klasse, gegen die die Manifest-Regel für Übungstypen geschrieben ist
(`frontend/CLAUDE.md`: *„Übungstypen kommen aus dem Server-Manifest"*, weil drei Kopien zwangsläufig
auseinanderliefen). Hier ist es **eine** Kopie — aber sie hat vier Leser, die aus ihr drei verschiedene
Antworten ableiten. Der Schaden ist damit nicht mehr eine fehlende Beschriftung, sondern je nach Stelle
ein gesperrtes Bedienelement, ein still verworfener Wert oder ein unerreichbarer Zustand.

Und die Lücke ist breiter, als der Titel sagt: Selbst wenn die Liste morgen vom Server käme, blieben die
vier Stellen uneinig darüber, **was ein unbekannter Wert bedeutet**. Die Herkunft der Liste und die
Semantik „unbekannter Wert" sind zwei Fragen, und nur die erste steht im Titel.

**Kein `entgangen_bei`:** Die Liste ist alt; B-143 und B-148 haben ihr nur Aufgaben gegeben, für die sie
nicht gedacht war.

## Offene Punkte

1. ~~**Woher kommt die Liste künftig?** … zuerst nachsehen, ob das OpenAPI-Dokument die Einzelwerte
   ausweist — davon hängt ab, ob es geschenkt ist.~~ **Gemessen am 2026-08-11: nein.** `Program.cs:313`
   lässt `schema.Enum` für `[Flags]` bewusst weg (B-60), die Werte stehen nur in der englischen
   Beschreibung. Damit bleiben **zwei** Wege: ein Endpunkt (Muster `ExerciseTypesController`) oder ein
   Tor, das die handgepflegte Liste stehen lässt und bewacht.
   **Empfehlung: das Tor.** Ein Endpunkt kostet Route, DTO, Client-Methode und einen Ladezustand in
   sieben Dateien; er macht eine Liste asynchron, die heute beim Rendern einfach dasteht — für sechs
   Werte, die sich seit dem ersten Commit nicht geändert haben. Das Tor kostet eine Datei und meldet den einen
   Fall, um den es geht.
2. **Wenn Tor: wo?** Ein C#-Test wäre der erste, der eine Frontend-Datei liest — dafür läuft er im
   normalen `dotnet test` mit. Ein E2E hätte das Vorbild (`uebungstypen.spec.ts`), liefe aber nur im
   langsamen Lauf und braucht dafür einen Bildschirm, der alle Werte zeigt.
   **Empfehlung: C#-Test**, in `Pugling.Api.Tests`, der `labels.ts` gegen `Enum.GetNames<SchoolTypes>()`
   hält (`None` ausgenommen, das ist kein Auswahlwert). Er ist billiger und schneller als der E2E, und
   „erster seiner Art" ist kein Gegenargument — der Client-Routen-Wächter war das auch.
3. **Neu: Gehört die Uneinigkeit der vier Stellen in diese Story?** Sie ist der teurere Teil des Fundes,
   aber ein eigener Gedanke: Selbst mit bewachter Liste bleiben drei Antworten auf „unbekannter Wert".
   **Empfehlung: nein, eigene Story.** Das Ziel dieser hier („eine neue Schulart fällt auf") ist ohne sie
   erfüllt, und die Vereinheitlichung ist eine Produktfrage (was *soll* passieren?), keine Aufräumarbeit.
4. **Neu: `VaterKind.tsx:150,186` trägt dieselbe Unerreichbarkeit, die heute in B-148 als Regression
   behoben wurde.** Der Kommentar dort begründet nur die eine Hälfte („sonst hätte jedes Speichern die
   Kombination zurückgesetzt") und verschweigt die andere: `None` ist damit nicht mehr auszulösen. Die
   Reihe und — seit heute — das Fachlehrer-Profil lösen das mit der gesperrten Option.
   **Empfehlung: mit Punkt 3 zusammen in die eigene Story**, als deren konkretester Fall.

## Akzeptanzkriterien (Entwurf, final erst nach dem Grillen)

1. Ergänzt jemand einen Wert in `SchoolTypes`, wird ein Lauf rot und nennt `labels.ts` als Fundort.
2. Entfernt jemand einen Wert aus `SchoolTypes`, ebenso.
3. `None` ist ausdrücklich ausgenommen und der Grund steht im Test — es ist „nicht gesetzt", kein
   Auswahlwert (`labels.ts:30` sagt das heute schon).
4. Die Reihenfolge der Liste bleibt Sache des Frontends: Sie ist eine Anzeigeentscheidung, das Tor prüft
   die **Menge**.

## Verlauf

- **2026-08-10** — angelegt aus dem Frontend-Review des Nachtlauf-Sprints 3. Nicht im Sprint behoben: der
  Fund betrifft eine Datei, die dieser Sprint gar nicht anfasst, und die Lösung ist eine eigene
  Entwurfsfrage.
- **2026-08-11** — **ausformuliert.** Zwei Messungen haben die Story verändert.
  **Erstens ist der billigste Weg tot:** Das OpenAPI-Dokument trägt die Einzelwerte eines
  `[Flags]`-Enums nicht maschinenlesbar, und zwar mit Vorsatz (`Program.cs:313-314`, Begründung aus
  B-60) — sie stehen nur in der englischen Beschreibung. Aus drei Wegen wurden zwei, und die Empfehlung
  kippt vom Manifest-Endpunkt auf ein **Tor**: Ein Endpunkt kostet Route, DTO, Client-Methode und einen
  `useAsync` in acht Formularen, für sechs Werte, die sich seit dem ersten Commit nicht geändert haben.
  **Zweitens ist der Fund größer als sein Titel:** Die Liste hat **dreizehn** Fundstellen in sieben
  Dateien, **vier** davon entscheidungstragend — und die vier leiten aus ihr **drei verschiedene** Antworten auf „unbekannter
  Wert" ab (gesperrte Option / stiller Verwurf in `ExerciseEditModal.tsx:143` / Unerreichbarkeit in
  `VaterKind.tsx:150,186`). Die Story nannte nur die erste. Das gehört als eigene Story abgespalten
  (offene Punkte 3 und 4) — es ist eine Produktfrage, keine Aufräumarbeit.
  Nebenbei belegt: `VaterKind` trägt dieselbe Unerreichbarkeit, die heute Vormittag in B-148 als
  Regression behoben wurde, und ihr Kommentar begründet nur die andere Hälfte.
